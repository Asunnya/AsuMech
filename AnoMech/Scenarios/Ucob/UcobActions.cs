using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Ucob.UcobConstants;

namespace AnoMech.Scenarios.Ucob;

public static class UcobActions
{
    private static readonly DamageSpec Magic = DamageType.Magic;

    // -- Exaflare --
    // The kill is held past the snapshot so the KO reads off the visible bloom.
    private static readonly TimingSpec ExaflareTiming = new() { DamageDelay = 0.6f };

    public static readonly EnemyAction ExaflareFirst = new(ActionId.ExaflareFirst)
    {
        Effects = [Damage(Magic, Lethal)],
        Timing = ExaflareTiming,
    };

    public static readonly EnemyAction ExaflareRest = new(ActionId.ExaflareRest)
    {
        Cast = new() { AnimationLock = 0f },
        Effects = [Damage(Magic, Lethal)],
        Timing = ExaflareTiming,
    };
}
