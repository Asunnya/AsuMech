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

namespace AnoMech.Scenarios.M11s.Ecliptic;

// From the Crown after the second Orbital Omen to the enrage: Ecliptic Stampede's Mammoth Meteors,
// Atomic Impact's puddles on two marked players while the rest bait meteors round the boss, one-
// and two-player towers, Meteowrath tethers from the cardinals, Two-way Fireball's line stacks,
// Crown of Arcadia and the three Heartbreak Kick towers. The kick and the enrage were never reached
// in the logs.
public sealed class M11sEclipticScenario : IScenario
{
    public string Name => "Ecliptic Stampede to Heartbreak Kick";
    public IPhase Phase => M11sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M11sEclipticAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M11sEclipticSettingsWindow settingsWindow = new();

    public static readonly float[] CrownCastAt = [4.39f, 62.06f];
    public const float StampedeCastAt = 16.78f;
    public const float AtomicMarkedAt = 22.89f;
    public const float MammothCastAt = 22.94f;
    public static readonly float[] AtomicHitAt = [28.95f, 31.98f, 34.97f, 37.95f, 40.98f, 43.95f];
    public static readonly float[] MeteorBaitAt = [28.95f, 30.96f, 32.96f, 34.97f, 36.97f, 38.98f];
    public const float MeteorResolves = 3f;
    public const float TowerCastAt = 39.96f;
    public const float TowerResolves = 5f;
    public const float ShadesShowAt = 44.58f;
    public const float TetherAt = 47.38f;
    public const float FireballCastAt = 51.96f;
    public const float WrathAt = 54.5f;
    public const float FireballLinesAt = 58.73f;
    public static readonly float[] KickCastAt = [75f, 95f, 115f];
    public static readonly int[] KickHits = [5, 6, 7];
    public const float FirstKickAfter = 6.2f;
    public const float KickEvery = 2f;
    public const float HeartbreakerCastAt = 139f;

    public const float PuddleRadius = 5f;
    private const float PuddleShowsAfter = 0.76f;
    private const ushort PuddleBurning = 1;
    // UNVERIFIED: how soon a puddle starts burning and how long standing in it kills.
    private const float PuddleArms = 1f;
    private const float PuddleKills = 1f;
    public const float TowerRadius = 4f;

