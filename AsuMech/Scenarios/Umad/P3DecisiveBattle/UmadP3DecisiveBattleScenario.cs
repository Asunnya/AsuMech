using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Umad.UmadConstants;

namespace AsuMech.Scenarios.Umad.P3DecisiveBattle;

// Dancing Mad P3 up to Limit Cut: the Decisive Battle, Bowels of Agony's elements and crystals, Exdeath's Thunder III and Implosion.
// t=0 = pull +6:58.0 (Definition of Insanity starts at 2.50).
public sealed class UmadP3DecisiveBattleScenario : IScenario
{
    public string Name => "Decisive Battle";
    public IPhase Phase => UmadZone.P3;
    public float BgmSecondsAtStart => 28.0f;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UmadP3DecisiveBattleAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly UmadP3DecisiveBattleSettingsWindow settingsWindow = new();

    public const float SpawnAt = 2.50f;
    public const float DecisiveBattleAt = 12.63f;
    public const float BowelsCastAt = 27.13f;
    public const float BowelsAt = 32.09f;
    public const float CrystalsAt = 33.17f;
    public const float PbaoeCastAt = 44.30f;
    public const float PbaoeAt = 51.27f;
    public const float ShortDebuffsAt = 51.17f;
    public const float ShortCrystalAt = 52.20f;
    public const float BusterCastAt = 55.41f;
    public static readonly float[] BusterAt = [60.46f, 63.49f];
    public const float ImplosionCastAt = 68.30f;
    public static readonly float[] ShockwaveAt = [74.00f, 76.01f];
    public const float LongDebuffsAt = 78.19f;
    public const float LongCrystalAt = 79.21f;
    public const float EndAt = 80.4f;

    private const float ShortDuration = 18.99f;
    private const float LongDuration = 45.99f;
    private const float WindDuration = 67.99f;

    public const float CrystalRadius = 14f;
    public const float ChaosAtWall = 9.7f;
    private static readonly uint[] CrystalLayoutIds =
        [EObjId.WindCrystalNeLayoutId, EObjId.WindCrystalSeLayoutId, EObjId.WindCrystalSwLayoutId, EObjId.WindCrystalNwLayoutId];

    private SimWorld world = null!;
    private SimParty party = null!;
    private UmadP3DecisiveBattleState state = null!;
    private SimEnemy? kefka;
    private SimEnemy? chaos;
    private SimEnemy? exdeath;
    private SimEnemy? thunderAnchor;
    private SimEventObject? windCrystal;
    private SimEventObject? fireCrystal;
    private SimEventObject? waterCrystal;

