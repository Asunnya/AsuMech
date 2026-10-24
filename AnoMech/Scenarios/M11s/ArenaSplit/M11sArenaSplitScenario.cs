using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M11s.M11sConstants;
using Actions = AnoMech.Scenarios.M11s.M11sActions;

namespace AnoMech.Scenarios.M11s.ArenaSplit;

// Arena Split: Flatliner throws the party onto two platforms, then twice towers that knock back,
// Meteowrath tethers to stretch across the arena, Fire Breath on the rest, chasing puddles and the
// portals' meteor lines; Massive Meteor's stacks, the last towers and Arcadion Avalanche, which
// leaves one corner of one platform, until Crown of Arcadia restores the arena.
public sealed class M11sArenaSplitScenario : IScenario
{
    public string Name => "Arena Split";
    public IPhase Phase => M11sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M11sArenaSplitAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M11sArenaSplitSettingsWindow settingsWindow = new();

    public const float SplitStartsAt = 5.13f;
    public const float FlatlinerCastAt = 5.23f;
    public const float FlatlinerResolves = 6f;
    // Its hitbox doubles to 10 while it stands in the fallen middle, so both platforms reach it.
    public const float BossGrowsAt = 10.93f;
    public const float MiddleFallsAt = 11.31f;
    public const float MajesticMeteorCastAt = 20.37f;
    public static readonly float[] RoundShowsAt = [25.41f, 57.43f];
    public static readonly float[] TetherAt = [26.43f, 58.48f];
    public static readonly float[] TowerCastAt = [28.52f, 60.55f, 98.6f];
    public const float TowerResolves = 10f;
    public static readonly float[] PreyAt = [44.42f, 76.52f];
    public static readonly float[] BreathCastAt = [44.5f, 76.61f];
    public static readonly float[] BreathHitsAt = [53.27f, 85.39f];
    public static readonly float[][] PuddleAt = [[46.52f, 48.52f, 50.53f], [78.57f, 80.57f, 82.57f]];
    public const float PuddleResolves = 3f;
    public static readonly float[] RoundEndsAt = [54.32f, 86.44f];
    public const float MassiveMarkedAt = 89.69f;
    public const float MassiveCastAt = 89.76f;
    public static readonly float[] MassiveHitAt = [95.9f, 97.37f, 98.84f, 100.31f, 101.78f];
    public const float LastTowersShowAt = 95.48f;
    public const float AvalancheCastAt = 103.68f;
    public const float AvalancheResolves = 15.5f;
    public const float CrownCastAt = 124.81f;
    public const float ArenaRestoredAt = 125.79f;
    public const float CrownResolves = 5f;
    public const float BossShrinksAt = 130.57f;
    private const float TowerMarkBefore = 0.08f;
    private const float KnockbackFlight = 1.6f;

    public static readonly Vector3[] TowerSpots = [new(-16f, 0f, -11f), new(-16f, 0f, 11f), new(16f, 0f, -11f), new(16f, 0f, 11f)];
    public const float TowerRadius = 4f;

    private static readonly uint[] AvalancheCasts = [0xB44A, 0xB44C, 0xB44E, 0xB450];
    private static readonly uint[] AvalancheSmashes = [0xB44B, 0xB44D, 0xB44F, 0xB451];
    public static readonly (Vector3 At, float Heading)[] SmashFrom =
    [
        (new(-4.33f, 0f, -2.5f), MathF.PI / 3f),
        (new(-4.33f, 0f, 2.5f), 2f * MathF.PI / 3f),
        (new(4.33f, 0f, 2.5f), -2f * MathF.PI / 3f),
        (new(4.33f, 0f, -2.5f), -MathF.PI / 3f),
    ];

