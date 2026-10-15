using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P2LightRampant.FruP2LightRampantState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P2LightRampant;

public sealed class FruP2LightRampantScenario : IScenario
{
    public string Name => "Light Rampant";
    public IPhase Phase => FruZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP2LightRampantAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP2LightRampantSettingsWindow settingsWindow = new();

    private const float PuddleRadius = 6f;
    private const float PuddleGrace = 1.2f;
    private const float PuddleLifetime = 11f;
    // Curse chains stretched shorter than this snap.
    private const float CurseMinLength = 25f;

    private SimWorld world = null!;
    private FruP2LightRampantState state = null!;
    private SimEnemy? usurper;
    private readonly SimEnemy?[] helpers = new SimEnemy?[8];
    private readonly List<SimTether> chains = [];
    private readonly List<(Vector3 Position, float ArmedAt, float EndsAt, SimEventObject? Visual)> puddles = [];
    private readonly Dictionary<SimCharacter, Vector3> previousPositions = new();
    private bool cursed;
    private bool wiped;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP2LightRampantState(world.Rng, settingsWindow.Overrides);
        chains.Clear();
        puddles.Clear();
        previousPositions.Clear();
        cursed = false;
        wiped = false;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP2LightRampantState>)AiStrats[idx]).Run(state, world);

        world.OnDeathWithStatus(StatusId.ChainsOfEverlastingLight, _ => InescapableIllumination());
        world.OnDeathWithStatus(StatusId.CurseOfEverlastingLight, _ => InescapableIllumination());

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(3f, () => usurper?.Cast(Actions.LightRampant));
        world.Events.Add(8.75f, AssignDebuffs);
        world.Events.Add(8.75f, () => Chain(StatusId.ChainsOfEverlastingLight, TetherId.ChainsOfEverlastingLight, TowerAt - 8.75f));
        world.Events.Add(11f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(11f, () => usurper?.SetTargetable(false));
        world.Events.Add(11f, ShowTowers);
        for (var wave = 0; wave < HammerWaves; wave++)
        {
            var w = wave;
            world.Events.Add(HammerAt(w), () => LuminousHammer(HammerAt(w)));
        }
        world.Events.Add(TowerAt, BrightHunger);
        world.Events.Add(TowerAt, () => Chain(StatusId.CurseOfEverlastingLight, TetherId.CurseOfEverlastingLight, CursesEndAt - TowerAt));
        world.Events.Add(20f, () => HolyLights(first: true));
        world.Events.Add(23f, () => HolyLights(first: false));
        world.Events.Add(PowerfulLightAt, PowerfulLight);
        world.Events.Add(PowerfulLightAt, () => world.Map.AddEffect(MapEffect.Show, MapEffect.LightRampantCenterTower));
        world.Events.Add(CursesEndAt, ClearChains);
        world.Events.Add(29.6f, () => usurper?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(29.6f, () => usurper?.SetTargetable(true));
        world.Events.Add(32f, () => usurper?.Cast(state.Pairs ? Actions.BanishPairsCast : Actions.BanishSpreadCast));
        world.Events.Add(33.7f, CenterTower);
        world.Events.Add(37.1f, Banish);
        world.Events.Add(40f, () => usurper?.Cast(Actions.HouseOfLightCast));
        world.Events.Add(45f, HouseOfLight);
    }

    private void SpawnActors()
    {
        usurper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.UsurperP2,
            NameId: BNpcNameId.Usurper,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.OnlyWhenVisible,
            Placement: new Placement(Vector3.Zero, MathF.PI),
            ModelCharaId: ModelCharaId.UsurperLight));
        for (var i = 0; i < helpers.Length; i++) helpers[i] = SpawnHelper(BNpcBaseId.Helper, Vector3.Zero);
    }

    private SimEnemy? SpawnHelper(uint bnpcBaseId, Vector3 position) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: bnpcBaseId,
        NameId: BNpcNameId.Usurper,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: bnpcBaseId == BNpcBaseId.Helper ? SpawnVisibility.InvisibleHelper : SpawnVisibility.Visible,
        Placement: new Placement(position, 0f)));

    private void AssignDebuffs()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            if (state.InitialStacks(role) is > 0 and var stacks)
                member.AddStatus(StatusId.Lightsteeped, 39.25f, stacks, overrideStacks: true);
            if (state.BaitsHammer(role)) member.AttachLockonVfx(LockonId.LuminousHammer);
        }
        foreach (var role in state.Weights)
            world.Party.Get(role)?.AddStatus(StatusId.WeightOfLight, PowerfulLightAt - 8.75f);
    }

    private void Chain(ushort statusId, ushort tetherId, float duration)
    {
        ClearChains();
        cursed = statusId == StatusId.CurseOfEverlastingLight;
        for (var i = 0; i < 6; i++)
        {
            var (from, to) = state.ChainLink(i);
            world.Party.Get(from)?.AddStatus(statusId, duration);
            chains.Add(world.Tether(world.Party.Get(from), world.Party.Get(to), tetherId));
        }
    }

    private void ClearChains()
    {
        cursed = false;
        chains.ForEach(chain => chain.Despawn());
        chains.Clear();
    }

    private void InescapableIllumination() => Wipe(ActionId.InescapableIllumination, "a chained player died");

    // Once: the wipe's own deaths would otherwise set it off again.
    private void Wipe(uint actionId, string explanation)
    {
        if (wiped) return;
        wiped = true;
        world.Party.WipeAllPlayers(actionId, explanation);
    }

    private void ShowTowers()
    {
        for (var i = 0; i < 6; i++) world.Map.AddEffect(MapEffect.Show, (byte)(MapEffect.LightRampantFirstTower + i));
    }

    private void LuminousHammer(float at)
    {
        var i = 0;
        foreach (var role in state.HammerBaiters)
        {
            if (world.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            CastAt(helpers[i++], Actions.LuminousHammer, target);
            var visual = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.HolyPuddle,
                Placement = new Placement(target.Position, 0f),
                TimelineState = 1,
            });
            puddles.Add((target.Position, at + PuddleGrace, at + PuddleLifetime, visual));
        }
    }

    // Each of the six towers wants exactly one; anything else wipes.
    private void BrightHunger()
    {
        for (var i = 0; i < 6; i++)
        {
            world.Map.AddEffect(MapEffect.Hide, (byte)(MapEffect.LightRampantFirstTower + i));
            var soakers = world.Party.Find.InsideCircle(Tower(i), TowerRadius).Count;
            if (soakers != 1)
            {
                Wipe(ActionId.BrightHunger, $"{soakers}/1 in a tower");
                return;
            }
            helpers[i]?.SetPosition(Tower(i));
            helpers[i]?.Cast(Actions.BrightHunger, Tower(i));
        }
    }

    private void HolyLights(bool first)
    {
        foreach (var position in state.Orbs(first))
        {
            var orb = SpawnHelper(BNpcBaseId.HolyLight, position);
            world.Events.Add(1.7f, () => orb?.Cast(Actions.Burst));
            world.Events.Add(7.5f, () => orb?.FadeOut());
        }
    }

    private void PowerfulLight()
    {
        var i = 0;
        foreach (var role in state.Weights)
            if (world.Party.Get(role) is { } target && target.IsAlive())
                CastAt(helpers[i++], Actions.PowerfulLight, target);
    }

    private void CenterTower()
    {
        world.Map.AddEffect(MapEffect.Hide, MapEffect.LightRampantCenterTower);
        helpers[0]?.SetPosition(Vector3.Zero);
        helpers[0]?.Cast(Actions.BrightHungerCenter, Vector3.Zero);
    }

    // Pairs go on all supports or all DPS; spreads on everyone.
    private void Banish()
    {
        var targets = state.Pairs
            ? world.Party.ActiveMembers().Where(m => m is ISimPartyMember p && p.Role.IsDps() != state.SupportPairs)
            : world.Party.ActiveMembers();
        var i = 0;
        foreach (var target in targets.ToList())
            CastAt(helpers[i++], state.Pairs ? Actions.BanishPairs : Actions.BanishSpread, target);
    }

    private void HouseOfLight()
    {
        var i = 0;
        foreach (var target in world.Party.ActiveMembers().ToList())
        {
            helpers[i]?.SetPosition(Vector3.Zero);
            helpers[i++]?.Cast(Actions.HouseOfLight, target);
        }
    }

    private static void CastAt(SimEnemy? helper, EnemyAction action, SimCharacter target)
    {
        helper?.SetPosition(target.Position);
        helper?.Cast(action, target);
    }

    public void Tick(float delta, float elapsed)
    {
        foreach (var puddle in puddles.Where(p => elapsed >= p.EndsAt)) puddle.Visual?.Despawn();
        puddles.RemoveAll(p => elapsed >= p.EndsAt);
        foreach (var member in world.Party.ActiveMembers().ToList())
        {
            var previous = previousPositions.GetValueOrDefault(member, member.Position);
            previousPositions[member] = member.Position;
            if (puddles.Any(p => elapsed >= p.ArmedAt && DistanceToSegment(p.Position, previous, member.Position) <= PuddleRadius))
                member.Die(ActionId.LuminousHammer, "stepped in a Luminous Hammer puddle");
        }
        if (cursed && ChainTooShort())
            Wipe(ActionId.RefulgentFate, "a Curse of Everlasting Light chain was too short");
    }

    private bool ChainTooShort()
    {
        for (var i = 0; i < 6; i++)
        {
            var (from, to) = state.ChainLink(i);
            if (world.Party.Get(from) is { } a && world.Party.Get(to) is { } b && Vector3.Distance(a.Position, b.Position) < CurseMinLength)
                return true;
        }
        return false;
    }

    private static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        var line = to - from;
        var t = line.LengthSquared() > 0 ? Math.Clamp(Vector3.Dot(point - from, line) / line.LengthSquared(), 0, 1) : 0;
        return Vector3.Distance(point, from + t * line);
    }
}
