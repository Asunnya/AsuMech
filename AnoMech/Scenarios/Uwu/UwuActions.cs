using System.Linq;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Uwu.UwuConstants;

namespace AnoMech.Scenarios.Uwu;

public static class UwuActions
{
    private static readonly DamageSpec Magic = DamageType.Magic;
    private static readonly DamageSpec Physical = DamageType.Physical;

    // Don't hit player in gaol
    private static readonly AreaSpec SparesGaoled = new() { AdjustTargets = (_, hits) =>
        hits.Where(h => !h.HasStatus(StatusId.Fetters)).ToList() };

    // TODO: find proper delay and knockbackId
    private static readonly IEnemyActionEffect LandslideKnockback = Knockback(distance: 30, speed: 50);

    // -- Shared --
    public static readonly EnemyAction FeatherRain = new(ActionId.FeatherRain)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = SparesGaoled,
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.5f },
    };

    public static readonly EnemyAction EruptionPuddle = new(ActionId.EruptionPuddle)
    {
        Cast = new() { AnimationLock = 0.1f },
        Area = SparesGaoled,
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.96f },
    };

    public static readonly EnemyAction LandslideLine = Landslide(ActionId.LandslideLine, 2.1f);
    public static readonly EnemyAction LandslideAwaken = Landslide(ActionId.LandslideAwaken, 1.1f);
    public static readonly EnemyAction LandslideLineUltima = Landslide(ActionId.LandslideLineUltima, 1.1f);

    // Knocks back 0.73s after the visual.
    private static EnemyAction Landslide(uint actionId, float animationLock) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock },
        Effects = [LandslideKnockback],
        Timing = new() { DamageDelay = 1.03f },
    };

    // -- Ultima --
    public static readonly EnemyAction UltimatePredation = Visual(ActionId.UltimatePredation, 4.5f);
    public static readonly EnemyAction PostUltimatePredation1 = Visual(ActionId.PostUltimatePredation1, 2.1f);
    public static readonly EnemyAction PostUltimatePredation2 = Visual(ActionId.PostUltimatePredation2, 2.1f);
    public static readonly EnemyAction PostUltimatePredation3 = Visual(ActionId.PostUltimatePredation3, 2.1f);
    public static readonly EnemyAction RadiantPlumeUltima = Visual(ActionId.RadiantPlumeUltima, 2.1f);
    public static readonly EnemyAction LandslideUltima = Visual(ActionId.LandslideUltima, 2.1f);
    public static readonly EnemyAction ViscousAetheroplasmUltima = Visual(ActionId.ViscousAetheroplasmUltima, 2.1f);
    public static readonly EnemyAction UltimateAnnihilation = Visual(ActionId.UltimateAnnihilation, 4.5f);

    public static readonly EnemyAction CeruleumVent = new(ActionId.CeruleumVent)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.66f },
    };

    public static readonly EnemyAction RadiantPlumePuddle = new(ActionId.RadiantPlumePuddle)
    {
        Cast = new() { AnimationLock = 0.1f },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.86f },
    };

    public static readonly EnemyAction ViscousAetheroplasm = new(ActionId.ViscousAetheroplasmEffect)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, TankBuster)],
        Timing = new() { DamageDelay = 0.5f },
    };

    public static readonly EnemyAction HomingLasers = new(ActionId.HomingLasers)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Magic, TankBuster)],
        Timing = (TimingSpec)new() { DamageDelay = 2.88f },
    };

    // -- Garuda --
    public static readonly EnemyAction WickedWheelAwaken = new(ActionId.WickedWheelAwaken)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = [Damage(Physical, Lethal)],
        Timing = (TimingSpec)new() { DamageDelay = 1.31f },
    };

    // The donut's inner radius is Wicked Wheel's reach.
    public static readonly EnemyAction WickedTornado = new(ActionId.WickedTornado)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { Size = 7 },
        Effects = [Damage(Magic, Lethal)],
        Timing = (TimingSpec)new() { DamageDelay = 0.5f },
    };

    public static readonly EnemyAction WickedWheel = new(ActionId.WickedWheel)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = [Damage(Physical, Lethal)],
        Timing = (TimingSpec)new() { DamageDelay = 1.37f },
    };

    public static readonly EnemyAction MistralShriek = Raidwide(ActionId.MistralShriek, 2.3f, damage: 0.26f);

    // -- Ifrit --
    public static readonly EnemyAction CrimsonCyclone = new(ActionId.CrimsonCyclone)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Magic, Lethal)],
        Timing = (TimingSpec)new() { DamageDelay = 0.47f },
    };

    public static readonly EnemyAction CrimsonCycloneAwaken = new(ActionId.CrimsonCycloneAwaken)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Magic, Lethal)],
        Timing = (TimingSpec)new() { DamageDelay = 0.47f },
        DeathExplanation = "Awaken",
    };

    public static readonly EnemyAction EruptionIfrit = Visual(ActionId.EruptionIfrit, 2.4f);
    public static readonly EnemyAction InfernalFetters = Visual(ActionId.InfernalFetters, 0.6f);

    // -- Titan --
    // Visual only: the LandslideLine helpers deal the knockback.
    public static readonly EnemyAction LandslideTitan = Visual(ActionId.LandslideTitan, 4.1f);
    public static readonly EnemyAction BoulderTitan = Visual(ActionId.BoulderTitan, 2.1f);
    public static readonly EnemyAction Tumult = Raidwide(ActionId.Tumult, 1.1f, damage: 0f);

    public static readonly EnemyAction Bury = new(ActionId.Bury)
    {
        Effects = [Damage(Magic, Lethal)],
        Timing = (TimingSpec)new() { DamageDelay = 0.53f },
    };

    public static readonly EnemyAction Burst = new(ActionId.Burst)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Magic, Lethal)],
        Timing = (TimingSpec)new() { DamageDelay = 0.46f },
    };

    private static EnemyAction Visual(uint actionId, float animationLock) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock },
    };

    // Survivable by everyone.
    private static EnemyAction Raidwide(uint actionId, float animationLock, float damage) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock },
        Effects = [Damage(Magic)],
        Timing = (TimingSpec)new() { DamageDelay = damage },
    };
}