    private SimWorld world = null!;
    private M11sArenaSplitState state = null!;
    private SplitArena arena = null!;
    private SimEnemy? tyrant;
    private readonly SimEnemy?[] shades = new SimEnemy?[4];
    private readonly SimEnemy?[] towerHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] puddleHelpers = new SimEnemy?[24];
    private readonly SimEnemy?[] breathHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] lineHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] stackHelpers = new SimEnemy?[2];
    private SimEnemy? knockbackHelper;
    private SimEnemy? smashHelper;
    private readonly List<(SimEnemy Shade, WrathTether Tether, SimTether? Line)> tethers = [];
    private readonly int[] towerSoakers = new int[4];
    private bool towersUp;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M11sArenaSplitState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        arena = new SplitArena(() => world.Events.Elapsed);
        tethers.Clear();
        towersUp = false;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M11sArenaSplitState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(arena, "Fell off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(4f, TankTheBoss);
        world.Events.Add(SplitStartsAt, () => world.Map.AddEffect(MapEffect.ArenaSplits, MapEffect.ArenaSlot));
        world.Events.Add(FlatlinerCastAt, HoldTheBoss);
        world.Events.Add(FlatlinerCastAt, () =>
        {
            tyrant?.Cast(ActionId.Flatliner, animationLock: 0.2f);
            knockbackHelper?.Cast(Actions.FlatlinerKnockback);
        });
        world.Events.Add(FlatlinerCastAt + FlatlinerResolves, () => arena.AirborneUntil(FlatlinerCastAt + FlatlinerResolves + KnockbackFlight));
        world.Events.Add(BossGrowsAt, () => tyrant?.SetModelScale(2f));
        world.Events.Add(MiddleFallsAt, () =>
        {
            world.Map.AddEffect(MapEffect.MiddleFalls, MapEffect.ArenaSlot);
            arena.Split();
        });
        world.Events.Add(MajesticMeteorCastAt, () => tyrant?.Cast(ActionId.MajesticMeteor, animationLock: 0.2f));
        for (var r = 0; r < M11sArenaSplitState.Rounds; r++)
        {
            var round = r;
            world.Events.Add(RoundShowsAt[round], () => ShowRound(round, MapEffect.PortalShows));
            world.Events.Add(TetherAt[round], () => TetherRound(round));
            world.Events.Add(TetherAt[round], () => FirePortals(round));
            AddTowers(TowerCastAt[round]);
            world.Events.Add(PreyAt[round], () => MarkPrey(round));
            world.Events.Add(BreathCastAt[round], () => tyrant?.Cast(ActionId.FireBreath, animationLock: 0.2f));
            for (var w = 0; w < 3; w++)
            {
                var wave = w;
                world.Events.Add(PuddleAt[round][wave], () => DropPuddles(wave));
            }
            world.Events.Add(BreathHitsAt[round], () => BreathAndWrath(round));
            world.Events.Add(RoundEndsAt[round], () => ShowRound(round, MapEffect.PortalOff));
        }
        world.Events.Add(MassiveMarkedAt, MarkMassiveMeteor);
        world.Events.Add(MassiveCastAt, () => tyrant?.Cast(ActionId.MassiveMeteor, animationLock: 0.2f));
        foreach (var at in MassiveHitAt)
            world.Events.Add(at, MassiveMeteorHit);
        world.Events.Add(LastTowersShowAt, () => ShowTowers(MapEffect.PortalShows));
        AddTowers(TowerCastAt[2]);
        world.Events.Add(AvalancheCastAt, ArcadionAvalanche);
        world.Events.Add(AvalancheCastAt + AvalancheResolves, () => arena.KeepOnly(state.AvalancheSafeSide));
        world.Events.Add(CrownCastAt, () => tyrant?.Cast(Actions.CrownOfArcadia));
        world.Events.Add(ArenaRestoredAt, () => world.Map.AddEffect(MapEffect.ArenaRestored, MapEffect.ArenaSlot));
        world.Events.Add(CrownCastAt + CrownResolves, arena.Restore);
        world.Events.Add(BossShrinksAt, () => tyrant?.SetModelScale(1f));
    }

    public void Tick(float delta, float elapsed)
    {
        if (towersUp) ShowTowerSoakers();
        for (var i = 0; i < tethers.Count; i++)
        {
            var (shade, tether, line) = tethers[i];
            if (Alive(tether.Role) is not { } target || line == null) continue;
            var wanted = Vector3.Distance(shade.Position, target.Position) >= Actions.MeteowrathStretch ? TetherId.MeteowrathStretched : TetherId.MeteowrathSlack;
            if (line.TetherId == wanted) continue;
            line.Despawn();
            tethers[i] = (shade, tether, world.Tether(shade, target, wanted));
        }
    }

    private void SpawnActors()
    {
        tyrant = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Tyrant,
            NameId: BNpcNameId.Tyrant,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        for (var i = 0; i < shades.Length; i++)
        {
            shades[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.TyrantShade,
                NameId: BNpcNameId.Tyrant,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(Vector3.Zero, 0f)));
            shades[i]?.SetVisible(false);
        }
        for (var i = 0; i < towerHelpers.Length; i++) towerHelpers[i] = SpawnHelper(new Placement(TowerSpots[i], 0f));
        for (var i = 0; i < puddleHelpers.Length; i++) puddleHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < breathHelpers.Length; i++) breathHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < lineHelpers.Length; i++) lineHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < stackHelpers.Length; i++) stackHelpers[i] = SpawnHelper(default);
        knockbackHelper = SpawnHelper(new Placement(Vector3.Zero, 0f));
        smashHelper = SpawnHelper(default);
    }

    private SimEnemy? SpawnHelper(Placement at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Tyrant,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: at));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void TankTheBoss() => tyrant?.SetTarget(Alive(PartyRole.MainTank));

    private void HoldTheBoss() => tyrant?.Follow();

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private static byte LineSlot(int x) => (byte)(MapEffect.MeteorainSlot + Array.IndexOf([-21, -11, 11, 21], x));

    private static byte ShadeSlot(WrathTether t)
        => (byte)(MapEffect.WrathShadeSlot + (t.Side > 0 ? 4 : 0) + Array.IndexOf([-15f, -5f, 5f, 15f], t.Z));

    private void ShowRound(int round, uint effect)
    {
        if (effect == MapEffect.PortalShows) ShowTowers(effect);
        foreach (var x in state.LineXs(round)) world.Map.AddEffect(effect, LineSlot(x));
        foreach (var t in state.Tethers[round]) world.Map.AddEffect(effect, ShadeSlot(t));
    }

    private void FirePortals(int round)
    {
        foreach (var x in state.LineXs(round)) world.Map.AddEffect(MapEffect.PortalFires, LineSlot(x));
    }

    private void ShowTowerSoakers()
    {
        for (var i = 0; i < TowerSpots.Length; i++)
        {
            var spot = TowerSpots[i];
            var soakers = Math.Min(2, AliveMembers().Count(m => Vector3.Distance(m.Position, spot) <= TowerRadius));
            if (soakers == towerSoakers[i]) continue;
            towerSoakers[i] = soakers;
            var effect = soakers switch { 0 => MapEffect.TowerEmptied, 1 => MapEffect.TowerOneIn, _ => MapEffect.TowerFilled };
            world.Map.AddEffect(effect, (byte)(MapEffect.TowerSlot + i));
        }
    }

    private void ShowTowers(uint effect)
    {
        if (effect == MapEffect.PortalShows)
        {
            Array.Clear(towerSoakers);
            towersUp = true;
        }
        for (var i = 0; i < TowerSpots.Length; i++) world.Map.AddEffect(effect, (byte)(MapEffect.TowerSlot + i));
    }

    private void AddTowers(float castAt)
    {
        world.Events.Add(castAt - TowerMarkBefore, () =>
        {
            for (var i = 0; i < TowerSpots.Length; i++) world.Map.AddEffect(MapEffect.TowerMarkShows, (byte)(MapEffect.TowerMarkSlot + i));
        });
        world.Events.Add(castAt, () => { foreach (var tower in towerHelpers) tower?.Cast(Actions.TowerExplosion); });
        world.Events.Add(castAt + TowerResolves - 0.05f, () => towersUp = false);
        world.Events.Add(castAt + TowerResolves - 0.05f, CheckTowerSoaks);
        world.Events.Add(castAt + TowerResolves, () => arena.AirborneUntil(castAt + TowerResolves + KnockbackFlight));
        world.Events.Add(castAt + TowerResolves + 0.1f, () => ShowTowers(MapEffect.PortalOff));
    }

    // Each tower takes exactly two; any other count sets it off on everyone.
    private void CheckTowerSoaks()
    {
        for (var i = 0; i < TowerSpots.Length; i++)
        {
            var spot = TowerSpots[i];
            var soakers = AliveMembers().Count(m => Vector3.Distance(m.Position, spot) <= TowerRadius);
            if (soakers == 2) continue;
            var tower = towerHelpers[i];
            world.Events.Add(0.1f, () => tower?.Cast(Actions.UnmitigatedTowerExplosion));
        }
    }

    private void TetherRound(int round)
    {
        foreach (var (_, _, line) in tethers) line?.Despawn();
        tethers.Clear();
        for (var i = 0; i < state.Tethers[round].Count && i < shades.Length; i++)
        {
            var tether = state.Tethers[round][i];
            if (shades[i] is not { } shade || Alive(tether.Role) is not { } target) continue;
            var spot = M11sArenaSplitState.ShadeSpot(tether);
            shade.SetPosition(new Placement(spot, M11sTrophyWeapons.Bearing(spot, new Vector3(0f, 0f, spot.Z))));
            shade.SetVisible(true);
            tethers.Add((shade, tether, world.Tether(shade, target, TetherId.MeteowrathSlack)));
        }
    }

    private void MarkPrey(int round)
    {
        foreach (var role in Enum.GetValues<PartyRole>().Where(r => state.TetherOf(round, r) == null))
            Alive(role)?.AttachLockonVfx(LockonId.Meteor);
    }

    private void DropPuddles(int wave)
    {
        var i = 0;
        foreach (var member in AliveMembers())
        {
            var helper = puddleHelpers[wave * 8 + i++];
            helper?.SetPosition(new Placement(member.Position, 0f));
            helper?.Cast(Actions.MajesticMeteorPuddle, member.Position);
        }
    }

    private void BreathAndWrath(int round)
    {
        var lineXs = state.LineXs(round);
        for (var i = 0; i < lineXs.Length; i++)
        {
            lineHelpers[i]?.SetPosition(new Placement(new Vector3(lineXs[i], 0f, -25f), 0f));
            lineHelpers[i]?.Cast(Actions.MajesticMeteorain);
        }
        var prey = Enum.GetValues<PartyRole>().Where(r => state.TetherOf(round, r) == null).Select(Alive).OfType<SimCharacter>().ToList();
        for (var i = 0; i < prey.Count && i < breathHelpers.Length; i++)
        {
            breathHelpers[i]?.SetPosition(new Placement(tyrant?.Position ?? Vector3.Zero, 0f));
            breathHelpers[i]?.Cast(Actions.FireBreathLine, prey[i]);
        }
        foreach (var (shade, tether, line) in tethers)
        {
            line?.Despawn();
            if (Alive(tether.Role) is { } target) shade.Cast(Actions.MajesticMeteowrath, target);
            world.Events.Add(0.8f, () => shade.SetVisible(false));
        }
        tethers.Clear();
    }

    private void MarkMassiveMeteor()
    {
        foreach (var healer in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer }.Select(Alive).OfType<SimCharacter>())
            healer.AttachLockonVfx(LockonId.MassiveMeteor);
    }

    private void MassiveMeteorHit()
    {
        PartyRole[] healers = [PartyRole.RegenHealer, PartyRole.ShieldHealer];
        for (var i = 0; i < healers.Length; i++)
        {
            if (Alive(healers[i]) is not { } healer) continue;
            stackHelpers[i]?.SetPosition(new Placement(healer.Position, 0f));
            stackHelpers[i]?.Cast(Actions.MassiveMeteorStack, healer);
        }
    }

    // The Tyrant faces the platform that falls; the smash leaves only one corner of the other.
    private void ArcadionAvalanche()
    {
        if (tyrant is not { } boss) return;
        boss.Face(new Vector3(-state.AvalancheSafeSide, 0f, 0f));
        boss.Cast(AvalancheCasts[state.Avalanche], animationLock: 0.2f);
        var (at, heading) = SmashFrom[state.Avalanche];
        smashHelper?.SetPosition(new Placement(at, heading));
        smashHelper?.Cast(Actions.ArcadionAvalancheSmash with { ActionId = AvalancheSmashes[state.Avalanche] });
    }

    public static bool InSmash(int avalanche, Vector2 p)
    {
        var (at, heading) = SmashFrom[avalanche];
        var d = p - new Vector2(at.X, at.Z);
        var forward = new Vector2(MathF.Sin(heading), MathF.Cos(heading));
        var along = Vector2.Dot(d, forward);
        var across = d.X * forward.Y - d.Y * forward.X;
        return along >= 0f && along <= 40f && MathF.Abs(across) <= 20f;
    }

    // The floor as it changes: the square, then two platforms, then one, then the square again.
    // While a knockback carries the party over the gap, only the outer walls count.
    private sealed class SplitArena(Func<float> now) : IArenaShape
    {
        private enum Floor { Square, Platforms, OnePlatform }

        private Floor floor = Floor.Square;
        private int keptSide;
        private float airborneUntil;

        public void Split() => floor = Floor.Platforms;
        public void KeepOnly(int side) { floor = Floor.OnePlatform; keptSide = side; }
        public void Restore() => floor = Floor.Square;
        public void AirborneUntil(float t) => airborneUntil = t;

        public bool IsOutside(Vector3 p)
        {
            if (floor == Floor.Square) return MathF.Abs(p.X) > Geometry.ArenaHalfWidth || MathF.Abs(p.Z) > Geometry.ArenaHalfWidth;
            if (MathF.Abs(p.Z) > M11sArenaSplitState.PlatformHalfLength || MathF.Abs(p.X) > M11sArenaSplitState.PlatformOuter) return true;
            if (now() < airborneUntil) return false;
            if (MathF.Abs(p.X) < M11sArenaSplitState.PlatformInner) return true;
            return floor == Floor.OnePlatform && MathF.Sign(p.X) != keptSide;
        }
    }
}
