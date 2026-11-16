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
    private const float ChainBreakVulnSeconds = 8.96f;
    private const float DamageDownSeconds = 30f;
    private const float CleansedPoisonSeconds = 1.95f;

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

    public const float SpotlightRadius = 5f;

    // UNVERIFIED: what a puddle does; none hit anyone in the logs.
    public static readonly EnemyAction PhagocyteSpotlight = new(ActionId.PhagocyteSpotlight)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "keep moving: the puddles drop where you stand",
    };

    public const float RavenousReachHalfAngle = MathF.PI / 3f;

    // Curtain Call's cleave cures Rotting Flesh; anyone else it catches leaves with Damage Down.
    public static readonly EnemyAction CurtainCallRavenousReach = new(ActionId.RavenousReachCone)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = RavenousReachHalfAngle },
        Effects =
        [
            Damage(),
            OnLackingStatus(StatusId.RottingFlesh, ApplyStatus(StatusId.DamageDown, DamageDownSeconds, delayToEffectResult: true)),
            OnHavingStatus(StatusId.RottingFlesh,
                RemoveStatus(StatusId.RottingFlesh, atImpact: true),
                ApplyStatus(StatusId.PoisonResistanceDownII, CleansedPoisonSeconds, delayToEffectResult: true)),
        ],
        Timing = new() { DamageDelay = 0.15f },
        DeathExplanation = "Rotting Flesh stands in the dragon's cleave, Bursting Grotesquerie stays out",
    };

    // UNVERIFIED: what two overlapping spreads do; no vuln was seen on the cured.
    public static readonly EnemyAction CellShedding = new(ActionId.CellShedding)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the cured spread 6y apart",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction CellDeath = new(ActionId.CellDeath)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "Rotting Flesh ran out: stand in the dragon's cleave to cure it",
    };

    public static readonly EnemyAction DramaticLysis = new(ActionId.DramaticLysis)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "Bursting Grotesquerie spreads 6y from everyone",
    };

    public static readonly EnemyAction ChainBreak = new(ActionId.ChainBreak)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, ChainBreakVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "break your chain 4y from everyone else's",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction Burst = new(ActionId.Burst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay 12y from every blood vessel",
    };

    public static readonly EnemyAction SplattershedRaidwide = new(ActionId.SplattershedRaidwide)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction SlaughtershedRaidwide = SplattershedRaidwide with { ActionId = ActionId.SlaughtershedRaidwide };

    public static readonly EnemyAction CurtainCallDramaticLysis = DramaticLysis with
    {
        ActionId = ActionId.CurtainCallDramaticLysis,
        DeathExplanation = "the spread role takes the corner without a blood vessel, 6y apart",
    };

    public static readonly EnemyAction CurtainCallFourthWallFusion = new(ActionId.CurtainCallFourthWallFusion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the stack role stacks in the corner with the blood vessel",
    };

    // UNVERIFIED: lethal; nobody stood in one.
    public static readonly EnemyAction SerpentineScourge = new(ActionId.SerpentineScourgeRect)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the dragon heads cleave half the room each, one side then the other",
    };

    // UNVERIFIED: the distance (30) and speed of the push.
    public const float RaptorKnucklesPush = 30f;

    public static readonly EnemyAction RaptorKnuckles = new(ActionId.RaptorKnucklesKnockback)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Push(RaptorKnucklesPush, 30f)],
        Timing = new() { DamageDelay = 0.2f },
    };

    public static readonly EnemyAction RefreshingOverkillRaidwide = new(ActionId.RefreshingOverkillRaidwide)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };
}
