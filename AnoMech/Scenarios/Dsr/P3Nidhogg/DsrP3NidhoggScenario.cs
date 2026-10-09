using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using AnoMech.Scenarios.Legacy;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

// From Nidhogg's arrival through Dive from Grace and Darkdragon Dive to the enrage. Nidhogg's HP
// is not simulated: the run ends before Revenge of the Horde lands.
public sealed class DsrP3NidhoggScenario : IScenario
{
    public string Name => "Nidhogg";
    public IPhase Phase => DsrZone.Nidhogg;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP3NidhoggAi()];

    private readonly DsrP3NidhoggStateOverrides overrides = new();
    public object SettingsOverrides => overrides;

    private const uint NidhoggMaxHp = 10000000;
    // UNVERIFIED: Nidhogg's hitbox.
    private const float NidhoggHitboxRadius = 5f;
    // UNVERIFIED: raidwide damage as a share of max HP.
    private const float FinalChorusDamage = 0.5f;
    private const float DiveFromGraceDamage = 0.3f;
    private const float DiveDamage = 0.3f;
    private const float TowerDamage = 0.3f;
    private const float EyeOfTheTyrantDamage = 0.3f;
    private const float AutoAttackDamage = 0.3f;
    private const float SoulTetherTankDamage = 0.5f;
    private const float AutoAttackHalfWidth = 1.5f;
    private const float AutoAttackRange = 60f;
    private const float DiveTowerRadius = 5f;
    private const float SoulTetherRadius = 5f;
    private const float FourTowerRadius = 11.31f;
    // UNVERIFIED: the donut's safe inner radius.
    private const float LashingWheelInnerRadius = 6f;
    // UNVERIFIED: Drachenlance's cone width.
    private const float DrachenlanceHalfAngle = MathF.PI / 4f;
    private const int EyeOfTheTyrantMinTargets = 4;
    private const float FireResistanceDownSeconds = 11.96f;
    private const float DiveVulnerabilitySeconds = 2.96f;
    private const float TowerVulnerabilitySeconds = 7f;
    private static readonly float[] ArrowSeconds = [9f, 19f, 30f];
    // Despawning an effect's caster cuts its VFX short.
    private const float EffectCarrierLinger = 3f;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private DsrP3NidhoggState state = null!;

    private SimEnemy? nidhogg;
    private readonly List<SimEnemy> divers = [];
    private readonly List<SimEnemy>[] diveTowerCasters = [[], [], []];
    private readonly List<(SimEnemy Caster, Vector3 At, int Needed)> fourTowers = [];
    private readonly List<SimEnemy> geirskogulCasters = [];
    private readonly SimTether?[] soulTethers = new SimTether?[2];
    private readonly bool[] soulTetherLocked = new bool[2];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new DsrP3NidhoggState(world.Rng, overrides);
        divers.Clear();
        foreach (var casters in diveTowerCasters) casters.Clear();
        fourTowers.Clear();
        geirskogulCasters.Clear();
        Array.Clear(soulTethers);
        Array.Clear(soulTetherLocked);

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP3NidhoggState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.ThordanArenaRadius, "Touched the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(NaurWaymarks));
        world.Events.Add(0f, SpawnNidhogg);
        world.Events.Add(0.09f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.NidhoggLayout));
        world.Events.Add(4.10f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.KnightsMap));
        world.Events.Add(5.13f, () => world.Map.AddEffect(ArenaDirector.NidhoggSlotOn, ArenaDirector.NidhoggSlot));
        world.Events.Add(6.96f, () => Raidwide(ActionId.FinalChorus, FinalChorusDamage));
        world.Events.Add(10.00f, NidhoggEngages);
        world.Events.Add(14.10f, AutoAttack);
        world.Events.Add(17.22f, AutoAttack);
        world.Events.Add(20.35f, AutoAttack);

        world.Events.Add(22.45f, () => nidhogg?.SetTarget(null, follow: false));
        world.Events.Add(22.45f, () => CastSelf(nidhogg, ActionId.DiveFromGrace, 4.7f));
        world.Events.Add(22.45f, MarkLines);
        world.Events.Add(27.45f, () => Raidwide(ActionId.DiveFromGrace, DiveFromGraceDamage));
        world.Events.Add(28.15f, GiveArrows);
        world.Events.Add(29.60f, () => CastWheels(0));
        world.Events.Add(37.19f, () => PlayEffect(nidhogg, WheelAction(0), 2.1f));
        world.Events.Add(37.28f, () => Dive(1));
        world.Events.Add(37.45f, () => EyeOfTheTyrant(0));
        world.Events.Add(40.89f, () => FirstWheel(0));
        world.Events.Add(41.38f, () => CastDiveTowers(1));
        world.Events.Add(43.88f, () => ResolveDiveTowers(1));
        world.Events.Add(44.01f, () => SecondWheel(0));
        world.Events.Add(46.51f, () => CastGeirskoguls(1));
        world.Events.Add(47.27f, () => Dive(2));
        world.Events.Add(50.97f, ResolveGeirskoguls);
        world.Events.Add(51.15f, () => CastWheels(1));
        world.Events.Add(51.38f, () => CastDiveTowers(2));
        world.Events.Add(53.88f, () => ResolveDiveTowers(2));
        world.Events.Add(56.50f, () => CastGeirskoguls(2));
        world.Events.Add(58.29f, () => Dive(3));
        world.Events.Add(58.74f, () => PlayEffect(nidhogg, WheelAction(1), 2.1f));
        world.Events.Add(59.01f, () => EyeOfTheTyrant(1));
        world.Events.Add(60.97f, ResolveGeirskoguls);
        world.Events.Add(62.40f, () => FirstWheel(1));
        world.Events.Add(62.40f, () => CastDiveTowers(3));
        world.Events.Add(64.85f, () => ResolveDiveTowers(3));
        world.Events.Add(65.52f, () => SecondWheel(1));
        world.Events.Add(67.48f, () => CastGeirskoguls(3));
        world.Events.Add(69.60f, ClearLines);
        world.Events.Add(69.60f, NidhoggEngages);
        world.Events.Add(69.67f, AutoAttack);
        world.Events.Add(71.98f, ResolveGeirskoguls);
        world.Events.Add(72.78f, AutoAttack);

        world.Events.Add(74.03f, CastDrachenlance);
        world.Events.Add(77.59f, ResolveDrachenlance);
        world.Events.Add(79.06f, CastFourTowers);
        world.Events.Add(83.96f, TetherSoulTethers);
        world.Events.Add(84.05f, ResolveFourTowers);
        world.Events.Add(86.68f, CastFourTowerGeirskoguls);
        world.Events.Add(91.10f, ResolveSoulTethers);
        world.Events.Add(91.14f, ResolveGeirskoguls);
        foreach (var auto in new[] { 96.23f, 99.35f, 102.47f, 105.59f, 108.71f })
            world.Events.Add(auto, AutoAttack);
        world.Events.Add(111.97f, CastDrachenlance);
        world.Events.Add(115.53f, ResolveDrachenlance);
        world.Events.Add(116.97f, () => CastSelf(nidhogg, ActionId.RevengeOfTheHorde, 11f));
        world.Events.Add(127.90f, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        for (var i = 0; i < soulTethers.Length; i++)
            LockSoulTetherOnTank(i);
    }

    private void SpawnNidhogg()
    {
        nidhogg = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Nidhogg,
            NameId: BNpcNameId.Nidhogg,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, 0f),
            HitboxRadius: NidhoggHitboxRadius,
            NpcSpawnTemplate: DsrNpcSpawn.Build(BNpcBaseId.Nidhogg, BNpcNameId.Nidhogg, NidhoggMaxHp)));
    }

    private void NidhoggEngages()
    {
        nidhogg?.SetTargetable(true);
        nidhogg?.SetTarget(party.Get(PartyRole.MainTank), follow: false);
    }

    // Nidhogg's autos are a line through his target.
    private void AutoAttack()
    {
        if (nidhogg == null || party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        var rotation = RotationTowards(nidhogg.Position, tank.Position);
        PlayEffect(nidhogg, ActionId.NidhoggAttack, 0.6f, rotation, tank.GameObjectId);
        foreach (var hit in party.Find.InsideRect(new Placement(nidhogg.Position, rotation), AutoAttackHalfWidth, AutoAttackRange).ToList())
        {
            if (hit == tank) damage.ApplyDamage(hit, AutoAttackDamage, ActionId.NidhoggAttack, "auto attack", lethal: false);
            else hit.Die(ActionId.NidhoggAttack, "stood in Nidhogg's auto attack line");
        }
    }

    private void MarkLines()
    {
        foreach (var (role, line) in state.Lines)
        {
            if (party.Get(role) is not { } member) continue;
            member.AttachLockonVfx(line switch { 1 => LockonId.FirstInLine, 2 => LockonId.SecondInLine, _ => LockonId.ThirdInLine }, 5f, persistent: false);
            member.AddStatus(line switch { 1 => StatusId.FirstInLine, 2 => StatusId.SecondInLine, _ => StatusId.ThirdInLine });
        }
    }

    private void GiveArrows()
    {
        foreach (var (role, arrow) in state.Arrows)
        {
            var status = arrow switch
            {
                DiveArrow.Up => StatusId.SpineshatterDiveTarget,
                DiveArrow.Down => StatusId.ElusiveJumpTarget,
                _ => StatusId.HighJumpTarget,
            };
            party.Get(role)?.AddStatus(status, ArrowSeconds[state.Lines[role] - 1]);
        }
    }

    private void ClearLines()
    {
        foreach (var member in AliveMembers())
            foreach (var status in new[] { StatusId.FirstInLine, StatusId.SecondInLine, StatusId.ThirdInLine })
                member.RemoveStatus(status);
    }

    // Each dive lands on its player; its tower is placed from where they stand and face.
    private void Dive(int line)
    {
        var towers = state.DiveTowers[line - 1];
        towers.Clear();
        var hits = new Dictionary<SimCharacter, int>();
        foreach (var role in state.Line(line))
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            var arrow = state.Arrows[role];
            var at = member.Position;
            var forward = new Vector3(MathF.Sin(member.Rotation), 0f, MathF.Cos(member.Rotation));
            towers.Add((role, at + forward * DsrP3NidhoggState.DiveOffset(arrow)));
            if (SpawnDiver(new Placement(at, member.Rotation)) is not { } diver) continue;
            diveTowerCasters[line - 1].Add(diver);
            PlayEffect(diver, DiveAction(arrow), 1.1f, target: member.GameObjectId, at: at);
            foreach (var hit in party.Find.InsideCircle(at, DiveTowerRadius))
                hits[hit] = hits.GetValueOrDefault(hit) + 1;
        }
        foreach (var (hit, count) in hits)
        {
            if (count > 1 || hit.HasStatus(StatusId.FireResistanceDownII))
            {
                hit.Die(DiveAction(DiveArrow.Circle), "hit by a second dive");
                continue;
            }
            damage.ApplyDamage(hit, DiveDamage, ActionId.DarkHighJump, "dive", lethal: false);
            hit.AddStatus(StatusId.FireResistanceDownII, FireResistanceDownSeconds);
            hit.AddStatus(StatusId.PhysicalVulnerabilityUp, DiveVulnerabilitySeconds);
        }
    }

    private void CastDiveTowers(int line)
    {
        var towers = state.DiveTowers[line - 1];
        var casters = diveTowerCasters[line - 1];
        for (var i = 0; i < towers.Count && i < casters.Count; i++)
        {
            casters[i].SetPosition(new Placement(towers[i].Spot, RotationTowards(towers[i].Spot, Vector3.Zero)));
            casters[i].NativeCast(ActionId.DarkdragonDive, ActionType.Action, 0f, 2.2f, false, position: towers[i].Spot);
        }
    }

    private void ResolveDiveTowers(int line)
    {
        var towers = state.DiveTowers[line - 1];
        var casters = diveTowerCasters[line - 1];
        var failed = false;
        for (var i = 0; i < towers.Count && i < casters.Count; i++)
        {
            var at = towers[i].Spot;
            PlayEffect(casters[i], ActionId.DarkdragonDive, 1.1f, at: at);
            var soakers = party.Find.InsideCircle(at, DiveTowerRadius).Where(m => m.IsAlive()).ToList();
            if (soakers.Count == 0) failed = true;
            SoakTower(soakers, ActionId.DarkdragonDive);
        }
        if (failed) TowerWipe("a Darkdragon Dive tower was left unsoaked");
    }

    private void SoakTower(IEnumerable<SimCharacter> soakers, uint actionId)
    {
        foreach (var soaker in soakers)
        {
            if (soaker.HasStatus(StatusId.FireResistanceDownII))
            {
                soaker.Die(actionId, "soaked a tower with Fire Resistance Down");
                continue;
            }
            damage.ApplyDamage(soaker, TowerDamage, actionId, "tower", lethal: false);
            soaker.AddStatus(StatusId.FireResistanceDownII, FireResistanceDownSeconds);
            soaker.AddStatus(StatusId.PhysicalVulnerabilityUp, TowerVulnerabilitySeconds);
        }
    }

    private void TowerWipe(string cause)
    {
        PlayEffect(nidhogg, ActionId.DarkdragonDiveFailed, 2.1f);
        foreach (var member in AliveMembers().ToList())
            member.Die(ActionId.DarkdragonDiveFailed, cause);
    }

    // Each tower's Geirskogul is aimed at whoever stands closest to it as the cast starts.
    private void CastGeirskoguls(int line) => CastGeirskogulsFrom(diveTowerCasters[line - 1]);

    private void CastGeirskogulsFrom(IEnumerable<SimEnemy> casters)
    {
        foreach (var caster in casters)
        {
            if (ClosestMember(caster.Position) is not { } target) continue;
            var rotation = RotationTowards(caster.Position, target.Position);
            caster.SetRotation(rotation);
            caster.NativeCast(ActionId.Geirskogul, ActionType.Action, 0f, 4.2f, false, rotation: rotation, position: caster.Position);
            geirskogulCasters.Add(caster);
        }
    }

    private void ResolveGeirskoguls()
    {
        var casters = geirskogulCasters.ToList();
        geirskogulCasters.Clear();
        foreach (var caster in casters)
        {
            PlayEffect(caster, ActionId.Geirskogul, 1.1f, caster.Rotation);
            damage.Resolve(caster, ActionId.Geirskogul, [DamageType.Lethal], []);
        }
        world.Events.Add(EffectCarrierLinger, () => casters.ForEach(c => c.FadeOut()));
    }

    private void CastWheels(int index) => CastSelf(nidhogg, WheelAction(index), 7.3f);

    private uint WheelAction(int index) => state.LashFirst[index] ? ActionId.LashAndGnash : ActionId.GnashAndLash;

    private void FirstWheel(int index) => Wheel(state.LashFirst[index]);

    private void SecondWheel(int index) => Wheel(!state.LashFirst[index]);

    // Lashing is the donut (stand close), Gnashing the circle (stand out).
    private void Wheel(bool lashing)
    {
        if (nidhogg == null) return;
        if (lashing)
        {
            PlayEffect(nidhogg, ActionId.LashingWheel, 1.1f);
            damage.Resolve(nidhogg, ActionId.LashingWheel, [DamageType.Lethal], [], size: LashingWheelInnerRadius);
            return;
        }
        PlayEffect(nidhogg, ActionId.GnashingWheel, 1.1f);
        damage.Resolve(nidhogg, ActionId.GnashingWheel, [DamageType.Lethal], []);
    }

    private void EyeOfTheTyrant(int index)
    {
        if (party.Get(state.EyeTargets[index]) is not { } target || !target.IsAlive()) return;
        PlayEffect(nidhogg, ActionId.EyeOfTheTyrant, 1.1f, target: target.GameObjectId, at: target.Position);
        foreach (var hit in damage.Resolve(target, ActionId.EyeOfTheTyrant, [], [], stackMinTargets: EyeOfTheTyrantMinTargets))
            if (hit.IsAlive()) damage.ApplyDamage(hit, EyeOfTheTyrantDamage, ActionId.EyeOfTheTyrant, "stack", lethal: false);
    }

    // Drachenlance cuts in front of Nidhogg, who faces his tank.
    private void CastDrachenlance()
    {
        if (nidhogg == null) return;
        if (party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive())
            nidhogg.SetRotation(RotationTowards(nidhogg.Position, tank.Position));
        CastSelf(nidhogg, ActionId.DrachenlanceWindup, 2.6f);
        world.Events.Add(0.09f, () => nidhogg?.NativeCast(ActionId.Drachenlance, ActionType.Action, 0f, 3.2f, false, rotation: nidhogg.Rotation, position: nidhogg.Position));
    }

    private void ResolveDrachenlance()
    {
        if (nidhogg == null) return;
        PlayEffect(nidhogg, ActionId.Drachenlance, 1.1f, nidhogg.Rotation);
        damage.Resolve(nidhogg, ActionId.Drachenlance, [DamageType.Lethal], [], size: DrachenlanceHalfAngle);
    }

    private void CastFourTowers()
    {
        fourTowers.Clear();
        for (var i = 0; i < DsrP3NidhoggState.TowerBearings.Length; i++)
        {
            var at = AtBearing(DsrP3NidhoggState.TowerBearings[i], FourTowerRadius);
            var needed = state.TowerSoakers[i];
            if (SpawnDiver(new Placement(at, RotationTowards(at, Vector3.Zero))) is not { } caster) continue;
            caster.NativeCast(FourTowerAction(needed), ActionType.Action, 0f, 4.7f, false, position: at);
            fourTowers.Add((caster, at, needed));
        }
    }

    private static uint FourTowerAction(int needed) => needed switch
    {
        1 => ActionId.DarkdragonDiveOne,
        2 => ActionId.DarkdragonDiveTwo,
        3 => ActionId.DarkdragonDiveThree,
        _ => ActionId.DarkdragonDiveFour,
    };

    private void ResolveFourTowers()
    {
        var failed = false;
        foreach (var (caster, at, needed) in fourTowers)
        {
            PlayEffect(caster, FourTowerAction(needed), 1.1f, at: at);
            var soakers = party.Find.InsideCircle(at, DiveTowerRadius).Where(m => m.IsAlive()).ToList();
            if (soakers.Count < needed) failed = true;
            SoakTower(soakers, FourTowerAction(needed));
        }
        if (failed) TowerWipe("a Darkdragon Dive tower had too few soakers");
    }

    private void CastFourTowerGeirskoguls() =>
        CastGeirskogulsFrom(fourTowers.Where((_, i) => i != state.QuietTower).Select(t => t.Caster).ToList());

    // Two non-tanks are tethered; a tank steps across the beam to take one over.
    private void TetherSoulTethers()
    {
        if (nidhogg == null) return;
        for (var i = 0; i < soulTethers.Length; i++)
        {
            var first = party.Get(state.SoulTetherTargets[i]) is { } holder && holder.IsAlive() ? holder : null;
            soulTethers[i] = world.Tether(nidhogg, End.Passable(first), TetherId.SoulTether);
        }
    }

    private void LockSoulTetherOnTank(int i)
    {
        if (soulTetherLocked[i] || nidhogg == null || soulTethers[i] is not { IsActive: true } tether) return;
        if (tether.B is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank } tank) return;
        if (soulTethers.Any(t => t != tether && t?.B == tether.B)) return;
        tether.Despawn();
        soulTethers[i] = world.Tether(nidhogg, (SimCharacter)tank, TetherId.SoulTether);
        soulTetherLocked[i] = true;
    }

    private void ResolveSoulTethers()
    {
        for (var i = 0; i < soulTethers.Length; i++)
        {
            if (soulTethers[i]?.B is not SimCharacter holder) continue;
            soulTethers[i]!.Despawn();
            soulTethers[i] = null;
            if (!holder.IsAlive()) continue;
            PlayEffect(nidhogg, ActionId.SoulTether, 1.1f, target: holder.GameObjectId, at: holder.Position);
            foreach (var hit in party.Find.InsideCircle(holder.Position, SoulTetherRadius).ToList())
            {
                if (hit is ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank }) damage.ApplyDamage(hit, SoulTetherTankDamage, ActionId.SoulTether, "tank buster", lethal: false);
                else hit.Die(ActionId.SoulTether, "Soul Tether needs a tank");
            }
        }
    }

    private SimEnemy? SpawnDiver(Placement placement)
    {
        var diver = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.NidhoggDiver,
            NameId: BNpcNameId.Nidhogg,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Placement: placement,
            NpcSpawnTemplate: DsrNpcSpawn.Build(BNpcBaseId.NidhoggDiver, BNpcNameId.Nidhogg, 1)));
        if (diver != null) divers.Add(diver);
        return diver;
    }

    private static uint DiveAction(DiveArrow arrow) => arrow switch
    {
        DiveArrow.Up => ActionId.DarkSpineshatterDive,
        DiveArrow.Down => ActionId.DarkElusiveJump,
        _ => ActionId.DarkHighJump,
    };

    private SimCharacter? ClosestMember(Vector3 at) =>
        AliveMembers().OrderBy(m => FlatDistance(m.Position, at)).FirstOrDefault();

    private void Raidwide(uint actionId, float fraction)
    {
        PlayEffect(nidhogg, actionId, 2.1f);
        foreach (var member in AliveMembers().ToList())
            damage.ApplyDamage(member, fraction, actionId, "raidwide", lethal: false);
    }

    private void DespawnAll()
    {
        foreach (var tether in soulTethers) tether?.Despawn();
        foreach (var diver in divers) diver.Despawn();
        divers.Clear();
        nidhogg?.Despawn();
    }

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: caster.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
}
