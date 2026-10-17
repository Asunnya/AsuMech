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

    // Burns everything in front of and beside Red Hot; only the slice behind it is spared.
    // UNVERIFIED: the half-angle; guides draw it as ~320°.
    public const float CutbackBlazeHalfAngle = MathF.PI * 160f / 180f;

    public static readonly EnemyAction CutbackBlazeCleave = new(ActionId.CutbackBlazeCleave)
    {
        Cast = new() { AnimationLock = 1.2f, FaceTarget = false },
        Area = new() { Size = CutbackBlazeHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "only the slice behind Red Hot is safe",
    };

    // UNVERIFIED: the minimum; stacks of three or four died in the logs.
    public static readonly EnemyAction PyrotationStack = new(ActionId.PyrotationStack)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(split: Stack(5))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack with the party",
    };
}
