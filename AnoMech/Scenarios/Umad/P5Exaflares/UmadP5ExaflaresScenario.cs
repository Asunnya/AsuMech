using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Map;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Scenarios.Umad.P5Exaflares;

// UMAD P5 "exaflares" (ground-fire): two diagonal walls of fire roll across the arena, then a
// final spread. Runs solo or with bots (UmadP5ExaflaresAi).
//
// Rolling hits are animation-based (no per-tile marker, just the ExaflareOmen lane arrow): each
// eruption snapshots position as it goes off. Lingering fire is safe - only the snapshot instant kills.
public sealed class UmadP5ExaflaresScenario : IMultiplayerReplayable
{
    public string Name => "Exaflares";
    public IPhase Phase => UmadZone.P5;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;

    // Enemy spawn level.
    private const byte Level = 100;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UmadP5ExaflaresAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly UmadP5ExaflaresSettingsWindow settingsWindow = new();

    private UmadP5ExaflaresState state = null!;
    private SimWorld world = null!;
    private SimParty party = null!;
    private SimEnemy? kefka;
    // Spawned at the start: an actor drops what it sends in its first frames, so a helper spawned on
    // the tick it acts shows no animation. Each action moves a free one into place.
    private readonly List<SimEnemy?> helpers = [];
    private readonly List<float> helperFreeAt = [];
    private readonly List<(float T, int Helper)> rollingHits = [];
    
    private const float ExaflareFirstHitDelay = 4.582f; // first rolling hit, after the line launch
    private const float ExaflareHitInterval   = 0.513f; // between rolling hits
    private const int   ExaflareHitCount      = 6;
    // Full eruption length; keep the VFX helper alive this long so the lingering fire isn't cut.
    private const float ExaflareVfxDuration = 90f / 30f; // 3.0s
    // The arrow's release (the origin eruption) lands at its 4 s bar end; keep it for the eruption.
    private const float ArrowBusy = 4f + ExaflareVfxDuration;
    private const float SpreadT = 25.09f;
    private const float KefkaAnimationLock = 3.1f;
    private const float HelperAnimationLock = 1.1f;

