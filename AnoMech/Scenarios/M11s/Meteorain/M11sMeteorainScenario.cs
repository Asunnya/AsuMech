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
using static AnoMech.Scenarios.M11s.M11sConstants;
using Actions = AnoMech.Scenarios.M11s.M11sActions;

namespace AnoMech.Scenarios.M11s.Meteorain;

// Meteorain: three pairs of meteors baited on whoever stands nearest the boss, the shades'
// tethers on the two meteors nearest it, four Fearsome Fireball lines (the first on the tanks, the
// rest blown into a meteor that then explodes), and Triple Tyrannhilation's three shockwaves that
// only the meteors still standing hide the party from.
public sealed class M11sMeteorainScenario : IScenario
{
    public string Name => "Meteorain to Triple Tyrannhilation";
    public IPhase Phase => M11sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M11sMeteorainAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M11sMeteorainSettingsWindow settingsWindow = new();

    public const float MeteorainCastAt = 6.19f;
    public static readonly float[] MeteorMarkedAt = [12.3f, 22.32f, 32.33f];
    public const float MeteorFallsAfter = 8.06f;
    public static readonly float[] FireballCastAt = [13.86f, 23.79f, 33.76f, 43.73f];
    public const float FireballLineAfter = 5.3f;
    public const float TetherAfterFall = 2.35f;
    public const float FatalityAfterFall = 7.97f;
    public const float MeteorExplodesAfter = 0.8f;
    public const float MeteorExplosionResolves = 3f;
    public const float TyrannhilationCastAt = 54.68f;
    public static readonly float[] ShockwaveAt = [62.83f, 64.39f, 65.95f];
    private const float MeteorBreaksAfter = 0.49f;
    private const float ShadeShowsBeforeTether = 1.02f;
    private const float ShadeOffAfter = 7.04f;
    private static readonly float[] AutoAttackAt = [1f, 4f];

    // UNVERIFIED: the meteor's size for blocking lines and sight.
    public const float MeteorRadius = 2f;
    public const float FireballHalfWidth = 3f;
    private const float GrabWidth = 1.5f;

