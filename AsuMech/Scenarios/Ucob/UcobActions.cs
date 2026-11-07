using AsuMech.Core.EnemyActions;
using AsuMech.Core.Native.Interfaces;
using static AsuMech.Core.EnemyActions.EnemyActionEffects;
using static AsuMech.Core.EnemyActions.Severity;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

public static class UcobActions
{

    public static readonly EnemyAction AutoAttack = new(ActionId.AutoAttack)
    {
        Cast = new() { AnimationLock = 0.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.03f },
    };

    // -- Exaflare --

    public static readonly EnemyAction Exaflare = new(ActionId.Exaflare)
    {
        Cast = new() { AnimationLock = 3.1f },
    };

    public static readonly EnemyAction ExaflareFirst = new(ActionId.ExaflareFirst)
    {
        Cast = new() { AnimationLock = 1.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.52f },
    };

    public static readonly EnemyAction ExaflareRest = new(ActionId.ExaflareRest)
    {
        Cast = new() { AnimationLock = 1.5f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.22f },
    };

    // -- Golden Bahamut --

    // UNVERIFIED: the sheet's shape is Custom; everyone stood within 3y of the target.
    // Fewer than all eight and the stack is too heavy for the rest.
    public static readonly EnemyAction MornAfah = new(ActionId.MornAfah)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(split: Distribution.Stack(8))],
        DeathExplanation = "Morn Afah is shared by the whole party: stack on the marked player",
    };

    // Akh Morn is a tank share: two in it, or a lone tank under an invuln.
    public static readonly EnemyAction AkhMorn = new(ActionId.AkhMorn)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [AkhMornHit],
        DeathExplanation = "Akh Morn: share it with the other tank, or take it alone under an invuln",
    };

    public static readonly EnemyAction AkhMornFollowUp = new(ActionId.AkhMornFollowUp)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [AkhMornHit],
        DeathExplanation = "Akh Morn: share it with the other tank, or take it alone under an invuln",
    };

    private static IEnemyActionEffect AkhMornHit => Damage(TankBuster, Distribution.Stack(2, understacked: TankBuster.MinMit(1f)));

    public static readonly EnemyAction MornAfahEnrage = new(ActionId.MornAfahEnrage)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction FlamesOfRebirth = new(ActionId.FlamesOfRebirth)
    {
        Cast = new() { AnimationLock = 1.0f }, // UNVERIFIED
        Effects = [ApplyStatus(StatusId.Transcendent, 6f), ApplyStatus(StatusId.PhoenixsBlessing, 0f)],
    };
}