    // The current run's randomized per-run assignments, exposed so
    // MultiplayerManager can read them after a host Start and broadcast them --
    // lets a peer's local "debug: bot controls my character" mode replay the
    // same choreography a host-side bot in that role would produce. Mirrors
    // UmadP3BlackHoleScenario.LastState.
    public UmadP5ExaflaresState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = worldParam.Party;
        state = new UmadP5ExaflaresState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        helpers.Clear();
        helperFreeAt.Clear();
        rollingHits.Clear();

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP5ExaflaresState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnKefka);
        world.Events.Add(0f, SpawnHelpers);
        world.Events.Add(2.89f, () => { if (kefka is { } boss) world.Map.DirectorUpdate(0x80000027U, 0x25U, 0x2U, 0x1BDBU, boss.EntityId); });
        world.Events.Add(3.0f, () => kefka?.Cast(ActionId.ChaosEnd1, animationLock: KefkaAnimationLock));

        // Rolling exaflares: left/right pairs every 2.5s, columns from the chosen order.
        LaunchExaflareLine(3.0f,  state.LeftOrder[0],  isLeft: true);
        LaunchExaflareLine(3.0f,  state.LeftOrder[1],  isLeft: true);
        LaunchExaflareLine(5.5f,  state.RightOrder[0], isLeft: false);
        LaunchExaflareLine(5.5f,  state.RightOrder[1], isLeft: false);
        LaunchExaflareLine(8.0f,  state.LeftOrder[2],  isLeft: true);
        LaunchExaflareLine(8.0f,  state.LeftOrder[3],  isLeft: true);
        LaunchExaflareLine(10.5f, state.RightOrder[2], isLeft: false);
        LaunchExaflareLine(10.5f, state.RightOrder[3], isLeft: false);
        LaunchExaflareLine(13.0f, state.LeftOrder[4],  isLeft: true);
        LaunchExaflareLine(13.0f, state.LeftOrder[5],  isLeft: true);
        LaunchExaflareLine(15.5f, state.RightOrder[4], isLeft: false);
        LaunchExaflareLine(15.5f, state.RightOrder[5], isLeft: false);

        world.Events.Add(19.10f, () => kefka?.VoiceLine(8206500));
        world.Events.Add(19.2f, () => kefka?.Cast(ActionId.ChaosEnd2, animationLock: KefkaAnimationLock));
        var spreadHelpers = LastRollingHitHelpers(8);
        world.Events.Add(SpreadT, () => ResolveSpread(spreadHelpers));
    }

    public void Tick(float delta, float elapsed)
    {
        state?.SpreadTick?.Invoke(delta); // bot spread relaxation (no-op in solo)
    }

    private void SpawnKefka()
    {
        kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.KefkaP5,
            NameId: BNpcNameId.Kefka,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.KefkaP5,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
    }

    // One rolling line of fire. `lineIdx` 1-6 faces the source; `isLeft` = top-left wall. The arrow
    // telegraph appears at launch and its release is the origin eruption; the rolling hits follow.
    private void LaunchExaflareLine(float startT, int lineIdx, bool isLeft)
    {
        var initPos = isLeft
            ? new Vector3(-35f + 5f * lineIdx, 0f, -5f * lineIdx)
            : new Vector3(5f * lineIdx, 0f, -35f + 5f * lineIdx);
        var dPos = isLeft ? new Vector3(5f, 0f, 5f) : new Vector3(-5f, 0f, 5f);
        var heading = MathF.PI * (isLeft ? 1f : -1f) / 4f;

        var arrow = ReserveHelper(startT, startT + ArrowBusy);
        world.Events.Add(startT, () =>
            PlaceHelper(arrow, initPos, heading)?.Cast(ActionId.ExaflareOmen, animationLock: HelperAnimationLock));

        for (var i = 0; i < ExaflareHitCount; i++)
        {
            var pos = initPos + dPos * (i + 1);
            var tErupt = startT + ExaflareFirstHitDelay + ExaflareHitInterval * i;

            var hit = ReserveHelper(tErupt, tErupt + ExaflareVfxDuration);
            rollingHits.Add((tErupt, hit));
            world.Events.Add(tErupt, () => PlaceHelper(hit, pos, 0f)?.Cast(UmadActions.ExaflareHit));
        }
    }

    // Final spread: every alive member is the target of their own spread, so a member is caught by
    // their own plus any overlapping one. All spreads resolve in this one event, so the vuln-up the
    // first covering leaves makes the second lethal.
    private void ResolveSpread(int[] spreadHelpers)
    {
        for (int i = 0; i < 8; i++)
        {
            var member = party.Get(i);
            if (member is null || !member.IsAlive()) continue;
            helpers[spreadHelpers[i]]?.Cast(UmadActions.ExaflareSpread, member);
        }
    }

    // Each spread fires from a helper still standing on its last exaflare tile.
    private int[] LastRollingHitHelpers(int count) =>
        rollingHits.OrderByDescending(h => h.T).Select(h => h.Helper).Distinct().Take(count).ToArray();

    // A helper free over [from, until], by schedule time; the set grows when none is.
    private int ReserveHelper(float from, float until)
    {
        var idx = helperFreeAt.FindIndex(freeAt => freeAt <= from);
        if (idx < 0)
        {
            idx = helperFreeAt.Count;
            helperFreeAt.Add(0f);
        }
        helperFreeAt[idx] = until;
        return idx;
    }

    private void SpawnHelpers()
    {
        foreach (var _ in helperFreeAt)
            helpers.Add(world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.KefkaHelper,
                NameId: BNpcNameId.Kefka,
                Level: 1,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Visibility: SpawnVisibility.InvisibleHelper,
                Placement: new Placement(Vector3.Zero, 0f))));
    }

    private SimEnemy? PlaceHelper(int idx, Vector3 position, float rotation)
    {
        var helper = helpers[idx];
        helper?.SetPosition(new Placement(position, rotation));
        return helper;
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new P5AiReplayStateMessage(s.LeftOrder.ToArray(), s.RightOrder.ToArray()) : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not P5AiReplayStateMessage msg) return null;
        var shadowState = UmadP5ExaflaresState.FromNetworkReplay(msg.LeftOrder, msg.RightOrder);
        ((IScenarioAi<UmadP5ExaflaresState>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }

    // A peer never runs this scenario's Tick, so the spread relaxation is driven from here.
    public void TickReplay(object shadowStateObj, float deltaSeconds)
    {
        if (shadowStateObj is UmadP5ExaflaresState shadowState)
            shadowState.SpreadTick?.Invoke(deltaSeconds);
    }
}
