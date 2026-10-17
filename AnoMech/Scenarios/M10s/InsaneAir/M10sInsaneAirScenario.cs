using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.M10sConstants;
using Actions = AnoMech.Scenarios.M10s.M10sActions;

namespace AnoMech.Scenarios.M10s.InsaneAir;

// Both brothers together, from Xtreme Spectacular to their Divers' Dare: the party waits out the
// multi-hit raidwide on the north wall, then each brother rides its surfboard round half the arena
// for four stops, snapping proteans, re-entering on a light party stack or dropping a tank buster
// on the group that follows it.
public sealed class M10sInsaneAirScenario : IScenario
{
    public string Name => "Xtreme Spectacular to Insane Air";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sInsaneAirAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sInsaneAirSettingsWindow settingsWindow = new();

    public const float BrothersAppearAt = 5.58f;
    public const float XtremeCastAt = 11.42f;
    public const float XtremeHitsAt = 22.85f;
    public const float InsaneAirAt = 42.88f;
    public static readonly float[] StopAt = [InsaneAirAt + 7.76f, InsaneAirAt + 15.11f, InsaneAirAt + 22.46f, InsaneAirAt + 29.82f];
    public const float StopToHit = 1.16f;
    public const float DiversDareAt = 78.53f;

    private const byte SurfingModelState = 0x2F;
    private const float XtremeLength = 50f;

    private static readonly float[] XtremeFollowUps = [23.92f, 24.94f, 25.75f, 26.28f, 26.82f, 27.22f];
    private static readonly float[] RecastAt = [InsaneAirAt + 9.36f, InsaneAirAt + 16.71f, InsaneAirAt + 24.07f];

