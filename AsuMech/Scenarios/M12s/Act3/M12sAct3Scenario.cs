using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Actions = AsuMech.Scenarios.M12s.M12sActions;

namespace AsuMech.Scenarios.M12s.Act3;

// Grotesquerie: Act 3, t=0 = P1 3:10. Every growth points a way; snakes burrow through the floor,
// which falls but for four platforms. Each player's growth throws a tower onto the next platform,
// soaked by whoever stands there, under a spread burst. The floor comes back with holes, two snake
// heads line-buster the tanks and Venomous Scourge hits the three furthest east and west, then The
// Fixer.
public sealed class M12sAct3Scenario : IScenario
{
    public string Name => "Grotesquerie: Act 3";
    public IPhase Phase => M12sZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sAct3Ai()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sAct3SettingsWindow settingsWindow = new();

    public const float ActCastAt = 2.78f;
    public const float GrowthAt = 6.93f;
    public const float MitoticPhaseLasts = 10f;
    public const float FeralFissionAt = 8.98f;
    public static readonly float[] SnakeCastAt = [8.98f, 9.30f, 9.61f, 9.92f, 10.19f];
    public const float SnakeAppearShows = 3f;
    public const float SnakeLeaveShows = 3.5f;
    public const float BossBurrowsAt = 12.11f;
    public const float BringDownTheHouseAt = 15.15f;
    public const float PlatformsAt = 15.91f;
    public const float BindAt = 16.89f;
    public const float BindLasts = 3f;
    public const float MetamitosisAt = 16.98f;
    public const float LysisAt = 18.54f;
    public const float FloorReturnsAt = 19.88f;
    public const float HeadsSpawnAt = 21f;
    public const float HeadsRiseAt = 21.3f;
    public const float SplitScourgeCastAt = 27.51f;
    public const float SplitScourgeHitsAt = 28.67f;
    public const float VenomousScourgeAt = 31.12f;
    public const float BossReturnsAt = 33.13f;
    public const float FixerCastAt = 35.45f;
    public const float EndsAt = 42f;

    public static readonly Vector3 BossHome = new(0f, 0f, -15f);

    private sealed record Snake(uint AppearId, Vector2 Appear, float Rotation, byte AppearSlot, Vector2 Leave, byte LeaveSlot);

    private static readonly Snake[] IntercardinalSnakes =
    [
        new(ActionId.GrandEntranceAppear1, new(-7.5f, -12.5f), 1.25f, 0x0B, new(7.5f, -7.5f), 0x15),
        new(ActionId.GrandEntranceAppear3, new(-17.5f, -2.5f), 0.785f, 0x0C, new(-12.5f, 2.5f), 0x16),
        new(ActionId.GrandEntranceAppear1, new(-7.5f, -2.5f), 1.25f, 0x0D, new(7.5f, 2.5f), 0x17),
        new(ActionId.GrandEntranceAppear3, new(12.5f, -2.5f), 0.785f, 0x0E, new(17.5f, 2.5f), 0x18),
        new(ActionId.GrandEntranceAppear1, new(-7.5f, 7.5f), 1.25f, 0x0F, new(7.5f, 12.5f), 0x19),
    ];

    private static readonly Snake[] CardinalSnakes =
    [
        new(ActionId.GrandEntranceAppear2, new(-17.5f, -12.5f), 1.11f, 0x10, new(-7.5f, -7.5f), 0x1A),
        new(ActionId.GrandEntranceAppear2, new(7.5f, -12.5f), 1.11f, 0x11, new(17.5f, -7.5f), 0x1B),
        new(ActionId.GrandEntranceAppear1, new(7.5f, -2.5f), -1.25f, 0x12, new(-7.5f, 2.5f), 0x1C),
        new(ActionId.GrandEntranceAppear2, new(-17.5f, 7.5f), 1.11f, 0x13, new(-7.5f, 12.5f), 0x1D),
        new(ActionId.GrandEntranceAppear2, new(7.5f, 7.5f), 1.11f, 0x14, new(17.5f, 12.5f), 0x1E),
    ];

    // Each falling slab starts at its spot and runs 10y south.
    private static readonly (uint Id, Vector2 At)[] IntercardinalSlabs =
    [
        (ActionId.BringDownTheHouseLarge, new(0f, -15f)), (ActionId.BringDownTheHouseLarge, new(0f, -5f)), (ActionId.BringDownTheHouseLarge, new(0f, 5f)),
        (ActionId.BringDownTheHouseSmall, new(15f, -5f)), (ActionId.BringDownTheHouseSmall, new(-15f, -5f)),
    ];

