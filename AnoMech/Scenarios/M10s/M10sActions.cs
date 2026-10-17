using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.M10s.M10sConstants;

namespace AnoMech.Scenarios.M10s;

public static class M10sActions
{
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);

    // Both tanks share it.
    public static readonly EnemyAction HotImpact = new(ActionId.HotImpact)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(TankBuster, Stack(2))],
        DeathExplanation = "the shared tank buster needs both tanks",
    };

    public static readonly EnemyAction DiversDare = new(ActionId.DiversDare)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    // Everyone the charge crosses within 13.5y of Red Hot dies; past that it is a light hit.
    public static readonly EnemyAction[] FlameFloaterLines =
    [
        FlameFloaterLine(ActionId.FlameFloaterFirst, 11.96f),
        FlameFloaterLine(ActionId.FlameFloaterSecond, 8.96f),
        FlameFloaterLine(ActionId.FlameFloaterThird, 5.96f),
        FlameFloaterLine(ActionId.FlameFloaterFourth, 2.96f),
    ];

    public const float FlameFloaterLethalWithin = 13.5f;

    private static EnemyAction FlameFloaterLine(uint actionId, float fireResistanceDown) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.2f },
        Effects = [Damage(split: Falloff(FlameFloaterLethalWithin)), ApplyStatus(StatusId.FireResistanceDownII, fireResistanceDown)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stretch the tether before the charge",
    };

    public static readonly EnemyAction AlleyOopInfernoSpread = new(ActionId.AlleyOopInfernoSpread)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.45f },
        DeathExplanation = "spread out",
    };

    // Burns everything behind and beside Red Hot; the slice it faces, the bait's, is spared.
    // UNVERIFIED: the half-angle; guides draw it as ~320°.
    public const float CutbackBlazeHalfAngle = MathF.PI * 160f / 180f;

    public static readonly EnemyAction CutbackBlazeCleave = new(ActionId.CutbackBlazeCleave)
    {
        Cast = new() { AnimationLock = 1.2f, FaceTarget = false },
        Area = new() { Size = CutbackBlazeHalfAngle, Rotation = MathF.PI },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "stay on the bait until it swings",
    };

    // UNVERIFIED: the minimum; stacks of three or four died in the logs.
    public static readonly EnemyAction PyrotationStack = new(ActionId.PyrotationStack)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(split: Stack(5))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack with the party",
    };

    public static readonly EnemyAction DiversDareBlue = new(ActionId.DiversDareBlue)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    public static readonly EnemyAction SickestTakeoffLane = new(ActionId.SickestTakeoffLane)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.5f },
        DeathExplanation = "stay out of Deep Blue's lane",
    };

    // Deep Blue's surf: everyone it reaches is carried along by the scenario.
    public static readonly EnemyAction SickSwellWave = new(ActionId.SickSwellWave)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: the minimum; every logged stack held its light party of four.
    public static readonly EnemyAction AwesomeSlab = new(ActionId.AwesomeSlab)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "one light party per stack",
    };

    public static readonly EnemyAction AwesomeSplash = new(ActionId.AwesomeSplash)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "spread out",
    };

    // UNVERIFIED: the half-angle; eight proteans 45° apart never caught a neighbour.
    public const float ProteanHalfAngle = MathF.PI * 15f / 180f;

    public static readonly EnemyAction ReverseFirst = Protean(ActionId.ReverseAlleyOopFirst);
    public static readonly EnemyAction DoubleDipFirst = Protean(ActionId.DoubleDipFirst);

    private static EnemyAction Protean(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = ProteanHalfAngle },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "one cone each",
    };

    // The second set only leaves Damage Down on whoever it catches.
    public static readonly EnemyAction ReverseSecond = Echo(ActionId.ReverseAlleyOopSecond);
    public static readonly EnemyAction DoubleDipSecond = Echo(ActionId.DoubleDipSecond);

    private static EnemyAction Echo(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = ProteanHalfAngle },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.DamageDown, 30f)],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: the knockback distance; guides call it small.
    public static readonly EnemyAction DeepImpactHit = new(ActionId.DeepImpactHit)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(TankBuster), Push(6f, 12f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a tank stands furthest from Deep Blue",
    };

    public static readonly EnemyAction XtremeSpectacular = new(ActionId.XtremeSpectacular)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage()],
    };

    public static readonly EnemyAction XtremeSpectacularHit = new(ActionId.XtremeSpectacularHit)
    {
        Cast = new() { AnimationLock = 0.2f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.2f },
    };

    public static readonly EnemyAction XtremeSpectacularLast = new(ActionId.XtremeSpectacularLast)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: the half-angle of the surfboards' cones.
    public const float SurfConeHalfAngle = MathF.PI * 20f / 180f;

    public static readonly EnemyAction BlastingSnapCone = SnapCone(ActionId.BlastingSnapCone);
    public static readonly EnemyAction PlungingSnapCone = SnapCone(ActionId.PlungingSnapCone);

    private static EnemyAction SnapCone(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.3f, FaceTarget = false },
        Area = new() { Size = SurfConeHalfAngle },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "one cone each",
    };

    // UNVERIFIED: the minimum; each logged one held its light party of four.
    public static readonly EnemyAction ReentryBlastCone = ReentryCone(ActionId.ReentryBlastCone);
    public static readonly EnemyAction ReentryPlungeCone = ReentryCone(ActionId.ReentryPlungeCone);

    private static EnemyAction ReentryCone(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.3f, FaceTarget = false },
        Area = new() { Size = SurfConeHalfAngle },
        Effects = [Damage(split: Stack(4))],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the light party shares it",
    };

    public static readonly EnemyAction VerticalBlastHit = VerticalHit(ActionId.VerticalBlastHit);
    public static readonly EnemyAction VerticalPlungeHit = VerticalHit(ActionId.VerticalPlungeHit);

    private static EnemyAction VerticalHit(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.3f },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the tank stands nearest the surfboard",
    };
}

