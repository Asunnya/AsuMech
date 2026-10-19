using System;
using System.Linq;
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

    public static readonly EnemyAction HotImpactOnFire = HotImpact with { ActionId = ActionId.HotImpactOnFire };

    // Each hits only the four players its brother picked.
    public static readonly EnemyAction Firesnaking = new(ActionId.Firesnaking)
    {
        Cast = new() { AnimationLock = 1.2f },
        Effects = [OnHavingStatus(StatusId.Firesnaking, Damage())],
    };

    public static readonly EnemyAction Watersnaking = new(ActionId.Watersnaking)
    {
        Cast = new() { AnimationLock = 1.2f },
        Effects = [OnHavingStatus(StatusId.Watersnaking, Damage())],
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

    // UNVERIFIED: the damage of the line that splits the arena.
    public static readonly EnemyAction FlameFloaterSplitLine = new(ActionId.FlameFloaterSplitLine)
    {
        Cast = new() { AnimationLock = 1.2f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay off the line Red Hot charges down",
    };

    // UNVERIFIED: the minimum.
    public static readonly EnemyAction FreakyPyrotationStack = new(ActionId.FreakyPyrotationStack)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(split: Stack(2))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "pair up with your partner",
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

    // UNVERIFIED: narrower than 9.6°, the gap Reverse leaves on the cardinal between two players.
    public const float ProteanHalfAngle = MathF.PI * 7.5f / 180f;

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

    public static readonly EnemyAction AwesomeSlabSmallWave = AwesomeSlab with { ActionId = ActionId.AwesomeSlabSmallWave };
    public static readonly EnemyAction AwesomeSplashSmallWave = AwesomeSplash with { ActionId = ActionId.AwesomeSplashSmallWave };

    // A 120° fan from the wall opposite the big wave; it only leaves Damage Down.
    public static readonly EnemyAction DeepVarialCone = new(ActionId.DeepVarialCone)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = MathF.PI / 3f },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, 30f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction SteamBurst = new(ActionId.SteamBurst)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the fire puddles burst; stay 9y away",
    };

    private static readonly VulnSpec VulnToFire = VulnerableTo(StatusId.FireResistanceDownII);

    // Fire Resistance Down is timed so all four run out together; a second landing on anyone kills.
    public static readonly EnemyAction[] HotAerialLandings =
    [
        HotAerialLanding(ActionId.HotAerialFirst, 7.96f),
        HotAerialLanding(ActionId.HotAerialSecond, 5.95f),
        HotAerialLanding(ActionId.HotAerialThird, 3.96f),
        HotAerialLanding(ActionId.HotAerialFourth, 1.96f),
    ];

    private static EnemyAction HotAerialLanding(uint actionId, float fireResistanceDown) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToFire), ApplyStatus(StatusId.FireResistanceDownII, fireResistanceDown)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "only the bait stands where Red Hot lands",
    };

    // Whoever stands in it is locked in the Watery Grave; nobody in it wipes the party.
    public static readonly EnemyAction DeepAerialTower = new(ActionId.DeepAerialTower)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(), ApplyStatus(StatusId.Stun, 2f)],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: an unsoaked tower kills everyone.
    public static readonly EnemyAction UnmitigatedExplosion = new(ActionId.UnmitigatedExplosion)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "two players soak Deep Aerial's tower",
    };

    private static readonly AreaSpec SparesThePrisoners = new() { AdjustTargets = (_, hits) => hits.Where(h => !h.HasStatus(StatusId.WateryGrave)).ToList() };

    // The wave's damage depends on how far the tether is stretched, and lands on everyone it crosses.
    // UNVERIFIED: the edge; a 27.8y tether killed, 29.5y did not.
    public const float XtremeWaveSafeFrom = 28.5f;

    public static readonly EnemyAction XtremeWaveFire = XtremeWave(ActionId.XtremeWaveFire, lethal: false);
    public static readonly EnemyAction XtremeWaveFireShort = XtremeWave(ActionId.XtremeWaveFire, lethal: true);
    public static readonly EnemyAction XtremeWaveWater = XtremeWave(ActionId.XtremeWaveWater, lethal: false);
    public static readonly EnemyAction XtremeWaveWaterShort = XtremeWave(ActionId.XtremeWaveWater, lethal: true);

    private static EnemyAction XtremeWave(uint actionId, bool lethal) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = SparesThePrisoners,
        Effects = lethal ? [Damage(Lethal)] : [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = lethal ? "stretch the tether until it changes colour" : "one wave each; stay out of the other lines",
    };

    public static readonly EnemyAction ScathingSteam = new(ActionId.ScathingSteam)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = SparesThePrisoners,
        Effects = [Damage()],
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

    // Insane Air with Xtreme snaking: a hit of the other element washes the snake off.
    public static readonly EnemyAction BlastingSnapConeWashing = Washing(SnapCone(ActionId.BlastingSnapCone) with { Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)] }, StatusId.XtremeWatersnaking);
    public static readonly EnemyAction PlungingSnapConeWashing = Washing(SnapCone(ActionId.PlungingSnapCone) with { Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)] }, StatusId.XtremeFiresnaking);
    public static readonly EnemyAction ReentryBlastConeWashing = Washing(ReentryCone(ActionId.ReentryBlastCone), StatusId.XtremeWatersnaking);
    public static readonly EnemyAction ReentryPlungeConeWashing = Washing(ReentryCone(ActionId.ReentryPlungeCone), StatusId.XtremeFiresnaking);
    public static readonly EnemyAction VerticalBlastHitWashing = Washing(VerticalHit(ActionId.VerticalBlastHit), StatusId.XtremeWatersnaking);
    public static readonly EnemyAction VerticalPlungeHitWashing = Washing(VerticalHit(ActionId.VerticalPlungeHit), StatusId.XtremeFiresnaking);

    private static EnemyAction Washing(EnemyAction move, ushort snake) =>
        move with { Effects = [.. move.Effects, OnHavingStatus(snake, RemoveStatus(snake))] };

    public static readonly EnemyAction XtremeFiresnaking = new(ActionId.XtremeFiresnaking)
    {
        Cast = new() { AnimationLock = 1.2f },
        Effects = [OnHavingStatus(StatusId.XtremeFiresnaking, Damage())],
    };

    public static readonly EnemyAction XtremeWatersnaking = new(ActionId.XtremeWatersnaking)
    {
        Cast = new() { AnimationLock = 1.2f },
        Effects = [OnHavingStatus(StatusId.XtremeWatersnaking, Damage())],
    };

    // The washed-off snake bursts around its player and catches their whole group.
    public static readonly EnemyAction BailoutFire = Bailout(ActionId.BailoutFire);
    public static readonly EnemyAction BailoutWater = Bailout(ActionId.BailoutWater);

    private static EnemyAction Bailout(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 0.6f, CastSecondsOverride = 0.7f },
        Effects = [Damage(), ApplyStatus(StatusId.MagicVulnerabilityUp, 2f)],
        Timing = new() { DamageDelay = 0.3f },
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