    private SimWorld world = null!;
    private M11sEclipticState state = null!;
    private SimEnemy? tyrant;
    private readonly SimEnemy?[] mammothHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] atomicHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] meteorHelpers = new SimEnemy?[36];
    private readonly SimEnemy?[] towerHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] shades = new SimEnemy?[4];
    private readonly SimEnemy?[] lineHelpers = new SimEnemy?[2];
    private SimEnemy? kickHelper;
    private readonly List<SimCharacter> marked = [];
    private readonly List<(Vector3 At, float ArmsAt)> puddles = [];
    private readonly List<SimEventObject> puddleObjects = [];
    private readonly Dictionary<SimCharacter, float> burning = [];
    private readonly List<SimCharacter>[] soakers = [[], [], [], []];
    private readonly List<(SimEnemy Shade, SimCharacter Target, SimTether? Line)> tethers = [];

    public IReadOnlyList<SimCharacter> Marked => marked;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M11sEclipticState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        marked.Clear();
        puddles.Clear();
        puddleObjects.Clear();
        burning.Clear();
        foreach (var s in soakers) s.Clear();
        tethers.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M11sEclipticState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(4f, TankTheBoss);
        foreach (var at in CrownCastAt)
            world.Events.Add(at, () => tyrant?.Cast(Actions.CrownOfArcadia));
        world.Events.Add(StampedeCastAt, HoldTheBoss);
        world.Events.Add(StampedeCastAt, () => tyrant?.Cast(ActionId.EclipticStampede, animationLock: 0.2f));
        world.Events.Add(AtomicMarkedAt, MarkAtomicImpact);
        world.Events.Add(MammothCastAt, MammothMeteors);
        foreach (var at in AtomicHitAt)
            world.Events.Add(at, AtomicImpact);
        for (var w = 0; w < MeteorBaitAt.Length; w++)
        {
            var wave = w;
            world.Events.Add(MeteorBaitAt[wave], () => BaitMeteors(wave));
        }
        world.Events.Add(TowerCastAt, CastTowers);
        world.Events.Add(TowerCastAt + TowerResolves - 0.05f, CheckTowers);
        world.Events.Add(ShadesShowAt, () => ShowShades(MapEffect.PortalShows));
        world.Events.Add(TetherAt, TetherTowers);
        world.Events.Add(FireballCastAt, () => tyrant?.Cast(ActionId.TwoWayFireball, animationLock: 0.2f));
        world.Events.Add(WrathAt, Meteowrath);
        world.Events.Add(FireballLinesAt, FireballLines);
        world.Events.Add(FireballLinesAt + 0.5f, () => ShowShades(MapEffect.PortalOff));
        world.Events.Add(CrownCastAt[1], ClearPuddles);
        for (var k = 0; k < KickCastAt.Length; k++)
        {
            var kick = k;
            world.Events.Add(KickCastAt[k], () => HeartbreakKick(kick));
            for (var h = 0; h < KickHits[k]; h++)
                world.Events.Add(KickCastAt[k] + FirstKickAfter + h * KickEvery, () => kickHelper?.Cast(Actions.HeartbreakKickTower));
        }
        world.Events.Add(HeartbreakerCastAt, () => tyrant?.Cast(ActionId.Heartbreaker, animationLock: 0.2f));
    }

    public void Tick(float delta, float elapsed)
    {
        foreach (var member in AliveMembers())
        {
            if (!puddles.Any(p => elapsed >= p.ArmsAt && Vector3.Distance(p.At, member.Position) < PuddleRadius))
            {
                burning.Remove(member);
                continue;
            }
            var burnt = burning.GetValueOrDefault(member) + delta;
            burning[member] = burnt;
            if (burnt >= PuddleKills) member.Die(ActionId.AtomicImpact, "stood in an Atomic Impact puddle");
        }
        for (var i = 0; i < tethers.Count; i++)
        {
            var (shade, target, line) = tethers[i];
            if (line == null || !target.IsAlive()) continue;
            var wanted = Vector3.Distance(shade.Position, target.Position) >= Actions.EclipticWrathStretch ? TetherId.MeteowrathStretched : TetherId.MeteowrathSlack;
            if (line.TetherId == wanted) continue;
            line.Despawn();
            tethers[i] = (shade, target, world.Tether(shade, target, wanted));
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
        for (var i = 0; i < mammothHelpers.Length; i++) mammothHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < atomicHelpers.Length; i++) atomicHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < meteorHelpers.Length; i++) meteorHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < towerHelpers.Length; i++) towerHelpers[i] = SpawnHelper(new Placement(M11sEclipticState.Intercard(i, M11sEclipticState.TowerOffset), 0f));
        for (var i = 0; i < lineHelpers.Length; i++) lineHelpers[i] = SpawnHelper(default);
        kickHelper = SpawnHelper(new Placement(Vector3.Zero, 0f));
        for (var side = 0; side < shades.Length; side++)
        {
            var spot = M11sEclipticState.Cardinal(side, M11sEclipticState.ShadeDistance);
            shades[side] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.TyrantShade,
                NameId: BNpcNameId.Tyrant,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(spot, M11sTrophyWeapons.Bearing(spot, Vector3.Zero))));
            shades[side]?.SetVisible(false);
        }
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

    // The two standing furthest from the boss; ties go by the rolled priority.
    private void MarkAtomicImpact()
    {
        marked.Clear();
        var boss = tyrant?.Position ?? Vector3.Zero;
        var picked = Enum.GetValues<PartyRole>()
            .Select(r => (Role: r, Member: Alive(r)))
            .Where(x => x.Member != null)
            .OrderByDescending(x => MathF.Round(Vector3.Distance(x.Member!.Position, boss)))
            .ThenBy(x => state.MarkPriority.ToList().IndexOf(x.Role))
            .Take(2);
        foreach (var (_, member) in picked)
        {
            member!.AttachLockonVfx(LockonId.AtomicImpact);
            marked.Add(member);
        }
    }

    private void MammothMeteors()
    {
        var i = 0;
        foreach (var spot in state.MammothSpots())
        {
            mammothHelpers[i]?.SetPosition(new Placement(spot, 0f));
            mammothHelpers[i++]?.Cast(Actions.MammothMeteor);
        }
    }

    private void AtomicImpact()
    {
        for (var i = 0; i < marked.Count && i < atomicHelpers.Length; i++)
        {
            if (!marked[i].IsAlive()) continue;
            var at = marked[i].Position;
            atomicHelpers[i]?.SetPosition(new Placement(at, 0f));
            atomicHelpers[i]?.Cast(Actions.AtomicImpact, marked[i]);
            puddles.Add((at, world.Events.Elapsed + PuddleArms));
            world.Events.Add(PuddleShowsAfter, () => ShowPuddle(at));
        }
    }

    private void ShowPuddle(Vector3 at)
    {
        if (world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.AtomicPuddle, Placement = new Placement(at, 0f), TimelineState = PuddleBurning }) is { } puddle)
            puddleObjects.Add(puddle);
    }

    // UNVERIFIED: the puddles outlived every logged pull; they go with the next Crown here.
    private void ClearPuddles()
    {
        puddles.Clear();
        foreach (var puddle in puddleObjects) puddle.Despawn();
        puddleObjects.Clear();
    }

    // UNVERIFIED: the tower's look; it borrows the pair tower's omen.
    private void HeartbreakKick(int kick)
    {
        tyrant?.Cast(ActionId.HeartbreakKick, animationLock: 0.2f);
        var lastHit = FirstKickAfter + (KickHits[kick] - 1) * KickEvery;
        world.SpawnActionOmen(ActionId.WeightyImpact, Vector3.Zero, 0f, lastHit + 0.3f);
    }

    private void BaitMeteors(int wave)
    {
        var i = 0;
        foreach (var member in AliveMembers().Where(m => !marked.Contains(m)))
        {
            if (i >= 6) break;
            var helper = meteorHelpers[wave * 6 + i++];
            helper?.SetPosition(new Placement(member.Position, 0f));
            helper?.Cast(Actions.EclipticMajesticMeteor, member.Position);
        }
    }

    private void CastTowers()
    {
        for (var i = 0; i < towerHelpers.Length; i++)
            towerHelpers[i]?.Cast(state.IsSingleTower(i) ? Actions.EclipticCosmicKiss : Actions.WeightyImpact);
    }

    // A one-player tower takes exactly one, a pair tower two; otherwise it goes off on everyone.
    private void CheckTowers()
    {
        for (var i = 0; i < towerHelpers.Length; i++)
        {
            var spot = M11sEclipticState.Intercard(i, M11sEclipticState.TowerOffset);
            soakers[i].Clear();
            soakers[i].AddRange(Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().Where(m => Vector3.Distance(m.Position, spot) <= TowerRadius));
            if (soakers[i].Count == (state.IsSingleTower(i) ? 1 : 2)) continue;
            var tower = towerHelpers[i];
            world.Events.Add(0.1f, () => tower?.Cast(Actions.EclipticUnmitigatedExplosion));
        }
    }

    private void ShowShades(uint effect)
    {
        for (var side = 0; side < 4; side++) world.Map.AddEffect(effect, (byte)(MapEffect.EclipticShadeSlot + side));
    }

    private void TetherTowers()
    {
        var pairIndex = 0;
        for (var i = 0; i < soakers.Length; i++)
        {
            var inTower = soakers[i];
            if (inTower.Count == 0) continue;
            SimCharacter target;
            if (state.IsSingleTower(i)) target = inTower[0];
            else
            {
                var first = state.PairTetherFirst[Math.Min(pairIndex++, 1)];
                target = inTower.Count > 1 && !first ? inTower[1] : inTower[0];
            }
            if (shades[state.ShadeForTower[i]] is not { } shade || !target.IsAlive()) continue;
            shade.SetVisible(true);
            tethers.Add((shade, target, world.Tether(shade, target, TetherId.MeteowrathSlack)));
        }
    }

    private void Meteowrath()
    {
        foreach (var (shade, target, line) in tethers)
        {
            line?.Despawn();
            if (target.IsAlive()) shade.Cast(Actions.EclipticMeteowrath, target);
            world.Events.Add(0.8f, () => shade.SetVisible(false));
        }
        tethers.Clear();
    }

    // Two lines from the boss at the two nearest players.
    private void FireballLines()
    {
        if (tyrant is not { } boss) return;
        var nearest = AliveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).Take(2).ToList();
        for (var i = 0; i < nearest.Count; i++)
        {
            lineHelpers[i]?.SetPosition(boss.Placement());
            lineHelpers[i]?.Cast(Actions.TwoWayFireballLine, nearest[i]);
        }
    }
}
