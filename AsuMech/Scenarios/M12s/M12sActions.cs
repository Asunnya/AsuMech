using System;
using AsuMech.Core.EnemyActions;
using static AsuMech.Core.EnemyActions.Distribution;
using static AsuMech.Core.EnemyActions.EnemyActionEffects;
using static AsuMech.Core.EnemyActions.Severity;
using static AsuMech.Scenarios.M12s.M12sConstants;

namespace AsuMech.Scenarios.M12s;

public static class M12sActions
{
    private static readonly VulnSpec VulnToPoison = VulnerableTo(StatusId.PoisonResistanceDownII);
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private const float PoisonSeconds = 12f;
    private const float MagicVulnSeconds = 1.96f;
    private const float DamageDownSeconds = 30f;

    public static readonly EnemyAction TheFixer = new(ActionId.TheFixer)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    public static readonly EnemyAction MortalSlayerGreen = new(ActionId.MortalSlayerGreen)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPoison), ApplyStatus(StatusId.PoisonResistanceDownII, PoisonSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "one orb per player: the closest to the boss on the orb's side takes it, 6y from the others",
    };

    public static readonly EnemyAction MortalSlayerPurple = new(ActionId.MortalSlayerPurple)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPoison, TankBuster), ApplyStatus(StatusId.PoisonResistanceDownII, PoisonSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "purple orbs are tank busters: a tank steps up to take each",
    };

    // UNVERIFIED: how hard an orb that finds nobody on its side hits the party.
    public static readonly EnemyAction ArmySlayerGreen = new(ActionId.ArmySlayerGreen)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPoison)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "an orb found nobody on its side",
    };

    public static readonly EnemyAction ArmySlayerPurple = ArmySlayerGreen with { ActionId = ActionId.ArmySlayerPurple };

    public const float RavenousReachHalfAngle = MathF.PI / 3f;

    public static readonly EnemyAction RavenousReachCone = new(ActionId.RavenousReachCone)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = RavenousReachHalfAngle },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stand behind the dragon head",
    };

    public const float SpotlightRadius = 5f;

    public static readonly EnemyAction PhagocyteSpotlight = new(ActionId.PhagocyteSpotlight)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "keep moving: the puddles drop where you stand",
    };

    public static readonly EnemyAction DramaticLysis = new(ActionId.DramaticLysis)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "Bursting Grotesquerie spreads 6y from everyone",
    };

    // UNVERIFIED: the minimum; the role that doesn't spread takes it together.
    public static readonly EnemyAction FourthWallFusion = new(ActionId.FourthWallFusion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the role that doesn't spread stacks on Shared Grotesquerie",
    };

    public const float HemorrhagicHalfAngle = MathF.PI / 12f;

    // UNVERIFIED: lethal; a single hit doesn't always kill in retail.
    public static readonly EnemyAction HemorrhagicProjection = new(ActionId.HemorrhagicProjection)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = HemorrhagicHalfAngle, ExcludeCaster = true },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "turn so your arrow's cleave points at nobody",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction Burst = new(ActionId.Burst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay 12y from every blood vessel",
    };

    public static readonly EnemyAction VisceralBurst = new(ActionId.VisceralBurst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the tank busters go to the tanks' spot, away from the party",
    };

    // UNVERIFIED: the minimum; everyone but the tanks takes it.
    public static readonly EnemyAction Act1FourthWallFusion = new(ActionId.Act1FourthWallFusion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the party stacks on the healer while the tanks take the busters apart",
    };
}
