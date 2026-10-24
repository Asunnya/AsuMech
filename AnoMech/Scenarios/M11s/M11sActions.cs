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
    // UNVERIFIED: how long before the hit the orange area appears.
    private const float HurricaneExplosionOmenDelay = 3.2f;

    // The orange area shows only near the end of the cast; the laser cast draws the lines until then.
    public static readonly EnemyAction HurricaneExplosion = new(ActionId.HurricaneExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false, OmenDelay = HurricaneExplosionOmenDelay },
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

    // Hits everyone within 4y of where the meteor lands.
    public static readonly EnemyAction CosmicKiss = new(ActionId.CosmicKiss)
    {
        Cast = new() { AnimationLock = 0.2f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: how close two meteors may land before they go off.
    public const float MeteorsTooClose = 6f;

    public static readonly EnemyAction UnmitigatedExplosion = new(ActionId.UnmitigatedExplosion)
    {
        Cast = new() { AnimationLock = 0.2f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "two meteors landed too close together",
    };

    public static readonly EnemyAction ForegoneFatality = new(ActionId.ForegoneFatality)
    {
        Cast = new() { AnimationLock = 0.2f },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a tank takes each shade's tether",
    };

    public static readonly EnemyAction CometExplosion = new(ActionId.CometExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, 30f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
    };

    // The first has no meteor to hide behind: the tanks take the front of the line.
    public static readonly EnemyAction FearsomeFireballFirst = new(ActionId.FearsomeFireballLine)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(split: WildCharge(1, 4, TankBuster))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a tank in front, the party behind in the line",
    };

    // A meteor in the way takes the line and explodes; unblocked, the party shares it.
    public static readonly EnemyAction FearsomeFireball = new(ActionId.FearsomeFireballLine)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(split: Stack(4))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack behind a meteor so it takes the line",
    };

    // UNVERIFIED: lethal for anyone a meteor doesn't hide.
    public static readonly EnemyAction Shockwave = new(ActionId.Shockwave)
    {
        Cast = new() { AnimationLock = 0.2f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "hide behind a meteor",
    };

    public static readonly EnemyAction FlatlinerKnockback = new(ActionId.FlatlinerKnockback)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), Push(15f, 15f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction MajesticMeteorPuddle = new(ActionId.MajesticMeteorPuddle)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "keep moving along the wall",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction MajesticMeteorain = new(ActionId.MajesticMeteorain)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay out of the portal's meteor line",
    };

    // The tether shows stretched from about 47y; closer and its line kills its target.
    public const float MeteowrathStretch = 47f;

    public static readonly EnemyAction MajesticMeteowrath = new(ActionId.MajesticMeteowrath)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects =
        [
            OnTarget(Damage(VulnToMagic, split: Falloff(MeteowrathStretch))),
            OnOthers(Damage(VulnToMagic)),
            ApplyStatus(StatusId.MagicVulnerabilityUp, 2.96f),
        ],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stretch the tether and keep its line off the others",
    };

    public static readonly EnemyAction FireBreathLine = new(ActionId.FireBreathLine)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 2.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "keep the breaths and tether lines apart",
    };

    public const float TowerKnockback = 23f;

    public static readonly EnemyAction TowerExplosion = new(ActionId.TowerExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), Push(TowerKnockback, 20f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    // Not lethal in the logs: it leaves Damage Down on everyone.
    public static readonly EnemyAction UnmitigatedTowerExplosion = new(ActionId.UnmitigatedTowerExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, 30f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
    };

    // UNVERIFIED: the minimum.
    public static readonly EnemyAction MassiveMeteorStack = new(ActionId.MassiveMeteorStack)
    {
        Cast = new() { AnimationLock = 0.2f },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "one stack per platform",
    };

    public static readonly EnemyAction ArcadionAvalancheSmash = new(0xB44B)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "only the far corner of the safe platform survives",
    };

    // UNVERIFIED: how close is lethal; 21y from it only hurts.
    public const float MammothLethalWithin = 18f;

    public static readonly EnemyAction MammothMeteor = new(ActionId.MammothMeteor)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(split: Falloff(MammothLethalWithin))],
        Timing = new() { DamageDelay = 0.5f },
        DeathExplanation = "get away from the giant meteors",
    };

    public static readonly EnemyAction AtomicImpact = new(ActionId.AtomicImpact)
    {
        Cast = new() { AnimationLock = 0.2f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the marked drop their puddles away from everyone",
    };

    public static readonly EnemyAction EclipticMajesticMeteor = MajesticMeteorPuddle with
    {
        ActionId = ActionId.EclipticMajesticMeteor,
        DeathExplanation = "keep the group moving round the boss",
    };

    // One soaker, a tank; Weighty Impact takes two.
    public static readonly EnemyAction EclipticCosmicKiss = new(ActionId.EclipticCosmicKiss)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction WeightyImpact = EclipticCosmicKiss with { ActionId = ActionId.WeightyImpact };

    public static readonly EnemyAction EclipticUnmitigatedExplosion = UnmitigatedTowerExplosion with { ActionId = ActionId.EclipticUnmitigatedExplosion };

    // The tether shows stretched from about 40y.
    public const float EclipticWrathStretch = 40f;

    public static readonly EnemyAction EclipticMeteowrath = MajesticMeteowrath with
    {
        ActionId = ActionId.EclipticMeteowrath,
        Effects =
        [
            OnTarget(Damage(VulnToMagic, split: Falloff(EclipticWrathStretch))),
            OnOthers(Damage(VulnToMagic)),
            ApplyStatus(StatusId.MagicVulnerabilityUp, 2.96f),
        ],
    };

    // UNVERIFIED: the minimum; four share each line.
    public static readonly EnemyAction TwoWayFireballLine = new(ActionId.TwoWayFireballLine)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(split: Stack(4))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "four in each line",
    };

    // UNVERIFIED: never reached in the logs. All eight share it; fewer, and each hit is a buster only
    // an invulnerable tank lives through.
    public static readonly EnemyAction HeartbreakKickTower = new(ActionId.HeartbreakKickTower)
    {
        Cast = new() { AnimationLock = 0.2f, FaceTarget = false },
        Effects = [Damage(split: Stack(8, understacked: TankBuster.MinMit(1f)))],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a tank invulns the tower alone, or all eight stand in it",
    };
}
