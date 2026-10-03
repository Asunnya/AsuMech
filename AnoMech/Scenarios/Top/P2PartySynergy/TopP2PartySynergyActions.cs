using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top.P2PartySynergy;

public static class TopP2PartySynergyActions
{
    private static readonly DamageSpec Magic = DamageType.Magic
        .VulnerableTo(StatusId.MagicVulnerabilityUp)
        .VulnerableTo(StatusId.VulnerabilityUp);

    public static readonly EnemyAction OptimizedFireIII = new(ActionId.OptimizedFireIII)
    {
        Effects = [Damage(Magic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
    };

    public static readonly EnemyAction Spotlight = new(ActionId.Spotlight)
    {
        Effects = [StackDamage(Magic, min: 4), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
    };

    public static readonly EnemyAction Discharger = new(ActionId.Discharger)
    {
        Effects = [Knockback(KnockbackId.Discharger)],
        Timing = new() { ResolveOffset = 0.5f },
    };

    public static readonly EnemyAction EfficientBladework = new(ActionId.EfficientBladework)
    {
        Cast = new() { OmenDelay = Duration.OmegaAttackOmenDelay },
        Effects = [Damage(DamageType.Lethal)],
    };
}
