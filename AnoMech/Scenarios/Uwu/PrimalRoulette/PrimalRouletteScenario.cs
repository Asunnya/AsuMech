using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using AnoMech.Scenarios.Legacy;
using static AnoMech.Scenarios.Uwu.UwuConstants;
using static AnoMech.Scenarios.Uwu.UwuUtils;

namespace AnoMech.Scenarios.Uwu.PrimalRoulette;

public sealed class PrimalRouletteScenario : IScenario
{
    public string Name => "Primal Roulette";
    public IPhase Phase => UwuZone.Ultima;
    public IReadOnlyList<IScenarioAi> AiStrats => [new PrimalRouletteAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    private const float TankPurgeDamage = 0.6f;
    private const float UltimaDamage = 0.66f;
    private const float AethericBoomDamage = 0.35f;
    private const float AethericBoomKnockback = 8f;
    private const float UltimaplasmBurstDamage = 0.52f;
    private const float FusionBurstDamage = 0.86f;
    private const float EarthenFuryDamage = 0.71f;
    private const float HellfireDamage = 0.61f;
    private const float AerialBlastDamage = 0.62f;
    // UNVERIFIED: the group always took it with all eight; a short stack has never been seen.
    private const int ViscousMinStack = 6;
    private const float OrbTouchRadius = 1.5f;
    private const float OrbDriftSpeed = 0.42f;
    private const float PoppedOrbFadeOut = 0.9f;
    private const float PartnerOrbFadeOut = 1f;
    private const float WeightOfTheLandCastTime = 2.7f;
    private const float WickedTornadoInnerRadius = 7f;

    private const int WeightFirstDummies = 0;
    private const int WeightSecondDummies = 4;
    private const int EruptionDummies = 8;
    private const int FeatherRainDummies = 11;
    private const int TornadoDummy = 16;
    private const int ViscousDummy = 17;
    private const int DummyCount = 18;
    private const int Eruptions = 3;

    private static readonly Placement UltimaAfterSuppression = new(new Vector3(13.7f, 0f, -13.7f), float.DegreesToRadians(-45f));
    private static readonly Placement CentreFacingNorth = new(Vector3.Zero, MathF.PI);
    private static readonly Placement CrimsonCycloneSouth = new(new Vector3(0f, 0f, 19.5f), MathF.PI);
    private static readonly Placement CrimsonCycloneEast = new(new Vector3(19.5f, 0f, 0f), float.DegreesToRadians(-90f));
    private static readonly Vector3 UltimaTankedNorth = new(-3f, 0f, -12f);
    private static readonly Vector3 UltimaForTheBurn = new(0f, 0f, -12f);

    private static readonly Vector3[][] OrbPairs =
    [
        [new(-6.5f, 0f, 9.5f), new(-9.5f, 0f, 6.5f)],
        [new(6.5f, 0f, 9.5f), new(9.5f, 0f, 6.5f)],
        [new(6.5f, 0f, -9.5f), new(9.5f, 0f, -6.5f)],
        [new(-6.5f, 0f, -9.5f), new(-9.5f, 0f, -6.5f)],
    ];

    private readonly PrimalRouletteSettingsWindow settingsWindow = new();

    private SimWorld world = null!;
    private SimParty party = null!;
    private UwuUtils utils = null!;
    private DamageSolver damage = null!;
    private PrimalRouletteState state = null!;

    private SimEnemy? ultima;
    private SimEnemy? garuda;
    private SimEnemy? ifrit;
    private SimEnemy? titan;
    private SimEnemy? crimsonSouth;
    private SimEnemy? crimsonEast;
    private readonly SimEnemy?[] dummies = new SimEnemy?[DummyCount];
    private readonly List<Orb> orbs = [];
    private readonly PartyRole[] eruptionBaits = new PartyRole[Eruptions];
    private IReadOnlyList<SimCharacter> crimsonCycloneSnapshot = [];
    private IReadOnlyList<SimCharacter> wickedWheelSnapshot = [];
    private IReadOnlyList<SimCharacter> wickedTornadoSnapshot = [];

    private sealed class Orb(SimEnemy enemy, int pair)
    {
        public SimEnemy Enemy { get; } = enemy;
        public int Pair { get; } = pair;
        public bool Burst { get; set; }
        public float? GoneAt { get; set; }
    }

    public void RunInstanceEvents(SimWorld instanceWorld)
    {
        var instanceUtils = new UwuUtils(instanceWorld);
        instanceWorld.Events.Add(1f, () =>
        {
            instanceUtils.UpdateArena(1);
            instanceUtils.UpdateArena(2);
        });
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        utils = new UwuUtils(world);
        damage = new DamageSolver(party);
        state = new PrimalRouletteState(world.Rng, party.Player != null ? party.PlayerRole : null, settingsWindow.Overrides);
        orbs.Clear();
        crimsonCycloneSnapshot = [];
        wickedWheelSnapshot = [];
        wickedTornadoSnapshot = [];

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<PrimalRouletteState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => utils.SpawnArenaFloor());
        world.Events.Add(0f, SpawnBosses);

        world.Events.Add(2.50f, () => CastSelf(ultima, ActionId.TankPurge, 3.7f));
        world.Events.Add(6.46f, () => Raidwide(ultima, ActionId.TankPurge, TankPurgeDamage, 2.1f));
        world.Events.Add(8.60f, () => ultima?.SetTargetable(false));
        world.Events.Add(8.60f, () => ultima?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(10.65f, () => ultima?.SetPosition(CentreFacingNorth));
        world.Events.Add(10.74f, () => ultima?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(12.88f, () => ultima?.SetTargetable(true));
        world.Events.Add(12.88f, () => CastSelf(ultima, ActionId.Ultima, 4.7f));
        world.Events.Add(17.87f, () => Raidwide(ultima, ActionId.Ultima, UltimaDamage, 2.1f));
        world.Events.Add(18.00f, UltimaFacesMainTank);
        world.Events.Add(24.10f, () => CastSelf(ultima, ActionId.AethericBoom, 3.7f));
        world.Events.Add(28.07f, AethericBoom);
        world.Events.Add(30.06f, SpawnOrbs);
        world.Events.Add(33.32f, DriftOrbsTogether);
        world.Events.Add(36.67f, FuseUnpoppedPairs);
        world.Events.Add(37.00f, () => ultima?.MoveTo(UltimaTankedNorth, 3f, 2.7f));
        world.Events.Add(49.28f, () => PlayEffect(ultima, ActionId.ViscousAetheroplasmRoulette, 1.1f));
        world.Events.Add(50.03f, ApplyViscousAetheroplasm);

        Roulette(0, PrimalRouletteState.SegmentStarts[0]);
        world.Events.Add(62.03f, () => ResolveViscousAetheroplasm(0));
        Roulette(1, PrimalRouletteState.SegmentStarts[1]);
        world.Events.Add(82.03f, () => ResolveViscousAetheroplasm(1));
        Roulette(2, PrimalRouletteState.SegmentStarts[2]);
        world.Events.Add(102.03f, () => ResolveViscousAetheroplasm(2));

        world.Events.Add(111.73f, () => ultima?.SetTargetable(false));
        world.Events.Add(111.73f, () => ultima?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(113.87f, () => ultima?.SetPosition(new Placement(UltimaForTheBurn, 0f)));
        world.Events.Add(113.96f, () => ultima?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(116.01f, () => ultima?.SetTargetable(true));
        // The group always burned Ultima down from here; what it does if it lives was never seen.
        world.Events.Add(116.10f, () => PlayEffect(ultima, ActionId.UltimaAfterRoulette, 1.1f));
        world.Events.Add(122.00f, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        if (orbs.Count == 0) return;
        var now = world.Events.Elapsed;
        foreach (var orb in orbs.ToList())
        {
            if (!orb.Enemy.IsActive)
                orbs.Remove(orb);
            else if (orb.GoneAt is { } goneAt)
            {
                if (now < goneAt) continue;
                orb.Enemy.Despawn();
                orbs.Remove(orb);
            }
            else if (party.Find.InsideCircle(orb.Enemy.Position, OrbTouchRadius).Count > 0)
                BurstOrb(orb, now);
        }
    }

    private SimCharacter? Get(PartyRole role) => party.Get(role);

    private SimEnemy? Dummy(int index) => dummies[index];

    private Func<SimEnemy?>[] DummyGetters(int first, int count) =>
        Enumerable.Range(first, count).Select(i => (Func<SimEnemy?>)(() => dummies[i])).ToArray();

    private SimEnemy? SpawnEnemy(uint baseId, uint nameId, Placement placement, bool targetable, bool visible, EnemyListMode enemyList, byte? modeAttributeFlags = null) =>
        world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: targetable,
            EnemyList: enemyList,
            IsVisible: visible,
            Placement: placement,
            InitialModeAttributeFlags: modeAttributeFlags));

    private SimEnemy? SpawnHiddenPrimal(uint baseId, uint nameId) =>
        SpawnEnemy(baseId, nameId, new Placement(Vector3.Zero, 0f), false, false, EnemyListMode.Never);

    private void SpawnBosses()
    {
        ultima = SpawnEnemy(BNpcBaseId.UltimaWeapon, BNpcNameId.UltimaWeapon, UltimaAfterSuppression, true, true, EnemyListMode.Always, 0x11);
        garuda = SpawnHiddenPrimal(BNpcBaseId.Garuda, BNpcNameId.Garuda);
        ifrit = SpawnHiddenPrimal(BNpcBaseId.Ifrit, BNpcNameId.Ifrit);
        titan = SpawnHiddenPrimal(BNpcBaseId.Titan, BNpcNameId.Titan);
        crimsonSouth = SpawnHiddenPrimal(BNpcBaseId.Ifrit, BNpcNameId.Ifrit);
        crimsonEast = SpawnHiddenPrimal(BNpcBaseId.Ifrit, BNpcNameId.Ifrit);

        utils.Awaken(ultima, true);
        utils.Awaken(garuda, false);

        for (var i = 0; i < dummies.Length; i++)
            dummies[i] = SpawnEnemy(BNpcBaseId.Dummy, BNpcNameId.Dummy, new Placement(Vector3.Zero, 0f), false, true, EnemyListMode.Never);

        UltimaFacesMainTank();
    }

    private void UltimaFacesMainTank()
    {
        if (Get(PartyRole.MainTank) is not { } tank) return;
        ultima?.SetTarget(tank, follow: false);
        ultima?.Face(tank);
    }

    private void Raidwide(SimEnemy? caster, uint actionId, float fraction, float animationLock)
    {
        PlayEffect(caster, actionId, animationLock);
        foreach (var member in party.ActiveMembers().ToList())
            damage.ApplyDamage(member, fraction, actionId, "Raidwide", false);
    }

    private void AethericBoom()
    {
        if (ultima == null) return;
        Raidwide(ultima, ActionId.AethericBoom, AethericBoomDamage, 2.1f);
        party.Knockback(ultima.Position, AethericBoomKnockback);
    }

    private void SpawnOrbs()
    {
        for (var pair = 0; pair < OrbPairs.Length; pair++)
            foreach (var at in OrbPairs[pair])
                if (SpawnEnemy(BNpcBaseId.Ultimaplasm, BNpcNameId.Ultimaplasm, new Placement(at, 0f), false, true, EnemyListMode.Never) is { } enemy)
                    orbs.Add(new Orb(enemy, pair));
    }

    private void DriftOrbsTogether()
    {
        foreach (var orb in orbs.Where(o => o.GoneAt == null))
        {
            var pair = OrbPairs[orb.Pair];
            orb.Enemy.MoveTo((pair[0] + pair[1]) / 2f, OrbDriftSpeed);
        }
    }

    // Touching one orb of a pair bursts it on everyone nearby and the other fizzles out.
    private void BurstOrb(Orb orb, float now)
    {
        orb.Burst = true;
        orb.GoneAt = now + PoppedOrbFadeOut;
        PlayEffect(orb.Enemy, ActionId.UltimaplasmBurst, 1.1f);
        foreach (var hit in party.Find.InsideActionAoe(ActionId.UltimaplasmBurst, orb.Enemy.Placement()))
            damage.ApplyDamage(hit, UltimaplasmBurstDamage, ActionId.UltimaplasmBurst, "Aetheroplasm", false);
        foreach (var partner in orbs.Where(o => o.Pair == orb.Pair && o != orb && o.GoneAt == null))
            partner.GoneAt = now + PartnerOrbFadeOut;
    }

    private void FuseUnpoppedPairs()
    {
        foreach (var pair in orbs.Where(o => o.GoneAt == null).GroupBy(o => o.Pair).Where(g => g.Count() == 2).ToList())
        {
            var fused = pair.First();
            Raidwide(fused.Enemy, ActionId.FusionBurst, FusionBurstDamage, 1.1f);
            foreach (var orb in pair) orb.GoneAt = world.Events.Elapsed;
        }
    }

    private void ApplyViscousAetheroplasm()
    {
        for (var i = 0; i < state.ViscousTargets.Count; i++)
            if (Get(state.ViscousTargets[i]) is { } carrier && carrier.IsAlive())
                carrier.AddStatus(StatusId.ViscousAetheroplasm, PrimalRouletteState.ViscousDurations[i]);
    }

    private void ResolveViscousAetheroplasm(int index)
    {
        if (Get(state.ViscousTargets[index]) is not { } carrier || !carrier.IsAlive()) return;
        carrier.RemoveStatus(StatusId.ViscousAetheroplasm);
        var burst = Dummy(ViscousDummy);
        burst?.SetPosition(new Placement(carrier.Position, 0f));
        PlayEffect(burst, ActionId.ViscousAetheroplasmEffect, 1.1f);
        damage.Resolve(carrier, ActionId.ViscousAetheroplasmEffect, [DamageType.Magic], [], stackMinTargets: ViscousMinStack);
    }

    private void Roulette(int segment, float start)
    {
        var primal = state.Order[segment];
        var absorb = primal switch
        {
            Primal.Garuda => ActionId.PostUltimatePredation1,
            Primal.Ifrit => ActionId.PostUltimatePredation2,
            _ => ActionId.PostUltimatePredation3,
        };
        world.Events.Add(start, () => CastSelf(ultima, absorb, 1.7f));
        world.Events.Add(start + 1.96f, () => PlayEffect(ultima, absorb, 1.1f));
        switch (primal)
        {
            case Primal.Garuda: GarudaRoulette(start); break;
            case Primal.Ifrit: IfritRoulette(start); break;
            default: TitanRoulette(start); break;
        }
    }

    private void WarpIn(SimEnemy? primal, Placement at)
    {
        primal?.SetPosition(at);
        primal?.SetVisible(true);
        primal?.PlayActionTimeline(ActionTimelineId.WarpEnd);
    }

    private void TitanRoulette(float s)
    {
        world.Events.Add(s + 4.01f, () => titan?.SetPosition(CentreFacingNorth));
        WeightOfTheLand(s + 4.09f, s + 7.07f, WeightFirstDummies);
        WeightOfTheLand(s + 7.13f, s + 10.11f, WeightSecondDummies);
        WeightOfTheLand(s + 10.11f, s + 13.09f, WeightFirstDummies);
        world.Events.Add(s + 10.84f, () => WarpIn(titan, CentreFacingNorth));
        world.Events.Add(s + 13.28f, () => CastSelf(titan, ActionId.EarthenFury, 2.7f));
        world.Events.Add(s + 16.26f, () => Raidwide(titan, ActionId.EarthenFury, EarthenFuryDamage, 2.1f));
        world.Events.Add(s + 21.43f, () => titan?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(s + 21.91f, () => titan?.SetVisible(false));
    }

    private void WeightOfTheLand(float castAt, float effectAt, int firstDummy)
    {
        var spots = new Vector3[4];
        world.Events.Add(castAt, () =>
        {
            var targets = RoleList.Random(world.Rng, party, spots.Length).List;
            for (var i = 0; i < spots.Length; i++)
            {
                spots[i] = Get(targets[i])?.Position ?? Vector3.Zero;
                dummies[firstDummy + i]?.SetPosition(new Placement(spots[i], 0f));
            }
        });

        for (var i = 0; i < spots.Length; i++)
        {
            var index = i;
            Func<Vector3> spot = () => spots[index];
            utils.Cast(() => dummies[firstDummy + index],
                castAt, new() { ActionId = ActionId.WeightOfTheLand, ActionType = ActionType.Action, CastTime = WeightOfTheLandCastTime },
                effectAt, new() { ActionId = ActionId.WeightOfTheLand, AnimationLock = 1.1f, SpellId = (ushort)ActionId.WeightOfTheLand, ActionType = ActionType.Action },
                new() { CastPosition = spot, ActionEffectPosition = spot },
                0.2f, snapshot => KillSnapshot(damage, snapshot, ActionId.WeightOfTheLand, "stood in a puddle"));
        }
    }

    private void IfritRoulette(float s)
    {
        world.Events.Add(s + 4.01f, () =>
        {
            WarpIn(ifrit, CentreFacingNorth);
            WarpIn(crimsonSouth, CrimsonCycloneSouth);
            WarpIn(crimsonEast, CrimsonCycloneEast);
        });
        world.Events.Add(s + 6.19f, () =>
        {
            CastSelf(crimsonSouth, ActionId.CrimsonCyclone, 2.7f);
            CastSelf(crimsonEast, ActionId.CrimsonCyclone, 2.7f);
        });
        world.Events.Add(s + 6.20f, PickEruptionBaits);
        for (var i = 0; i < Eruptions; i++)
        {
            var index = i;
            utils.EruptionPuddle(() => dummies[EruptionDummies + index], () => Get(eruptionBaits[index]), s + 6.23f, s + 9.23f);
        }
        world.Events.Add(s + 6.24f, () => CastSelf(ifrit, ActionId.EruptionIfrit, 2.2f));
        world.Events.Add(s + 8.73f, () => PlayEffect(ifrit, ActionId.EruptionIfrit, 1.1f));
        world.Events.Add(s + 8.89f, SnapshotCrimsonCyclones);
        world.Events.Add(s + 9.18f, ResolveCrimsonCyclones);
        world.Events.Add(s + 13.45f, () =>
        {
            crimsonSouth?.SetVisible(false);
            crimsonEast?.SetVisible(false);
        });
        world.Events.Add(s + 14.21f, () => CastSelf(ifrit, ActionId.Hellfire, 2.7f));
        world.Events.Add(s + 17.19f, () => Raidwide(ifrit, ActionId.Hellfire, HellfireDamage, 2.1f));
        world.Events.Add(s + 22.36f, () => ifrit?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(s + 22.85f, () => ifrit?.SetVisible(false));
    }

    private void PickEruptionBaits()
    {
        var picked = RoleList.Random(world.Rng, party, Eruptions).List;
        for (var i = 0; i < Eruptions; i++) eruptionBaits[i] = picked[i];
    }

    private void SnapshotCrimsonCyclones() =>
        crimsonCycloneSnapshot = new[] { crimsonSouth, crimsonEast }
            .Where(dasher => dasher != null)
            .SelectMany(dasher => party.Find.InsideActionAoe(ActionId.CrimsonCyclone, dasher!.Placement()))
            .Distinct()
            .ToList();

    private void ResolveCrimsonCyclones()
    {
        PlayEffect(crimsonSouth, ActionId.CrimsonCyclone, 2.1f);
        PlayEffect(crimsonEast, ActionId.CrimsonCyclone, 2.1f);
        KillSnapshot(damage, crimsonCycloneSnapshot, ActionId.CrimsonCyclone, "stood in Ifrit's path");
    }

    private void GarudaRoulette(float s)
    {
        world.Events.Add(s + 4.01f, () => WarpIn(garuda, CentreFacingNorth));
        world.Events.Add(s + 6.22f, () => CastSelf(garuda, ActionId.WickedWheelAwaken, 2.7f));
        world.Events.Add(s + 8.92f, () => wickedWheelSnapshot = garuda == null ? [] : party.Find.InsideActionAoe(ActionId.WickedWheelAwaken, garuda.Placement()));
        world.Events.Add(s + 9.13f, () => PlayEffect(garuda, ActionId.WickedWheelAwaken, 2.8f));
        world.Events.Add(s + 9.30f, () => KillSnapshot(damage, wickedWheelSnapshot, ActionId.WickedWheelAwaken, "stood inside Wicked Wheel"));
        world.Events.Add(s + 9.40f, () => Dummy(TornadoDummy)?.SetPosition(new Placement(Vector3.Zero, 0f)));
        world.Events.Add(s + 11.30f, WickedTornado);
        world.Events.Add(s + 11.80f, () => KillSnapshot(damage, wickedTornadoSnapshot, ActionId.WickedTornado, "stayed outside after Wicked Wheel"));
        world.Events.Add(s + 13.08f, () => CastSelf(garuda, ActionId.AerialBlast, 2.7f));
        world.Events.Add(s + 16.07f, () => Raidwide(garuda, ActionId.AerialBlast, AerialBlastDamage, 2.1f));
        world.Events.Add(s + 21.24f, () => garuda?.PlayActionTimeline(ActionTimelineId.WarpStart2));
        world.Events.Add(s + 22.69f, () => garuda?.SetVisible(false));
        utils.FeatherRain(DummyGetters(FeatherRainDummies, 5), s + 22.60f, s + 22.75f, s + 23.73f,
            resolve: snapshot => KillSnapshot(damage, snapshot, ActionId.FeatherRain, "stood under a feather"));
    }

    private void WickedTornado()
    {
        var tornado = Dummy(TornadoDummy);
        PlayEffect(tornado, ActionId.WickedTornado, 2.1f);
        wickedTornadoSnapshot = tornado == null ? [] : party.Find.InsideActionAoe(ActionId.WickedTornado, tornado.Placement(), size: WickedTornadoInnerRadius);
    }

    private void DespawnAll()
    {
        foreach (var enemy in new[] { ultima, garuda, ifrit, titan, crimsonSouth, crimsonEast }.Concat(dummies))
            enemy?.Despawn();
        foreach (var orb in orbs) orb.Enemy.Despawn();
        orbs.Clear();
        Array.Clear(dummies);
        ultima = garuda = ifrit = titan = crimsonSouth = crimsonEast = null;
    }
}
