using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M10s.InsaneAir;
using static AnoMech.Scenarios.M10s.M10sConstants;
using Actions = AnoMech.Scenarios.M10s.M10sActions;

namespace AnoMech.Scenarios.M10s.XtremeSnaking;

// The brothers' second Insane Air: Xtreme snakes mark four players fire and four water, then both
// boards make the same move at each of four stops; a hit of the other element washes a snake off and
// it bursts as Bailout around its player. A snake still on when it runs out kills.
public sealed class M10sXtremeSnakingScenario : IScenario
{
    public string Name => "Xtreme Snaking to Insane Air";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sXtremeSnakingAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sXtremeSnakingSettingsWindow settingsWindow = new();

    public const float SnakingCastAt = 3f;
    public const float SnakingHitAt = 7.97f;
    public const float InsaneAirAt = 15.97f;
    public static readonly float[] StopAt = [InsaneAirAt + 8.74f, InsaneAirAt + 19.07f, InsaneAirAt + 29.41f, InsaneAirAt + 39.75f];
    public const float StopToHit = 1.16f;
    public const float WashedOffAfterHit = 0.45f;
    public const float SnakesRunOutAt = 58.8f;
    public const float DiversDareAt = 61.54f;

    // UNVERIFIED: what a snake does when it runs out; every logged one was washed off first.
    private const float SnakeSeconds = 51f;
    private static readonly float[] RecastAt = [InsaneAirAt + 10.33f, InsaneAirAt + 20.67f, InsaneAirAt + 31.01f];
    private static readonly float[] ArrowsAt = [InsaneAirAt - 0.09f, InsaneAirAt + 8.65f, InsaneAirAt + 18.98f, InsaneAirAt + 29.33f, InsaneAirAt + 39.66f];
    private static readonly uint[] BoardStates = [0x00020001, 0x00200010, 0x00800040, 0x02000100, 0x04000004];
    private const uint ArrowOff = 0x00080004;

