using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Multiplayer;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Actions = AsuMech.Scenarios.Ucob.UcobActions;

namespace AsuMech.Scenarios.Ucob.P5Exaflares;

// UCOB P5 "Exaflares": one set of Bahamut Prime's Exaflares (UcobExaflarePattern). The flame is
// the actions' AnimationEnd gimmick timeline, played by the action effect on the helper.
// Lingering flame is decorative: only the snapshot kills.
public sealed class UcobP5ExaflaresScenario : IMultiplayerReplayable
{
    public string Name => "Exaflares";
    public IPhase Phase => UcobZone.P5;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP5ExaflaresAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly UcobP5ExaflaresSettingsWindow settingsWindow = new();

    private const float DespawnAfterLastHit = 4f;

    private UcobP5ExaflaresState state = null!;

    // Polled by the multiplayer host; null until Run has rolled the pattern.
    public UcobP5ExaflaresState? LastState { get; private set; }
    private SimWorld world = null!;
    private SimEnemy? bahamut;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;

        state = new UcobP5ExaflaresState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        var solo = !selectedAi.HasValue;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UcobP5ExaflaresState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnBahamut);
        world.Events.Add(0.5f, () => bahamut?.AddStatusParam(StatusId.LohsDaih, LohsDaihTransformation));
        state.Pattern.Schedule(world, () => bahamut);
        world.Events.Add(state.LastHitAt + DespawnAfterLastHit, () => bahamut?.Despawn());
        
        if (!solo)
        {
            world.Events.Add(5f, () => bahamut?.SetTarget(world.Party.Get(PartyRole.MainTank)));
            world.Events.Add(8.98f, () => AutoAttackMainTank(1));
            world.Events.Add(12.00f, () => AutoAttackMainTank(1));
            world.Events.Add(15.03f, () => AutoAttackMainTank(2));
            world.Events.Add(18.08f, () => AutoAttackMainTank(1));
            world.Events.Add(21.11f, () => AutoAttackMainTank(0));
        }
    }

    private void SpawnBahamut()
    {
        bahamut = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.BahamutPrime,
            NameId: BNpcNameId.BahamutPrime,
            Level: UcobConstants.Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Visibility: SpawnVisibility.Visible,
            Placement: new Placement(Vector3.Zero, 0f)));
    }

    private void AutoAttackMainTank(byte variation)
        => bahamut?.Cast(Actions.AutoAttack, world.Party.Get(PartyRole.MainTank), variation);

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s
            ? new UcobP5ExaflaresAiReplayStateMessage(s.Direction.RadiansFromNorth, s.LaneOrder.ToArray())
            : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not UcobP5ExaflaresAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        var shadowState = UcobP5ExaflaresState.FromNetworkReplay(msg.DirectionRadians, msg.LaneOrder);
        if (shadowState == null) return null;
        ((IScenarioAi<UcobP5ExaflaresState>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }
}
