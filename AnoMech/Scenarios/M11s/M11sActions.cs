using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.M11s.M11sConstants;

namespace AnoMech.Scenarios.M11s;

public static class M11sActions
{
    private static readonly VulnSpec VulnToPhysical = VulnerableTo(StatusId.PhysicalVulnerabilityUp);
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private const float PhysicalVulnSeconds = 1.96f;
    private const float MagicVulnSeconds = 3f;

    public static readonly EnemyAction CrownOfArcadia = new(ActionId.CrownOfArcadia)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    public const float RawSteelConeHalfAngle = MathF.PI / 4f;
    public const float HeavyHitterHalfAngle = MathF.PI / 8f;

    public static readonly EnemyAction RawSteelScytheCone = new(ActionId.RawSteelScytheCone)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = RawSteelConeHalfAngle },
        Effects = [Damage(VulnToPhysical, TankBuster), ApplyStatus(StatusId.PhysicalVulnerabilityUp, PhysicalVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "each tank takes their own cone, away from the party",
    };

    // UNVERIFIED: the minimum; the six who aren't tanks share it.
    public static readonly EnemyAction HeavyHitter = new(ActionId.HeavyHitter)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = HeavyHitterHalfAngle },
        Effects = [Damage(VulnToPhysical, split: Stack(4)), ApplyStatus(StatusId.PhysicalVulnerabilityUp, PhysicalVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the party stacks in the cone behind the boss",
    };

    public static readonly EnemyAction RawSteelAxeStack = new(ActionId.RawSteelAxeStack)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPhysical, TankBuster, Stack(2)), ApplyStatus(StatusId.PhysicalVulnerabilityUp, PhysicalVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "both tanks share the axe",
    };

    public static readonly EnemyAction Impact = new(ActionId.Impact)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPhysical), ApplyStatus(StatusId.PhysicalVulnerabilityUp, PhysicalVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "spread out",
    };

    // UNVERIFIED: the weapons' own areas are lethal.
    public static readonly EnemyAction AxeStrike = new(ActionId.AssaultEvolvedAxe)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay out of the axe's circle",
    };

    // The omen is gl_sircle_6005: safe within 5y of the scythe.
    public static readonly EnemyAction ScytheStrike = new(ActionId.AssaultEvolvedScythe)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = 5f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "get under the scythe",
    };

    public static readonly EnemyAction SwordStrike = new(ActionId.AssaultEvolvedSword)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay off the sword's cross",
    };

    // On a healer. UNVERIFIED: the minimum; the party takes it together in the middle.
    public static readonly EnemyAction HeavyWeight = new(ActionId.HeavyWeight)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPhysical, split: Stack(4)), ApplyStatus(StatusId.PhysicalVulnerabilityUp, PhysicalVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack in the middle, clear of the axe",
    };

    public const float SweepingVictoryHalfAngle = MathF.PI / 12f;

    public static readonly EnemyAction SweepingVictory = new(ActionId.SweepingVictory)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = SweepingVictoryHalfAngle },
        Effects = [Damage(VulnToPhysical), ApplyStatus(StatusId.PhysicalVulnerabilityUp, PhysicalVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "one protean each around the scythe",
    };

    // UNVERIFIED: the minimum; each line takes a light party.
    public static readonly EnemyAction SharpTaste = new(ActionId.SharpTaste)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPhysical, split: Stack(4)), ApplyStatus(StatusId.PhysicalVulnerabilityUp, PhysicalVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "one light party per line",
    };

    public static readonly EnemyAction Cometite = new(ActionId.Cometite)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "keep moving out of the comets",
    };

    public static readonly EnemyAction Comet = new(ActionId.Comet)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "spread out",
    };

    // UNVERIFIED: the minimum; the whole party takes it.
    public static readonly EnemyAction CrushingComet = new(ActionId.CrushingComet)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToMagic, split: Stack(6)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack with the party",
    };

    public static readonly EnemyAction DanceOfDominationFirst = new(ActionId.DanceOfDominationFirst)
    {
        Cast = new() { AnimationLock = 0.2f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    public static readonly EnemyAction DanceOfDominationRepeat = DanceOfDominationFirst with { ActionId = ActionId.DanceOfDominationRepeat };
    public static readonly EnemyAction DanceOfDominationLast = DanceOfDominationFirst with { ActionId = ActionId.DanceOfDominationLast };

    public static readonly EnemyAction EyeOfTheHurricane = new(ActionId.EyeOfTheHurricane)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToMagic, split: Stack(2)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "pair up with your partner, apart from the other pairs",
    };

    // Not lethal on its own: it leaves Damage Down, and kills through the pair stack's vuln.
    public static readonly EnemyAction HurricaneExplosion = new(ActionId.HurricaneExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.DamageDown, 30f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.5f },
        DeathExplanation = "stand in a gap between the lines",
    };

    public static readonly EnemyAction Charybdistopia = new(ActionId.Charybdistopia)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    // UNVERIFIED: how often a tornado hits someone standing in it.
    public static readonly EnemyAction Charybdis = new(ActionId.Charybdis)
    {
        Cast = new() { AnimationLock = 0.2f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, 30f)],
        Timing = new() { DamageDelay = 0.2f },
    };

    public const float PowerfulGustHalfAngle = MathF.PI / 4f;

    public static readonly EnemyAction PowerfulGust = new(ActionId.PowerfulGust)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = PowerfulGustHalfAngle },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 2.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "two players per tornado, one gust each",
    };

    public static readonly EnemyAction OneAndOnlyRaidwide = new(ActionId.OneAndOnlyRaidwide)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
    };

    private static readonly VulnSpec VulnToFire = VulnerableTo(StatusId.FireResistanceDownII);

    // A second line on the same tank kills through Fire Resistance Down II.
    public static readonly EnemyAction GreatWallOfFireLine = new(ActionId.GreatWallOfFireLine)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToFire, TankBuster), ApplyStatus(StatusId.FireResistanceDownII, 7f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "swap for the second line, or invuln it",
    };

    // UNVERIFIED: lethal; nobody stood in it in the logs.
    public static readonly EnemyAction WallExplosion = new(ActionId.WallExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the wall the line left explodes",
    };

    // Each leaves Damage Down; neither is lethal on its own.
    public static readonly EnemyAction OrbitalOmenLine = new(ActionId.OrbitalOmenLine)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, 30f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.5f },
    };

    public const float FireAndFuryHalfAngle = MathF.PI / 4f;

    public static readonly EnemyAction FireAndFuryFront = new(ActionId.FireAndFuryFront)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = FireAndFuryHalfAngle },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, 30f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction FireAndFuryBack = FireAndFuryFront with { ActionId = ActionId.FireAndFuryBack };
}