    private SimWorld world = null!;
    private M10sXtremeSnakingState state = null!;
    private SimEnemy? redHot;
    private SimEnemy? deepBlue;
    private readonly SimEnemy?[] redHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] blueHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] bailoutHelpers = new SimEnemy?[4];
    private readonly Dictionary<SimCharacter, ushort> snakes = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sXtremeSnakingState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        snakes.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sXtremeSnakingState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(SnakingCastAt, () =>
        {
            redHot?.Cast(Actions.XtremeFiresnaking);
            deepBlue?.Cast(Actions.XtremeWatersnaking);
        });
        world.Events.Add(SnakingHitAt - 0.05f, PickTheSnakes);
        world.Events.Add(InsaneAirAt - 2.81f, () =>
        {
            redHot?.AddStatus(StatusId.DirectionalDisregard);
            deepBlue?.AddStatus(StatusId.DirectionalDisregard);
        });
        world.Events.Add(InsaneAirAt - 2.67f, () =>
        {
            redHot?.Cast(ActionId.Leap, animationLock: 0.8f);
            deepBlue?.Cast(ActionId.DeepBlueLeap, animationLock: 0.8f);
        });
        world.Events.Add(InsaneAirAt - 0.28f, () =>
        {
            redHot?.SetPosition(Stop(InsaneAirVariant.RedHotStart));
            deepBlue?.SetPosition(Stop(InsaneAirVariant.DeepBlueStart));
        });
        ScheduleBoardsAndArrows();
        world.Events.Add(InsaneAirAt, () =>
        {
            redHot?.Cast(ActionId.InsaneAirTwoRedFirst, animationLock: 0.2f);
            deepBlue?.Cast(ActionId.InsaneAirTwoBlueFirst, animationLock: 0.2f);
        });
        foreach (var at in RecastAt)
            world.Events.Add(at, () =>
            {
                redHot?.Cast(ActionId.InsaneAirTwoRed, animationLock: 0.2f);
                deepBlue?.Cast(ActionId.InsaneAirTwoBlue, animationLock: 0.2f);
            });
        for (var round = 0; round < M10sXtremeSnakingState.Rounds; round++)
        {
            var r = round;
            world.Events.Add(StopAt[r], () => Surf(r));
            world.Events.Add(StopAt[r] + StopToHit, () => Resolve(r));
            world.Events.Add(StopAt[r] + StopToHit + WashedOffAfterHit, BurstTheWashedSnakes);
        }
        world.Events.Add(InsaneAirAt + 41.5f, () =>
        {
            redHot?.RemoveStatus(StatusId.DirectionalDisregard);
            deepBlue?.RemoveStatus(StatusId.DirectionalDisregard);
            redHot?.SetPosition(new Placement(new Vector3(5f, 0f, 0f), -MathF.PI / 2f));
            deepBlue?.SetPosition(new Placement(new Vector3(-5f, 0f, 0f), MathF.PI / 2f));
        });
        world.Events.Add(SnakesRunOutAt, SnakesRunOut);
        world.Events.Add(DiversDareAt, () =>
        {
            redHot?.Cast(Actions.DiversDare);
            deepBlue?.Cast(Actions.DiversDareBlue);
        });
        world.Events.Add(69f, () => { });
    }

    private void SpawnActors()
    {
        redHot = SpawnBrother(BNpcBaseId.RedHot, BNpcNameId.RedHot, new Placement(new Vector3(5f, 0f, 0f), -MathF.PI / 2f));
        deepBlue = SpawnBrother(BNpcBaseId.DeepBlue, BNpcNameId.DeepBlue, new Placement(new Vector3(-5f, 0f, 0f), MathF.PI / 2f));
        for (var i = 0; i < 4; i++)
        {
            redHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
            blueHelpers[i] = SpawnHelper(BNpcNameId.DeepBlue);
            bailoutHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
        }
    }

    private SimEnemy? SpawnBrother(uint baseId, uint nameId, Placement at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: baseId,
        NameId: nameId,
        Level: Level,
        Targetable: true,
        EnemyList: EnemyListMode.Always,
        Placement: at));

    private SimEnemy? SpawnHelper(uint nameId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: nameId,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private static Placement Stop(Vector2 at) => new(new Vector3(at.X, 0f, at.Y), 0f);

    private List<SimCharacter> Alive() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList();

    private void PickTheSnakes()
    {
        if (redHot is not { } boss) return;
        var byDistance = Alive().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).ToList();
        if (!state.FireOnTheNearest) byDistance.Reverse();
        state.Fire.Clear();
        for (var i = 0; i < byDistance.Count; i++)
        {
            var snake = i < 4 ? StatusId.XtremeFiresnaking : StatusId.XtremeWatersnaking;
            byDistance[i].AddStatus(snake, SnakeSeconds);
            snakes[byDistance[i]] = snake;
            if (i < 4 && RoleOf(byDistance[i]) is { } role) state.Fire.Add(role);
        }
    }

    private PartyRole? RoleOf(SimCharacter member) =>
        Enum.GetValues<PartyRole>().Cast<PartyRole?>().FirstOrDefault(r => world.Party.Get(r!.Value) == member);

    // Each board's arrow shows the move at its next stop; the pair of board markers walks with them.
    private void ScheduleBoardsAndArrows()
    {
        var middle = state.RedHotEndsInTheMiddle;
        var (boardA, boardB) = middle ? ((byte)0x0C, (byte)0x0D) : ((byte)0x0A, (byte)0x0B);
        byte[] redSlots = [0x0F, 0x10, 0x13, middle ? (byte)0x16 : (byte)0x12];
        byte[] blueSlots = [0x15, 0x14, 0x11, middle ? (byte)0x12 : (byte)0x0E];
        for (var i = 0; i < ArrowsAt.Length; i++)
        {
            var boardState = BoardStates[i];
            world.Events.Add(ArrowsAt[i], () =>
            {
                world.Map.AddEffect(boardState, boardA);
                world.Map.AddEffect(boardState, boardB);
            });
        }
        for (var round = 0; round < M10sXtremeSnakingState.Rounds; round++)
        {
            var move = state.Moves[round];
            var (red, blue) = (redSlots[round], blueSlots[round]);
            world.Events.Add(ArrowsAt[round], () =>
            {
                world.Map.AddEffect(RedArrow(move), red);
                world.Map.AddEffect(BlueArrow(move), blue);
            });
            world.Events.Add(ArrowsAt[round + 1], () =>
            {
                world.Map.AddEffect(ArrowOff, red);
                world.Map.AddEffect(ArrowOff, blue);
            });
        }
    }

    private static uint RedArrow(SurfMove move) => move switch
    {
        SurfMove.Snap => 0x00020001,
        SurfMove.Reentry => 0x00200010,
        _ => 0x00800040,
    };

    private static uint BlueArrow(SurfMove move) => move switch
    {
        SurfMove.Snap => 0x02000100,
        SurfMove.Reentry => 0x08000400,
        _ => 0x20001000,
    };

    private void Surf(int round)
    {
        var red = state.Boards.RedHotStop(round);
        var blue = state.Boards.DeepBlueStop(round);
        var move = state.Moves[round];
        redHot?.SetPosition(Stop(red).Face(Vector3.Zero));
        deepBlue?.SetPosition(Stop(blue).Face(Vector3.Zero));
        redHot?.Cast(move switch { SurfMove.Snap => ActionId.BlastingSnap, SurfMove.Reentry => ActionId.ReentryBlast, _ => ActionId.VerticalBlast }, animationLock: 0.6f);
        deepBlue?.Cast(move switch { SurfMove.Snap => ActionId.PlungingSnap, SurfMove.Reentry => ActionId.ReentryPlunge, _ => ActionId.VerticalPlunge }, animationLock: 0.6f);
    }

    private void Resolve(int round)
    {
        var red = state.Boards.RedHotStop(round);
        var blue = state.Boards.DeepBlueStop(round);
        var move = state.Moves[round];
        Move(move, red, Nearest(red), redHelpers, Actions.BlastingSnapConeWashing, Actions.ReentryBlastConeWashing, Actions.VerticalBlastHitWashing);
        Move(move, blue, Nearest(blue), blueHelpers, Actions.PlungingSnapConeWashing, Actions.ReentryPlungeConeWashing, Actions.VerticalPlungeHitWashing);
    }

    // Each board picks the four players nearest it, nearest first; Re-entry and Vertical aim at the first.
    private List<SimCharacter> Nearest(Vector2 board) =>
        Alive().OrderBy(m => Vector2.DistanceSquared(new Vector2(m.Position.X, m.Position.Z), board)).Take(4).ToList();

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

    private void BurstTheWashedSnakes()
    {
        var helper = 0;
        foreach (var (member, snake) in snakes.ToList())
        {
            if (member.HasStatus(snake)) continue;
            snakes.Remove(member);
            if (!member.IsAlive() || helper >= bailoutHelpers.Length) continue;
            bailoutHelpers[helper]?.SetPosition(new Placement(member.Position, 0f));
            bailoutHelpers[helper++]?.Cast(snake == StatusId.XtremeFiresnaking ? Actions.BailoutFire : Actions.BailoutWater, member);
        }
    }

    private void SnakesRunOut()
    {
        foreach (var (member, snake) in snakes.ToList())
        {
            if (!member.IsAlive() || !member.HasStatus(snake)) continue;
            member.RemoveStatus(snake);
            member.Die(snake == StatusId.XtremeFiresnaking ? ActionId.XtremeFiresnaking : ActionId.XtremeWatersnaking,
                "the snake was never washed off (take a hit from the other brother)");
        }
        snakes.Clear();
    }
}
