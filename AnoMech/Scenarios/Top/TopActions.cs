using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

public static class TopActions
{
    private static readonly DamageSpec Magic = DamageType.Magic
        .VulnerableTo(StatusId.MagicVulnerabilityUp)
        .VulnerableTo(StatusId.VulnerabilityUp)
        .VulnerableTo(StatusId.MagicVulnerabilityUpMini, minStacks: 2);

    private static readonly DamageSpec Physical = new();

    private static readonly RuinSpec ComeRuin = new((2, StatusId.TwiceComeRuin), (3, StatusId.TriceComeRuin));

    // -- Sword/shield/leg/staff body forms --
    public static readonly EnemyAction EfficientBladework = new(ActionId.EfficientBladework)
    {
        Cast = new() { OmenDelay = Duration.OmegaAttackOmenDelay },
        Effects = [Damage(DamageType.Lethal)],
    };

    public static readonly EnemyAction BeyondDefense = new(ActionId.BeyondDefenseAOE)
    {
        Effects =
        [
            OnOthers(Damage(DamageType.Lethal)),
            OnTarget(Damage(Physical)),
            OnTarget(ApplyRuin(ComeRuin, 2, 6.96f)),
        ],
        Timing = new() { ResolveOffset = 0.4f },
    };

    public static readonly EnemyAction PilePitch = new(ActionId.PilePitch)
    {
        Effects = [StackDamage(Magic, min: 3), ApplyRuin(ComeRuin, 2, 6.96f)],
    };

    public static readonly EnemyAction Discharger = new(ActionId.Discharger)
    {
        Effects = [Knockback(KnockbackId.Discharger)],
        Timing = new() { ResolveOffset = 0.5f },
    };

    public static readonly EnemyAction OptimizedFireIII = new(ActionId.OptimizedFireIII)
    {
        Effects = [Damage(Magic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
    };

    // -- Wave Cannon / Oversampled / Diffuse --
    public static readonly EnemyAction OversampledWaveCannon = new(ActionId.OversampledWaveCannonAoe)
    {
        Effects =
        [
            Damage(Magic),
            ApplyRuin(ComeRuin, 2, 6.96f),
            ApplyStatus(StatusId.MagicVulnerabilityUp, 4.96f),
        ],
    };

    // -- Hyper Pulse --
    public static readonly EnemyAction HyperPulseCharging = new(ActionId.HyperPulseDeltaCharging)
    {
        Effects = [Damage(DamageType.Lethal)],
    };

    public static readonly EnemyAction HyperPulseShoot = new(ActionId.HyperPulseDeltaShoot)
    {
        Effects = [Damage(DamageType.Lethal)],
    };

    // -- Delta-specific --
    public static readonly EnemyAction RunMiDeltaVersion = new(ActionId.RunMiDeltaVersion)
    {
        Effects = [Damage(Magic)],
    };

    public static readonly EnemyAction DeltaExplosion = new(ActionId.DeltaExplosion)
    {
        Effects = [Damage(DamageType.Lethal)],
        Timing = new() { ResolveOffset = 0.4f },
    };

    public static readonly EnemyAction DeltaUnmitigatedExplosion = new(ActionId.DeltaUnmitigatedExplosion)
    {
        Effects = [Damage(DamageType.Lethal)],
        Timing = new() { ResolveOffset = 0.4f },
    };

    public static readonly EnemyAction SwivelCannonLeft = SwivelCannon(ActionId.SwivelCannonL, MathF.PI / 2);
    public static readonly EnemyAction SwivelCannonRight = SwivelCannon(ActionId.SwivelCannonR, -MathF.PI / 2);

    private static EnemyAction SwivelCannon(uint actionId, float rotation) => new(actionId)
    {
        Cast = new() { OmenDelay = 8.5f },
        Area = new() { Size = Geometry.SwivelCannonHalfAngle, Rotation = rotation },
        Effects = [Damage(DamageType.Lethal)],
    };

    public static readonly EnemyAction HwTetherBreak = new(ActionId.HwTetherBreak)
    {
        Effects =
        [
            Damage(Magic),
            ApplyRuin(ComeRuin, 3, Duration.HwTetherBreakStack),
            ApplyStatus(StatusId.MagicVulnerabilityUpMini, Duration.HwTetherBreakStack),
        ],
    };

    public static readonly EnemyAction HwTetherFail = new(ActionId.HwTetherFail)
    {
        Effects = [Damage(DamageType.Lethal)],
    };

    // -- P2 Party Synergy --
    public static readonly EnemyAction Spotlight = new(ActionId.Spotlight)
    {
        Effects = [StackDamage(Magic, min: 4), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
    };
}