    public UmadP3DecisiveBattleState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new UmadP3DecisiveBattleState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP3DecisiveBattleState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnKefka);
        world.Events.Add(SpawnAt, () => kefka?.Cast(ActionId.DefinitionOfInsanity, animationLock: 3.1f));
        world.Events.Add(SpawnAt, SpawnChaosAndExdeath);
        world.Events.Add(9.65f, () => chaos?.Cast(ActionId.DecisiveBattle_Chaos, animationLock: 3.1f));
        world.Events.Add(9.69f, () => exdeath?.Cast(ActionId.DecisiveBattle_Exdeath, animationLock: 3.1f));
        world.Events.Add(DecisiveBattleAt, PickHeroes);
        world.Events.Add(12.65f, () => kefka?.SetPosition(new Placement(new Vector3(0f, 0f, -10f), 0f)));
        world.Events.Add(13.92f, () => kefka?.Cast(ActionId.KefkaRest, animationLock: 2.1f));
        world.Events.Add(20.0f, GatherBossesInTheMiddle);

        world.Events.Add(BowelsCastAt, () => chaos?.Cast(UmadActions.BowelsOfAgony));
        world.Events.Add(BowelsAt + 0.01f, HandOutDebuffs);
        world.Events.Add(CrystalsAt, SpawnCrystals);
        world.Events.Add(CrystalsAt + 0.3f, () => chaos?.MoveTo(Wall(state.WindSpot, ChaosAtWall), speed: 6f));

        world.Events.Add(PbaoeCastAt, () => exdeath?.Cast(UmadActions.ThunderIIIPbaoe));
        world.Events.Add(PbaoeCastAt, () => world.Tether(exdeath, thunderAnchor, TetherId.ExdeathThunder, duration: PbaoeAt - PbaoeCastAt));
        world.Events.Add(45.03f, () => world.Map.AddEffect(packetFlags: 0x00020001U, index: 0x22));
        world.Events.Add(PbaoeAt, DropHeroes);
        world.Events.Add(ShortDebuffsAt, () => BurstElement(state.FireShort));
        world.Events.Add(ShortCrystalAt, () => CrystalStrikes(state.FireShort));
        world.Events.Add(BusterCastAt, () => exdeath?.Cast(ActionId.ThunderIII_Cast, animationLock: 3.1f));
        foreach (var at in BusterAt)
            world.Events.Add(at, BustClosestToExdeath);
        world.Events.Add(56.27f, () => world.Map.AddEffect(packetFlags: 0x00080004U, index: 0x22));

        world.Events.Add(66.16f, () => kefka?.Cast(ActionId.KefkaUnrest, animationLock: 2.1f));
        world.Events.Add(68.25f, () => kefka?.Cast(ActionId.Trance, animationLock: 0.2f));
        world.Events.Add(72.32f, () => kefka?.AddStatusParam(StatusId.KefkaTrance, 0x1FF));
        var implosionFrom = new Placement();
        world.Events.Add(ImplosionCastAt, () =>
        {
            if (chaos is null) return;
            implosionFrom = chaos.Placement();
            chaos.Cast(state.Latitudinal ? ActionId.LatitudinalImplosion : ActionId.LongitudinalImplosion, animationLock: 6.1f);
        });
        for (var i = 0; i < 2; i++)
        {
            var turn = i * MathF.PI;
            world.Events.Add(ShockwaveAt[0], () => chaos?.CastFromHelper(UmadActions.ImplosionShockwave, at: implosionFrom.AddToRotation(turn + (state.Latitudinal ? MathF.PI / 2f : 0f))));
            world.Events.Add(ShockwaveAt[1], () => chaos?.CastFromHelper(UmadActions.ImplosionShockwave, at: implosionFrom.AddToRotation(turn + (state.Latitudinal ? 0f : MathF.PI / 2f))));
        }
        world.Events.Add(LongDebuffsAt, () => BurstElement(!state.FireShort));
        world.Events.Add(LongCrystalAt, () => CrystalStrikes(!state.FireShort));
        world.Events.Add(EndAt, () => windCrystal?.Despawn());
    }

    public static Vector3 Wall(int spot, float radius)
    {
        var dir = UmadP3DecisiveBattleState.Intercardinal(spot) * radius;
        return new Vector3(dir.X, 0f, dir.Y);
    }

    private void SpawnKefka()
    {
        kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.KefkaP3, NameId: BNpcNameId.Kefka, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.KefkaP3, Placement: new Placement(Vector3.Zero, 0f)));
        thunderAnchor = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.GravenImage, NameId: BNpcNameId.GravenImage, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            LayoutId: BNpcLayoutId.GravenImage, Placement: new Placement(new Vector3(0f, 0f, -47.5f), 0f)));
    }

    private void SpawnChaosAndExdeath()
    {
        chaos = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.ChaosP3, NameId: BNpcNameId.Chaos, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.ChaosP3, Placement: new Placement(new Vector3(-8f, 0f, 0f), 0f)));
        exdeath = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Exdeath, NameId: BNpcNameId.Exdeath, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.Exdeath, Placement: new Placement(new Vector3(8f, 0f, 0f), 0f)));
        chaos?.SetTarget(party.Get(PartyRole.MainTank), follow: false);
        exdeath?.SetTarget(party.Get(PartyRole.OffTank), follow: false);
    }

    private IEnumerable<SimCharacter> Alive(Func<PartyRole, bool> which)
        => Enum.GetValues<PartyRole>().Where(which).Select(r => party.Get(r)).OfType<SimCharacter>().Where(m => m.IsAlive());

    // Each boss marks the four closest to it as its heroes.
    private void PickHeroes()
    {
        chaos?.AddStatus(StatusId.EpicVillain);
        exdeath?.AddStatus(StatusId.FatedVillain);
        if (chaos is null) return;
        var epic = party.Find.ClosestN(chaos.Position, 4);
        foreach (var member in Alive(_ => true))
            member.AddStatus(epic.Contains(member) ? StatusId.EpicHero : StatusId.FatedHero);
    }

    private void DropHeroes()
    {
        foreach (var member in Alive(_ => true))
        {
            member.RemoveStatus(StatusId.EpicHero);
            member.RemoveStatus(StatusId.FatedHero);
        }
    }

    private void GatherBossesInTheMiddle()
    {
        chaos?.MoveTo(new Vector3(0.3f, 0f, 0f), speed: 6f);
        exdeath?.MoveTo(new Vector3(-1f, 0f, 0.1f), speed: 6f);
    }

    private void HandOutDebuffs()
    {
        foreach (var member in Alive(_ => true))
        {
            var role = ((ISimPartyMember)member).Role;
            member.AddStatus(state.Headwinds.Contains(role) ? StatusId.Headwind : StatusId.Tailwind, WindDuration);
            if (state.HoldsEntropy(role)) member.AddStatus(StatusId.EntropyP3, state.FireShort ? ShortDuration : LongDuration);
            if (state.HoldsDynamicFluid(role)) member.AddStatus(StatusId.DynamicFluidP3, state.FireShort ? LongDuration : ShortDuration);
        }
    }

    private SimEventObject? SpawnCrystal(uint eobjId, int spot, uint arg2)
    {
        var at = Wall(spot, CrystalRadius);
        return world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = eobjId, LayoutId = CrystalLayoutIds[spot], EventId = EObjId.PropEventId, TargetableStatus = 5, Arg2 = arg2,
            Placement = new Placement(at, MathF.Atan2(-at.X, -at.Z)),
        });
    }

    private void SpawnCrystals()
    {
        windCrystal = SpawnCrystal(EObjId.WindCrystal, state.WindSpot, EObjId.WindCrystalArg2);
        fireCrystal = SpawnCrystal(EObjId.FireCrystal, state.FireSpot, EObjId.FireCrystalArg2);
        waterCrystal = SpawnCrystal(EObjId.WaterCrystal, state.WaterSpot, EObjId.WaterCrystalArg2);
    }

    private (PartyRole Support, PartyRole Dps) Holders(bool fire) => fire ? state.Entropy : state.DynamicFluid;

    private void BurstElement(bool fire)
    {
        var (support, dps) = Holders(fire);
        foreach (var holder in Alive(r => r == support || r == dps))
        {
            holder.RemoveStatus(fire ? StatusId.EntropyP3 : StatusId.DynamicFluidP3);
            chaos?.CastFromHelper(fire ? UmadActions.StrayFlamesEntropy : UmadActions.StraySprayDynamicFluid, holder);
        }
    }

    // As many of the crystal's shots as there were holders still alive, on whoever is closest to it.
    private void CrystalStrikes(bool fire)
    {
        var (support, dps) = Holders(fire);
        var shots = Alive(r => r == support || r == dps).Count();
        var at = Wall(fire ? state.FireSpot : state.WaterSpot, CrystalRadius);
        foreach (var target in party.Find.ClosestN(at, shots))
            chaos?.CastFromHelper(fire ? UmadActions.InfernoFireCrystal : UmadActions.TsunamiWaterCrystal, target, at: new Placement(at, 0f));
        var crystal = fire ? fireCrystal : waterCrystal;
        world.Events.Add(1.4f, () => crystal?.Despawn());
    }

    private void BustClosestToExdeath()
    {
        if (exdeath is null) return;
        exdeath.CastFromHelper(UmadActions.ThunderIII, party.Find.Closest(exdeath.Position), at: exdeath.Placement());
    }
}
