using System;
using AsuMech.Core.EnemyActions;
using static AsuMech.Core.EnemyActions.Distribution;
using static AsuMech.Core.EnemyActions.EnemyActionEffects;
using static AsuMech.Core.EnemyActions.Severity;
using static AsuMech.Scenarios.M12s.M12sConstants;

namespace AsuMech.Scenarios.M12s;

public static class M12sActions
{
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private const float MagicVulnSeconds = 1.96f;

    public const float PowerGusherHalfAngle = MathF.PI / 4f;
    // UNVERIFIED: the sheet draws no omen; narrow enough that only the one it aims at stands in it.
    public const float ThunderHalfAngle = MathF.PI / 12f;
    public const float ArcanumLightDownSeconds = 23.96f;
    public const float DoomSeconds = 8f;
    public const float PortentSeconds = 10f;
    public const float HotBloodedSeconds = 5f;
    // Lands an inner or outer tower's soaker on the other platform.
    public const float WindPush = 25f;

    public static readonly EnemyAction IdyllicDream = new(ActionId.IdyllicDream)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction LindwurmsMeteor = IdyllicDream with { ActionId = ActionId.LindwurmsMeteor };

    // The vision only shows where the clones will strike later; nothing lands.
    public static readonly EnemyAction PowerGusherRehearsal = new(ActionId.PowerGusherRehearsal)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = PowerGusherHalfAngle },
    };

    public static readonly EnemyAction SnakingKickRehearsal = new(ActionId.SnakingKickRehearsal)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction PowerGusherReplay = new(ActionId.PowerGusherReplay)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = PowerGusherHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "remember which way each clone's Power Gusher faced",
    };

    public static readonly EnemyAction SnakingKickReplay = new(ActionId.SnakingKickReplay)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "remember where the Snaking Kick clone stood",
    };

    // On one role: its four carry Light Resistance Down II and can't take a triple tower.
    public static readonly EnemyAction ArcanumSpread = new(ActionId.ArcadianArcanumSpread)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnerableTo(StatusId.LightResistanceDownII)), ApplyStatus(StatusId.LightResistanceDownII, ArcanumLightDownSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "spread the light",
    };

    // UNVERIFIED: lethal to the others it catches.
    public static readonly EnemyAction ManaBurst = new(ActionId.ManaBurst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [OnTarget(Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "take a defamation far from everyone",
    };

    // UNVERIFIED: the minimum (three survive it).
    public static readonly EnemyAction HeavySlam = new(ActionId.HeavySlam)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(3)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack with your light party",
    };

    // UNVERIFIED: anything but one soaker, or Light Resistance Down II in a triple tower, kills.
    public static readonly EnemyAction CosmicKiss = new(ActionId.CosmicKiss)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(split: Exactly(1))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "one soaker per tower",
    };

    public static readonly EnemyAction CosmicKissTriple = CosmicKiss with
    {
        Effects = [Damage(VulnerableTo(StatusId.LightResistanceDownII), split: Exactly(1))],
        DeathExplanation = "one soaker per tower, and not one the light spread hit",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction UnmitigatedExplosion = new(ActionId.UnmitigatedExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a tower went unsoaked",
    };

    // A line from the tower through its soaker; the soaker takes Doom, and (UNVERIFIED) anyone else in it dies.
    public static readonly EnemyAction DarkII = new(ActionId.LindwurmsDarkII)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [OnTarget(Damage(), ApplyStatus(StatusId.Doom, DoomSeconds)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "point the doom line away from the party",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction StoneIII = new(ActionId.LindwurmsStoneIII)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "walk out of the earth tower",
    };

    // Throws its soaker away from the tower's centre.
    public static readonly EnemyAction CosmicKissWind = CosmicKissTriple with
    {
        Effects = [Damage(VulnerableTo(StatusId.LightResistanceDownII), split: Exactly(1)), Push(WindPush, 20f)],
    };

    public static readonly EnemyAction Glare = new(ActionId.LindwurmsGlare)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    // Lethal to everyone in it once it reaches more than one.
    public static readonly EnemyAction ThunderII = new(ActionId.LindwurmsThunderII)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = ThunderHalfAngle, ExcludeCaster = true },
        Effects = [Damage(split: Exactly(1))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the portent's line must reach exactly one",
    };

    // UNVERIFIED: lethal to the others in it.
    public static readonly EnemyAction ReenactedManaBurst = new(ActionId.ReenactedManaBurst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay far from the clones replaying a defamation",
    };

    // UNVERIFIED: the minimum (the two tanks survive one).
    public static readonly EnemyAction ReenactedHeavySlam = new(ActionId.ReenactedHeavySlam)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(2)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack on the clones replaying a stack",
    };

    // UNVERIFIED: the shape. Read here as the half-room in front of the boss, on the main tank.
    public const float DoubleSobatHalfAngle = MathF.PI / 2f;

    public static readonly EnemyAction DoubleSobatFirst = new(ActionId.DoubleSobatFirst)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = DoubleSobatHalfAngle, CastTypeOverride = Core.Native.Interfaces.CastType.Cone },
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "only the main tank in front of the boss",
    };

    // UNVERIFIED: the cone's width; it kicks the way the first did.
    public const float DoubleSobatSecondHalfAngle = MathF.PI / 4f;

    public static readonly EnemyAction DoubleSobatSecond = new(ActionId.DoubleSobatSecond)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false, CastSecondsOverride = 4.3f },
        Area = new() { Size = DoubleSobatSecondHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the second kick follows the first: step out of it",
    };

    // UNVERIFIED: lethal to anyone sharing it.
    public static readonly EnemyAction EsotericFinisher = new(ActionId.EsotericFinisher)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the tanks take these apart, away from the party",
    };

    public static readonly EnemyAction ArcadianHell = IdyllicDream with { ActionId = ActionId.ArcadianHell };

    public static readonly EnemyAction ArcadianHellFour = new(ActionId.ArcadianHellFour)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction ArcadianHellEight = ArcadianHellFour with { ActionId = ActionId.ArcadianHellEight };
}
