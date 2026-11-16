using System;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Native.Interfaces;
using static AsuMech.Core.EnemyActions.Distribution;
using static AsuMech.Core.EnemyActions.EnemyActionEffects;
using static AsuMech.Core.EnemyActions.Severity;
using static AsuMech.Scenarios.M12s.M12sConstants;

namespace AsuMech.Scenarios.M12s;

public static class M12sActions
{
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private static IEnemyActionEffect MagicVuln(float seconds) => ApplyStatus(StatusId.MagicVulnerabilityUp, seconds);

    public const float HalfRoom = MathF.PI / 2f;
    public const float ScaldingHalfAngle = 5f * MathF.PI / 180f;
    public const float ProjectionHalfAngle = 25f * MathF.PI / 180f;
    public const float ProjectionReplayHalfAngle = 22.5f * MathF.PI / 180f;
    public const float BowtieHalfAngle = MathF.PI / 3f;
    public const float AeroSafeRadius = 5f;

    public static readonly EnemyAction ArcadiaAflame = new(ActionId.ArcadiaAflame)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction SnakingKick = new(ActionId.SnakingKick)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { Size = HalfRoom },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "get behind Lindwurm",
    };

    public static readonly EnemyAction FirefallSplashJump = new(ActionId.FirefallSplashJump)
    {
        Cast = new() { AnimationLock = 0.8f },
        Effects = [Damage(VulnToMagic), MagicVuln(4.96f)],
        Timing = new() { DamageDelay = 0.2f },
        DeathExplanation = "only the boss tether stands where Lindwurm lands",
    };

    // The jump's target is spared: the boss lands on them.
    public static readonly EnemyAction ScaldingWaves = new(ActionId.ScaldingWaves)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ScaldingHalfAngle, ExcludeCaster = true },
        Effects = [Damage(VulnToMagic), MagicVuln(4.96f)],
        Timing = new() { DamageDelay = 0.2f },
        DeathExplanation = "one thin cleave each, nobody behind another",
    };

    // UNVERIFIED: lethal to anyone else inside.
    public static readonly EnemyAction ManaBurst = new(ActionId.ManaBurst)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [OnTarget(Damage(VulnToMagic), MagicVuln(1.96f)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "take a defamation far from everyone",
    };

    // UNVERIFIED: the minimum.
    public static readonly EnemyAction HeavySlam = new(ActionId.HeavySlam)
    {
        Cast = new() { AnimationLock = 4.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(2)), MagicVuln(4.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the light party shares the stack",
    };

    // Fired from the cone tether's holder the way they face. UNVERIFIED: lethal.
    public static readonly EnemyAction HemorrhagicProjection = new(ActionId.HemorrhagicProjection)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ProjectionHalfAngle, ExcludeCaster = true },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the cone holder faces away from the party",
    };

    public static readonly EnemyAction TimelessSpite = new(ActionId.TimelessSpite)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(VulnToMagic, split: Stack(2)), MagicVuln(5.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the defamations bait it with a partner each",
    };

    // UNVERIFIED: the replays' own areas are lethal.
    public static readonly EnemyAction FirefallSplashReplay = new(ActionId.FirefallSplashReplay)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "stay off the north clone when it lands",
    };

    public static readonly EnemyAction ScaldingWavesReplay = new(ActionId.ScaldingWavesReplay)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ScaldingHalfAngle },
        Effects = [Damage(VulnToMagic), MagicVuln(5.96f)],
        Timing = new() { DamageDelay = 0.2f },
        DeathExplanation = "the replay cleaves the same four: same lineup",
    };

    public static readonly EnemyAction ManaBurstReplay = new(ActionId.ManaBurstReplay)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a defamation clone goes off: keep 20y from it",
    };

    public static readonly EnemyAction HemorrhagicProjectionReplay = new(ActionId.HemorrhagicProjectionReplay)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ProjectionReplayHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a cone clone fires through the middle",
    };

    public static readonly EnemyAction HeavySlamReplay = new(ActionId.HeavySlamReplay)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(VulnToMagic), MagicVuln(5.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "soak the stack clones",
    };

    // UNVERIFIED: lethal to all.
    public static readonly EnemyAction UnmitigatedImpact = new(ActionId.UnmitigatedImpact)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a stack went off with nobody in it",
    };

    public static readonly EnemyAction BloodyBurst = new(ActionId.BloodyBurst)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: the black holes' releases are lethal.
    public static readonly EnemyAction LindwurmsWater = new(ActionId.LindwurmsWaterIII)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the hole that sucked in the blue orb drops a circle",
    };

    public static readonly EnemyAction LindwurmsAero = new(ActionId.LindwurmsAeroIII)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = AeroSafeRadius },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the green orb's donut covers the arena: get under its hole",
    };

    public static readonly EnemyAction StraightforwardThunder = new(ActionId.StraightforwardThunderII)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = BowtieHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the purple orb cleaves north and south of its hole",
    };

    public static readonly EnemyAction SidewaysFire = new(ActionId.SidewaysFireII)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = BowtieHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the orange orb cleaves east and west of its hole",
    };

    public static readonly EnemyAction WailingWave = new(ActionId.WailingWave)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: retail deals heavy damage (guides say a Damage Down); it kills here so the miss shows.
    public static readonly EnemyAction UnmitigatedExplosionAlpha = new(ActionId.UnmitigatedExplosion)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "Mutation α took damage",
    };

    public static readonly EnemyAction UnmitigatedExplosionBeta = UnmitigatedExplosionAlpha with
    {
        DeathExplanation = "Mutation β took no damage",
    };

    public static readonly EnemyAction DoubleSobatBuster = new(ActionId.DoubleSobatBuster)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = HalfRoom, CastTypeOverride = CastType.Cone },
        Effects = [Damage(TankBuster.MinMit(1f))],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "an invuln takes it, everyone else behind the boss",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction DoubleSobatRepeat = new(ActionId.DoubleSobatRepeat)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = HalfRoom },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the second kick turns with the first's animation",
    };

    public static readonly EnemyAction EsotericFinisher = new(ActionId.EsotericFinisher)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the tanks take their busters away from the party",
    };
}
