using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Core.EnemyActions;

// Actions no one fight owns.
public static class CommonActions
{
    // A Confused player's swing at the allies in front of them (SimParty uses it). Retail hits for
    // about twice the target's max HP; the area is the sheet's.
    public static readonly EnemyAction ConfusedAttack = new(50445)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(Severity.Lethal)],
        Timing = new() { DamageDelay = 0.58f },
    };
}