    private SimWorld world = null!;
    private M11sMeteorainState state = null!;
    private SimEnemy? tyrant;
    private readonly SimEnemy?[] meteors = new SimEnemy?[2 * M11sMeteorainState.MeteorSets];
    private readonly Dictionary<SimEnemy, Vector3> meteorAt = [];
    private readonly SimEnemy?[] shades = new SimEnemy?[4];
    private readonly SimEnemy?[] lineHelpers = new SimEnemy?[M11sMeteorainState.Fireballs];
    private readonly SimEnemy?[] shockHelpers = new SimEnemy?[3];
    private SimEnemy? wipeHelper;
    private readonly List<SimCharacter> marked = [];
    private readonly List<(SimEnemy Shade, SimCharacter Target, SimTether? Tether)> tethers = [];
    private int nextMeteor;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M11sMeteorainState(world.Rng, settingsWindow.Overrides);
        meteorAt.Clear();
        marked.Clear();
        tethers.Clear();
        nextMeteor = 0;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M11sMeteorainState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(4f, TankTheBoss);
        foreach (var at in AutoAttackAt)
            world.Events.Add(at, AutoAttack);
        world.Events.Add(MeteorainCastAt, HoldTheBoss);
        world.Events.Add(MeteorainCastAt, () => tyrant?.Cast(ActionId.Meteorain, animationLock: 0.2f));
        for (var s = 0; s < M11sMeteorainState.MeteorSets; s++)
        {
            var set = s;
            var fallAt = MeteorMarkedAt[set] + MeteorFallsAfter;
            world.Events.Add(MeteorMarkedAt[set], MarkMeteors);
            world.Events.Add(fallAt, DropMeteors);
            world.Events.Add(fallAt + TetherAfterFall - ShadeShowsBeforeTether, () => ShowShades(set, MapEffect.PortalShows));
            world.Events.Add(fallAt + TetherAfterFall, () => TetherMeteors(set));
            world.Events.Add(fallAt + FatalityAfterFall, ForegoneFatality);
            world.Events.Add(fallAt + TetherAfterFall - ShadeShowsBeforeTether + ShadeOffAfter, () => ShowShades(set, MapEffect.PortalOff));
        }
        for (var f = 0; f < M11sMeteorainState.Fireballs; f++)
        {
            var n = f;
            world.Events.Add(FireballCastAt[n], () => FearsomeFireball(n));
        }
        world.Events.Add(TyrannhilationCastAt, () => tyrant?.Cast(ActionId.TripleTyrannhilation, animationLock: 0.2f));
        for (var i = 0; i < ShockwaveAt.Length; i++)
        {
            var n = i;
            world.Events.Add(ShockwaveAt[n], () => Shockwave(n));
            world.Events.Add(ShockwaveAt[n] + MeteorBreaksAfter, BreakNearestMeteor);
        }
    }

    public void Tick(float delta, float elapsed)
    {
        for (var i = 0; i < tethers.Count; i++)
        {
            var (shade, target, tether) = tethers[i];
            var grab = AliveMembers()
                .Where(m => !ReferenceEquals(m, target) && OnSegment(shade.Position, target.Position, m.Position) < GrabWidth)
                .OrderBy(m => Vector3.DistanceSquared(m.Position, shade.Position))
                .FirstOrDefault();
            if (grab == null || Vector3.DistanceSquared(grab.Position, shade.Position) >= Vector3.DistanceSquared(target.Position, shade.Position)) continue;
            tether?.Despawn();
            tethers[i] = (shade, grab, world.Tether(shade, grab, TetherId.ForegoneFatality));
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
        for (var i = 0; i < meteors.Length; i++)
        {
            meteors[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Comet,
                NameId: BNpcNameId.Comet,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(Vector3.Zero, 0f)));
            meteors[i]?.SetVisible(false);
        }
        for (var side = 0; side < shades.Length; side++)
        {
            var spot = M11sMeteorainState.ShadeSpot(side);
            shades[side] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.TyrantShade,
                NameId: BNpcNameId.Tyrant,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(spot, M11sTrophyWeapons.Bearing(spot, Vector3.Zero))));
            shades[side]?.SetVisible(false);
        }
        for (var i = 0; i < lineHelpers.Length; i++) lineHelpers[i] = SpawnHelper();
        for (var i = 0; i < shockHelpers.Length; i++) shockHelpers[i] = SpawnHelper();
        wipeHelper = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Tyrant,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void TankTheBoss() => tyrant?.SetTarget(Alive(PartyRole.MainTank));

    private void HoldTheBoss() => tyrant?.Follow();

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private void AutoAttack()
    {
        if (Alive(PartyRole.MainTank) is { } tank)
            tyrant?.Cast(ActionId.Attack, tank, animationLock: 0.2f);
    }

    private static float OnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        var ab = new Vector2(b.X - a.X, b.Z - a.Z);
        var ap = new Vector2(p.X - a.X, p.Z - a.Z);
        var t = ab.LengthSquared() < 0.01f ? 0f : Math.Clamp(Vector2.Dot(ap, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(ap, t * ab);
    }

    private void MarkMeteors()
    {
        marked.Clear();
        var boss = tyrant?.Position ?? Vector3.Zero;
        foreach (var member in AliveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, boss)).Take(2))
        {
            member.AttachLockonVfx(LockonId.Meteor);
            marked.Add(member);
        }
    }

    private void DropMeteors()
    {
        foreach (var member in marked.Where(m => m.IsAlive()))
        {
            if (nextMeteor >= meteors.Length || meteors[nextMeteor] is not { } meteor) break;
            nextMeteor++;
            var at = member.Position;
            if (state.Meteors.Any(m => Vector3.Distance(m, at) < Actions.MeteorsTooClose))
                wipeHelper?.Cast(Actions.UnmitigatedExplosion);
            meteor.SetPosition(new Placement(at, 0f));
            meteor.SetVisible(true);
            meteor.Cast(Actions.CosmicKiss);
            meteorAt[meteor] = at;
            state.Meteors.Add(at);
        }
        marked.Clear();
    }

    private SimEnemy? MeteorAt(Vector3 at) => meteorAt.FirstOrDefault(kv => kv.Value == at).Key;

    private void ShowShades(int set, uint effect)
    {
        var (a, b) = state.ShadeSides[set];
        world.Map.AddEffect(effect, (byte)(MapEffect.ShadeSlot + a));
        world.Map.AddEffect(effect, (byte)(MapEffect.ShadeSlot + b));
    }

    private void TetherMeteors(int set)
    {
        tethers.Clear();
        foreach (var (side, at) in state.TetheredMeteors(set))
        {
            if (shades[side] is not { } shade || MeteorAt(at) is not { } meteor) continue;
            shade.SetVisible(true);
            tethers.Add((shade, meteor, world.Tether(shade, meteor, TetherId.ForegoneFatality)));
        }
    }

    private void ForegoneFatality()
    {
        foreach (var (shade, target, tether) in tethers)
        {
            tether?.Despawn();
            if (target is SimEnemy meteor && meteorAt.ContainsKey(meteor))
            {
                shade.Cast(ActionId.ForegoneFatality, meteor, animationLock: 0.2f);
                ExplodeMeteor(meteor);
            }
            else if (target.IsAlive())
                shade.Cast(Actions.ForegoneFatality, target);
            world.Events.Add(0.6f, () => shade.SetVisible(false));
        }
        tethers.Clear();
    }

    // The boss wears the marker, pointed at whoever the line will hit.
    private void FearsomeFireball(int n)
    {
        if (tyrant is not { } boss) return;
        boss.Cast(ActionId.FearsomeFireball, animationLock: 0.2f);
        var target = state.FireballPriority.Select(Alive).OfType<SimCharacter>().FirstOrDefault(m => !marked.Contains(m)) ?? AliveMembers().FirstOrDefault();
        if (target == null) return;
        boss.AttachLockonVfx(LockonId.FireballLine, target);
        world.Events.Add(FireballLineAfter, () => FireballLine(n, target));
    }

    private void FireballLine(int n, SimCharacter target)
    {
        if (tyrant is not { } boss) return;
        if (!target.IsAlive() && AliveMembers().FirstOrDefault() is { } anyone) target = anyone;
        boss.Face(target);
        var origin = new Vector2(boss.Position.X, boss.Position.Z);
        var dir = Vector2.Normalize(new Vector2(target.Position.X, target.Position.Z) - origin);
        var blocking = state.Meteors
            .Select(m => (At: m, Along: Vector2.Dot(new Vector2(m.X, m.Z) - origin, dir)))
            .Where(m => m.Along > 0f && MathF.Abs(Cross(new Vector2(m.At.X, m.At.Z) - origin, dir)) < FireballHalfWidth + MeteorRadius)
            .OrderBy(m => m.Along)
            .Select(m => ((Vector3 At, float Along)?)m)
            .FirstOrDefault();
        var action = n == 0 ? Actions.FearsomeFireballFirst : Actions.FearsomeFireball;
        if (blocking is { } meteor)
        {
            action = action with
            {
                Area = action.Area with { AdjustTargets = (_, hits) => hits.Where(h => Vector2.Dot(new Vector2(h.Position.X, h.Position.Z) - origin, dir) < meteor.Along).ToList() },
                Effects = [EnemyActionEffects.Damage()],
            };
            if (MeteorAt(meteor.At) is { } hit) world.Events.Add(MeteorExplodesAfter, () => ExplodeMeteor(hit));
        }
        lineHelpers[n]?.SetPosition(boss.Placement());
        lineHelpers[n]?.Cast(action, target);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private void ExplodeMeteor(SimEnemy meteor)
    {
        if (!meteorAt.Remove(meteor, out var at)) return;
        state.Meteors.Remove(at);
        meteor.SetAnimationState(1, 1);
        meteor.Cast(Actions.CometExplosion);
        world.Events.Add(MeteorExplosionResolves + 2f, () => meteor.SetVisible(false));
    }

    private void Shockwave(int n)
    {
        if (tyrant is not { } boss) return;
        var origin = new Vector2(boss.Position.X, boss.Position.Z);
        var standing = state.Meteors.Select(m => new Vector2(m.X, m.Z)).ToList();
        var action = Actions.Shockwave with
        {
            Area = Actions.Shockwave.Area with { AdjustTargets = (_, hits) => hits.Where(h => !Hidden(origin, new Vector2(h.Position.X, h.Position.Z), standing)).ToList() },
        };
        shockHelpers[n]?.SetPosition(boss.Placement());
        shockHelpers[n]?.Cast(action);
    }

    public static bool Hidden(Vector2 from, Vector2 to, IEnumerable<Vector2> meteors)
    {
        var d = to - from;
        var length = d.Length();
        if (length < 0.01f) return false;
        d /= length;
        return meteors.Any(m =>
        {
            var along = Vector2.Dot(m - from, d);
            return along > 0f && along < length && MathF.Abs(Cross(m - from, d)) < MeteorRadius;
        });
    }

    private void BreakNearestMeteor()
    {
        if (state.Meteors.Count == 0) return;
        var nearest = state.Meteors.OrderBy(m => m.LengthSquared()).First();
        state.Meteors.Remove(nearest);
        if (MeteorAt(nearest) is not { } meteor) return;
        meteorAt.Remove(meteor);
        meteor.SetAnimationState(1, 1);
        world.Events.Add(2f, () => meteor.SetVisible(false));
    }
}