    private static readonly (uint Id, Vector2 At)[] CardinalSlabs =
    [
        (ActionId.BringDownTheHouseMedium, new(-12.5f, -15f)), (ActionId.BringDownTheHouseMedium, new(12.5f, -15f)), (ActionId.BringDownTheHouseLarge, new(0f, -5f)),
        (ActionId.BringDownTheHouseMedium, new(-12.5f, 5f)), (ActionId.BringDownTheHouseMedium, new(12.5f, 5f)),
    ];

    public static IReadOnlyList<Placement> HeadSpots(bool cardinalPlatforms) => cardinalPlatforms
        ? [new(new Vector3(-15f, 0f, -10f), MathF.PI / 4f), new(new Vector3(15f, 0f, -10f), -MathF.PI / 4f)]
        : [new(new Vector3(-15f, 0f, 0f), MathF.PI / 2f), new(new Vector3(15f, 0f, 0f), -MathF.PI / 2f)];

    private SimWorld world = null!;
    private M12sAct3State state = null!;
    private Act3Floor floor = null!;
    private SimEnemy? boss;
    private readonly List<SimEnemy> heads = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sAct3State(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        floor = new Act3Floor(state);
        heads.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sAct3State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(floor, "Fell off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnBoss);
        world.Events.Add(1f, () => boss?.SetTarget(Alive(PartyRole.MainTank)));
        world.Events.Add(ActCastAt, () => boss?.Cast(ActionId.GrotesquerieAct3, animationLock: 0.2f));
        world.Events.Add(GrowthAt, GiveGrowths);
        world.Events.Add(FeralFissionAt, () => boss?.Cast(ActionId.FeralFission, animationLock: 0.2f));
        var snakes = state.CardinalPlatforms ? CardinalSnakes : IntercardinalSnakes;
        for (var i = 0; i < snakes.Length; i++)
        {
            var snake = snakes[i];
            world.Events.Add(SnakeCastAt[i], () => SnakesBurrow(snake));
            world.Events.Add(SnakeCastAt[i] + SnakeAppearShows, () => world.Map.AddEffect(MapEffect.SnakeShows, snake.AppearSlot));
            world.Events.Add(SnakeCastAt[i] + SnakeLeaveShows, () => world.Map.AddEffect(MapEffect.SnakeShows, snake.LeaveSlot));
        }
        world.Events.Add(BossBurrowsAt, () =>
        {
            boss?.SetModelState(ModelState.Burrowed);
            boss?.SetTargetable(false);
        });
        world.Events.Add(BringDownTheHouseAt, BringDownTheHouse);
        world.Events.Add(PlatformsAt, () => KeepOnlyPlatforms(snakes));
        world.Events.Add(BindAt, () => EachAlive(m => m.AddStatus(StatusId.Bind, BindLasts)));
        world.Events.Add(MetamitosisAt, Metamitosis);
        world.Events.Add(LysisAt, () => EachAlive(m => boss?.CastFromHelper(Actions.MitoticDramaticLysis, m, at: new Placement(m.Position, 0f))));
        world.Events.Add(FloorReturnsAt, FloorReturns);
        world.Events.Add(HeadsSpawnAt, SpawnHeads);
        world.Events.Add(HeadsRiseAt, () => heads.ForEach(h => h.PlayActionTimeline(ActionTimeline.SnakeHeadRises)));
        world.Events.Add(SplitScourgeCastAt, () => heads.ForEach(h => h.Cast(ActionId.SplitScourgeHead, animationLock: 0.2f)));
        world.Events.Add(SplitScourgeHitsAt, SplitScourge);
        world.Events.Add(VenomousScourgeAt, VenomousScourge);
        world.Events.Add(BossReturnsAt, () =>
        {
            boss?.SetModelState(ModelState.Normal);
            boss?.SetTargetable(true);
        });
        world.Events.Add(FixerCastAt, () => boss?.Cast(Actions.TheFixer));
        world.Events.Add(EndsAt, () => { });
    }

    public void OnActionResolved(ActionResolved resolved)
        => Towers.ResolveTower(world, resolved, ActionId.MetamitosisTower, Actions.UnmitigatedExplosion, 1);

    private void SpawnBoss()
    {
        boss = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP1,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(BossHome, 0f)));
    }

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private void EachAlive(Action<SimCharacter> act) => AliveMembers().ForEach(act);

    private static Vector3 ToWorld(Vector2 p) => new(p.X, 0f, p.Y);

    private void GiveGrowths()
    {
        foreach (var (role, growth) in state.Growths)
        {
            if (Alive(role) is not { } member) continue;
            member.AddStatus(StatusId.MitoticPhase, MitoticPhaseLasts);
            member.AddStatusParam(StatusId.DirectedGrotesquerie, M12sAct3State.GrowthParam(growth), LysisAt - GrowthAt);
        }
    }

    private void SnakesBurrow(Snake snake)
    {
        boss?.CastFromHelper(Actions.GrandEntrance with { ActionId = snake.AppearId }, at: new Placement(ToWorld(snake.Appear), snake.Rotation));
        boss?.CastFromHelper(Actions.GrandEntrance, at: new Placement(ToWorld(snake.Leave), 0f));
    }

    private void BringDownTheHouse()
    {
        boss?.Cast(ActionId.BringDownTheHouseBoss, animationLock: 0.2f);
        foreach (var (id, at) in state.CardinalPlatforms ? CardinalSlabs : IntercardinalSlabs)
            boss?.CastFromHelper(Actions.BringDownTheHouseLarge with { ActionId = id }, at: new Placement(ToWorld(at), 0f));
    }

    private void KeepOnlyPlatforms(Snake[] snakes)
    {
        foreach (var snake in snakes)
        {
            world.Map.AddEffect(MapEffect.SnakeGone, snake.AppearSlot);
            world.Map.AddEffect(MapEffect.SnakeGone, snake.LeaveSlot);
        }
        world.Map.AddEffect(state.CardinalPlatforms ? MapEffect.CardinalPlatforms : MapEffect.IntercardinalPlatforms, MapEffect.ArenaSlot);
        floor.Now = Act3Floor.Stage.Platforms;
        EachAlive(m => m.AddStatus(StatusId.FateOfTheWurm, FloorReturnsAt - PlatformsAt));
    }

    private void Metamitosis()
    {
        foreach (var (role, growth) in state.Growths)
        {
            if (Alive(role) is not { } member) continue;
            var from = new Vector2(member.Position.X, member.Position.Z);
            var tower = ToWorld(M12sAct3State.Projected(from, growth));
            boss?.CastFromHelper(ActionId.MetamitosisProjection, tower, at: new Placement(member.Position, 0f));
            boss?.CastFromHelper(Actions.MetamitosisTower, at: new Placement(tower, 0f));
        }
    }

    private void FloorReturns()
    {
        world.Map.AddEffect(state.CardinalPlatforms ? MapEffect.CardinalHoles : MapEffect.IntercardinalHoles, MapEffect.ArenaSlot);
        floor.Now = Act3Floor.Stage.Holes;
    }

    private void SpawnHeads()
    {
        foreach (var spot in HeadSpots(state.CardinalPlatforms))
        {
            var head = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.SnakeHead,
                NameId: BNpcNameId.LindwurmP1,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: spot));
            if (head != null) heads.Add(head);
        }
    }

    private void SplitScourge()
    {
        foreach (var head in heads)
        {
            if (world.Party.Find.Closest(head.Position) is not { } closest) continue;
            boss?.CastFromHelper(Actions.SplitScourge, closest, at: head.Placement());
        }
    }

    private void VenomousScourge()
    {
        var alive = AliveMembers();
        var west = alive.Where(m => m.Position.X < 0f).OrderBy(m => m.Position.X).Take(3);
        var east = alive.Where(m => m.Position.X > 0f).OrderByDescending(m => m.Position.X).Take(3);
        foreach (var member in west.Concat(east).ToList())
            boss?.CastFromHelper(Actions.VenomousScourge, member, at: new Placement(member.Position, 0f));
    }

    // The whole floor, then only the platforms, then the floor again with holes where the other four
    // squares fell.
    public sealed class Act3Floor(M12sAct3State state) : IArenaShape
    {
        public enum Stage { Whole, Platforms, Holes }

        public Stage Now { get; set; } = Stage.Whole;

        public bool IsOutside(Vector3 p) => !IsFloor(state, Now, new Vector2(p.X, p.Z));

        public static bool IsFloor(M12sAct3State state, Stage stage, Vector2 p)
        {
            if (MathF.Abs(p.X) > M12sConstants.Geometry.P1HalfWidth || MathF.Abs(p.Y) > M12sConstants.Geometry.P1HalfDepth) return false;
            return stage switch
            {
                Stage.Platforms => M12sAct3State.OnSquare(state.Platforms, p),
                Stage.Holes => !M12sAct3State.OnSquare(state.Holes, p),
                _ => true,
            };
        }
    }
}