    private SimWorld world = null!;
    private M10sInsaneAirState state = null!;
    private SimEnemy? redHot;
    private SimEnemy? deepBlue;
    private SimEnemy? xtremes;
    private readonly SimEnemy?[] redHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] blueHelpers = new SimEnemy?[4];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sInsaneAirState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sInsaneAirState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(BrothersAppearAt, () => { redHot?.SetVisible(true); deepBlue?.SetVisible(true); });
        world.Events.Add(XtremeCastAt, () =>
        {
            redHot?.Cast(ActionId.XtremeSpectacularRed, animationLock: 0.2f);
            deepBlue?.Cast(ActionId.XtremeSpectacularBlue, animationLock: 0.2f);
            redHot?.SetModelState(SurfingModelState);
            deepBlue?.SetModelState(SurfingModelState);
        });
        world.Events.Add(15.66f, () => xtremes?.Cast(Actions.XtremeSpectacular));
        world.Events.Add(16.1f, () =>
        {
            redHot?.SetTargetable(false);
            deepBlue?.SetTargetable(false);
            world.Map.AddEffect(MapEffect.SpectacularOn, MapEffect.ArenaSlot);
        });
        foreach (var at in XtremeFollowUps)
            world.Events.Add(at, () => xtremes?.Cast(Actions.XtremeSpectacularHit));
        world.Events.Add(27.62f, () => xtremes?.Cast(Actions.XtremeSpectacularLast));
        world.Events.Add(27.74f, () =>
        {
            world.Map.AddEffect(MapEffect.SpectacularOff, MapEffect.ArenaSlot);
            redHot?.SetModelState(0);
            deepBlue?.SetModelState(0);
            redHot?.SetAnimationState(0, 0);
            deepBlue?.SetAnimationState(0, 0);
        });
        world.Events.Add(31.1f, () => { redHot?.SetVisible(false); deepBlue?.SetVisible(false); });
        world.Events.Add(31.45f, () =>
        {
            redHot?.SetVisible(true);
            deepBlue?.SetVisible(true);
            redHot?.SetTargetable(true);
            deepBlue?.SetTargetable(true);
            redHot?.PlayActionTimeline(TimelineId.RedHotArrive);
            deepBlue?.PlayActionTimeline(TimelineId.Arrive);
        });
        world.Events.Add(32.99f, () =>
        {
            redHot?.Cast(ActionId.EpicBrotherhood, deepBlue, animationLock: 0.2f);
            deepBlue?.Cast(ActionId.EpicBrotherhoodBlue, redHot, animationLock: 0.2f);
        });
        world.Events.Add(40.07f, () =>
        {
            redHot?.AddStatus(StatusId.DirectionalDisregard);
            deepBlue?.AddStatus(StatusId.DirectionalDisregard);
        });
        world.Events.Add(40.21f, () =>
        {
            redHot?.Cast(ActionId.Leap, animationLock: 0.8f);
            deepBlue?.Cast(ActionId.DeepBlueLeap, animationLock: 0.8f);
        });
        world.Events.Add(42.6f, () =>
        {
            redHot?.SetPosition(Stop(InsaneAirVariant.RedHotStart));
            deepBlue?.SetPosition(Stop(InsaneAirVariant.DeepBlueStart));
        });
        foreach (var (at, slot, effect) in state.Variant.MapEffects)
            world.Events.Add(InsaneAirAt + at, () => world.Map.AddEffect(effect, slot));
        world.Events.Add(InsaneAirAt, () =>
        {
            redHot?.Cast(ActionId.InsaneAirRedFirst, animationLock: 0.2f);
            deepBlue?.Cast(ActionId.InsaneAirBlueFirst, animationLock: 0.2f);
        });
        foreach (var at in RecastAt)
            world.Events.Add(at, () =>
            {
                redHot?.Cast(ActionId.InsaneAirRed, animationLock: 0.2f);
                deepBlue?.Cast(ActionId.InsaneAirBlue, animationLock: 0.2f);
            });
        for (var round = 0; round < M10sInsaneAirState.Rounds; round++)
        {
            var r = round;
            world.Events.Add(StopAt[r], () => Surf(r));
            world.Events.Add(StopAt[r] + StopToHit, () => Resolve(r));
        }
        world.Events.Add(InsaneAirAt + 31.4f, () =>
        {
            redHot?.RemoveStatus(StatusId.DirectionalDisregard);
            deepBlue?.RemoveStatus(StatusId.DirectionalDisregard);
            redHot?.SetPosition(new Placement(new Vector3(5f, 0f, 0f), 0f));
            deepBlue?.SetPosition(new Placement(new Vector3(-5f, 0f, 0f), 0f));
        });
        world.Events.Add(DiversDareAt, () =>
        {
            redHot?.Cast(Actions.DiversDare);
            deepBlue?.Cast(Actions.DiversDareBlue);
        });
        world.Events.Add(86f, () => { });
    }

    private void SpawnActors()
    {
        redHot = SpawnBrother(BNpcBaseId.RedHot, BNpcNameId.RedHot, new Vector3(5f, 0f, 0f));
        deepBlue = SpawnBrother(BNpcBaseId.DeepBlue, BNpcNameId.DeepBlue, new Vector3(-5f, 0f, 0f));
        xtremes = SpawnHelper(BNpcNameId.RedHot);
        // The duty centres this rectangle on the arena; the sim's rectangles start at the caster.
        xtremes?.SetPosition(new Placement(new Vector3(-XtremeLength / 2f, 0f, 0f), MathF.PI / 2f));
        for (var i = 0; i < 4; i++)
        {
            redHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
            blueHelpers[i] = SpawnHelper(BNpcNameId.DeepBlue);
        }
    }

    private SimEnemy? SpawnBrother(uint baseId, uint nameId, Vector3 at)
    {
        var brother = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(at, 0f)));
        brother?.SetVisible(false);
        return brother;
    }

    private SimEnemy? SpawnHelper(uint nameId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: nameId,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private static Placement Stop(Vector2 at) => new(new Vector3(at.X, 0f, at.Y), 0f);

    private void Surf(int round)
    {
        var red = state.Variant.RedHotStop(round);
        var blue = state.Variant.DeepBlueStop(round);
        redHot?.SetPosition(Stop(red).Face(Vector3.Zero));
        deepBlue?.SetPosition(Stop(blue).Face(Vector3.Zero));
        redHot?.Cast(RedMove(state.Variant.RedHotMoves[round]), animationLock: 0.6f);
        deepBlue?.Cast(BlueMove(state.Variant.DeepBlueMoves[round]), animationLock: 0.6f);
    }

    private static uint RedMove(SurfMove move) => move switch
    {
        SurfMove.Snap => ActionId.BlastingSnap,
        SurfMove.Reentry => ActionId.ReentryBlast,
        _ => ActionId.VerticalBlast,
    };

    private static uint BlueMove(SurfMove move) => move switch
    {
        SurfMove.Snap => ActionId.PlungingSnap,
        SurfMove.Reentry => ActionId.ReentryPlunge,
        _ => ActionId.VerticalPlunge,
    };

    private void Resolve(int round)
    {
        var red = state.Variant.RedHotStop(round);
        var blue = state.Variant.DeepBlueStop(round);
        var redTargets = Nearest(red, 4);
        var blueTargets = Nearest(blue, 4);
        Move(state.Variant.RedHotMoves[round], red, redTargets, redHelpers, Actions.BlastingSnapCone, Actions.ReentryBlastCone, Actions.VerticalBlastHit);
        Move(state.Variant.DeepBlueMoves[round], blue, blueTargets, blueHelpers, Actions.PlungingSnapCone, Actions.ReentryPlungeCone, Actions.VerticalPlungeHit);
    }

    // Each board picks the four players nearest it, nearest first; Re-entry and Vertical aim at the first.
    private List<SimCharacter> Nearest(Vector2 board, int count) =>
        world.Party.ActiveMembers().Where(m => m.IsAlive())
            .OrderBy(m => Vector2.DistanceSquared(new Vector2(m.Position.X, m.Position.Z), board))
            .Take(count).ToList();

    private static void Move(SurfMove move, Vector2 board, List<SimCharacter> targets, SimEnemy?[] helpers, EnemyAction snap, EnemyAction reentry, EnemyAction vertical)
    {
        if (targets.Count == 0) return;
        var from = new Vector3(board.X, 0f, board.Y);
        switch (move)
        {
            case SurfMove.Snap:
                for (var i = 0; i < targets.Count && i < helpers.Length; i++)
                {
                    helpers[i]?.SetPosition(new Placement(from, 0f).Face(targets[i].Position));
                    helpers[i]?.Cast(snap);
                }
                break;
            case SurfMove.Reentry:
                helpers[0]?.SetPosition(new Placement(from, 0f).Face(targets[0].Position));
                helpers[0]?.Cast(reentry);
                break;
            default:
                helpers[0]?.SetPosition(new Placement(targets[0].Position, 0f));
                helpers[0]?.Cast(vertical, targets[0]);
                break;
        }
    }
}
