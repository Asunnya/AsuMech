using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using P1 = AnoMech.Scenarios.Umad.P1TeleTrouncing.UmadP1TeleTrouncingConstants;

namespace AnoMech.Scenarios.Umad;

public static class UmadActions
{
    private static readonly DamageSpec Magic = DamageType.Magic.VulnerableTo(UmadConstants.StatusId.MagicVulnerabilityUp);

    private static readonly IEnemyActionEffect MagicVulnerabilityUp =
        ApplyStatus(UmadConstants.StatusId.MagicVulnerabilityUp, P1.MagicVulnerabilityUpSeconds);

    public static readonly EnemyAction AutoAttack = new(UmadConstants.ActionId.AutoAttack1)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.AutoAttack },
        Effects = [Damage(DamageType.Physical)],
        Timing = new() { DamageDelay = 0.8f },
    };

    // -- P1 Tele-trouncing --
    public static readonly EnemyAction TeleTrouncing = Visual(P1.ActionId.TeleTrouncing, P1.AnimationLock.TeleTrouncing);
    public static readonly EnemyAction GravenImage = Visual(P1.ActionId.GravenImage, P1.AnimationLock.GravenImage);
    public static readonly EnemyAction Unk1BossP1 = Visual(P1.ActionId.Unk1BossP1, P1.AnimationLock.Unk1);
    public static readonly EnemyAction Unk2BossP1 = Visual(P1.ActionId.Unk2BossP1, P1.AnimationLock.Unk2);
    public static readonly EnemyAction TeleportP1 = Visual(P1.ActionId.TeleportP1, P1.AnimationLock.Teleport);
    public static readonly EnemyAction MysteryMagic = Visual(P1.ActionId.MysteryMagic, P1.AnimationLock.MysteryMagic);

    public static readonly EnemyAction TeleTrouncingArrowSpawn = Visual(P1.ActionId.TeleTrouncingArrowSpawn, P1.AnimationLock.Helper);

    // Cast on the holder, who counts toward the stack but is neither pushed nor given the vuln.
    public static readonly EnemyAction DoubleTroubleTrapStack = new(P1.ActionId.DoubleTroubleTrapStack)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects =
        [
            Damage(Magic, split: Stack(4)),
            OnOthers(MagicVulnerabilityUp),
            OnOthers(Knockback(P1.KnockbackId.DoubleTroubleTrapStack, knockbackDelay: 0f)),
        ],
        Timing = new() { DamageDelay = 0.67f },
    };

    public static readonly EnemyAction IndulgentWill = new(P1.ActionId.IndulgentWill)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic)],
        Timing = new() { DamageDelay = 0.63f },
    };

    // A spread on the sleeper it's cast on: lethal only through its vuln, to a sleeper it hits twice
    // or a Confused player whose Indulgent Will lands after it.
    public static readonly EnemyAction IdyllicWill = new(P1.ActionId.IdyllicWill)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic), MagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.63f },
    };

    // Truth: 2x Real1. Lie: Real2 (no telegraph) at the real slots, Fake at the other two.
    public static readonly EnemyAction ThrummingThunderReal1 = ThrummingThunder(P1.ActionId.ThrummingThunderReal1);
    public static readonly EnemyAction ThrummingThunderReal2 = ThrummingThunder(P1.ActionId.ThrummingThunderReal2);
    public static readonly EnemyAction ThrummingThunderFake = Visual(P1.ActionId.ThrummingThunderFake, P1.AnimationLock.Helper);

    private static EnemyAction ThrummingThunder(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.65f },
    };

    // Cast by the gazing statue, whose position is the gaze's source.
    public static readonly EnemyAction IndolentWill = StatueGaze(P1.ActionId.IndolentWill, lookAway: true);
    public static readonly EnemyAction AveMaria = StatueGaze(P1.ActionId.AveMaria, lookAway: false);

    private static EnemyAction StatueGaze(uint actionId, bool lookAway) => new(actionId)
    {
        Effects = [Gaze(lookAway)],
        Timing = new() { DamageDelay = 0.84f },
    };

    // One per player: a second circle on anyone is lethal through the first's vuln.
    public static readonly EnemyAction FlagrantFireSpread = new(P1.ActionId.FlagrantFireSpread)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic), MagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction FlagrantFireStack = new(P1.ActionId.FlagrantFireStack)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic, split: Stack(4)), MagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.71f },
    };

    private static EnemyAction Visual(uint actionId, float animationLock) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock },
    };
}
