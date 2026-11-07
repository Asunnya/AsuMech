using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Actions = AsuMech.Scenarios.Ucob.UcobActions;
using Golden = AsuMech.Scenarios.Ucob.P5GoldenBahamut.UcobP5GoldenBahamutScenario;

namespace AsuMech.Scenarios.Ucob.P5PhoenixTransition;

// The P5 transition only: the Phoenix revives the party and Bahamut descends and turns golden;
// it ends where Golden Bahamut starts fighting.
public sealed class UcobP5PhoenixTransitionScenario : IScenario
{
    public string Name => "Phoenix Transition";
    public IPhase Phase => UcobZone.P5;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [];

    public const float PhoenixCry = 0.94f;
    public const float PhoenixAppears = 3.56f;
    public const float PhoenixAppearAnimation = 3.85f;
    public const float PhoenixRises = 13.98f;
    public const float FlamesOfRebirth = 15.09f;
    public const float PhoenixLeaves = 18.15f;
    public const float PhoenixGone = 19.15f;
    public const float GoldenDescent = 20.86f;
    public const float BahamutFliesToTheMiddle = 22.69f;
    public const float End = Golden.Targetable + 1f;

    private const float DescentSpeed = 30f;
    private static readonly Vector3 BahamutEdge = new(0f, 0f, -24f);

    private SimWorld world = null!;
    private SimEnemy? bahamut;
    private SimEnemy? phoenix;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        bahamut = null;
        phoenix = null;

        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, () => bahamut = SpawnBahamut(world, BahamutEdge));
        world.Events.Add(PhoenixCry, () => world.Announce("The Phoenix's shrill cry cuts through the pall of desperation!"));
        world.Events.Add(PhoenixAppears, SpawnPhoenix);
        world.Events.Add(PhoenixAppearAnimation, () => phoenix?.PlayActionTimeline(TimelineId.PhoenixAppears));
        world.Events.Add(PhoenixAppearAnimation, () => bahamut?.PlayActionTimeline(TimelineId.BahamutWatchesPhoenix));
        world.Events.Add(PhoenixRises, () => phoenix?.PlayActionTimeline(TimelineId.PhoenixRises));
        world.Events.Add(FlamesOfRebirth, () => phoenix?.Cast(Actions.FlamesOfRebirth));
        world.Events.Add(PhoenixLeaves, () => phoenix?.PlayActionTimeline(TimelineId.PhoenixLeaves));
        world.Events.Add(PhoenixGone, () => phoenix?.Despawn());
        world.Events.Add(GoldenDescent, () => bahamut?.Cast(ActionId.GoldenDescent, animationLock: 7.1f));
        world.Events.Add(BahamutFliesToTheMiddle, () => bahamut?.MoveTo(Vector3.Zero, DescentSpeed, finalRotation: 0f));
        world.Events.Add(Golden.LohsDaih, () => bahamut?.AddStatusParam(StatusId.LohsDaih, LohsDaihTransformation));
        world.Events.Add(Golden.InnerRage, () => world.Announce("Bahamut Prime unleashes his inner rage!"));
        world.Events.Add(Golden.InnerRageAnimation, () => bahamut?.PlayActionTimeline(TimelineId.BahamutInnerRage));
        world.Events.Add(End, () => bahamut?.Despawn());
    }

    public static SimEnemy? SpawnBahamut(SimWorld world, Vector3 at)
        => world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.BahamutPrime,
            NameId: BNpcNameId.BahamutPrime,
            Level: UcobConstants.Level,
            Targetable: false,
            EnemyList: EnemyListMode.Always,
            Visibility: SpawnVisibility.Visible,
            Placement: new Placement(at, 0f)));

    private void SpawnPhoenix()
        => phoenix = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Phoenix,
            NameId: BNpcNameId.Phoenix,
            Level: UcobConstants.Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SpawnVisibility.Visible,
            Placement: new Placement(Vector3.Zero, 0f),
            IsHostile: false));
}
