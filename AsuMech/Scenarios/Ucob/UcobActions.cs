using System;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Native.Interfaces;
using static AsuMech.Core.EnemyActions.Distribution;
using static AsuMech.Core.EnemyActions.EnemyActionEffects;
using static AsuMech.Core.EnemyActions.Severity;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

public static class UcobActions
{

    public static readonly EnemyAction AutoAttack = new(ActionId.AutoAttack)
    {
        Cast = new() { AnimationLock = 0.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.03f },
    };

    // -- Exaflare --

    public static readonly EnemyAction Exaflare = new(ActionId.Exaflare)
    {
        Cast = new() { AnimationLock = 3.1f },
    };

    public static readonly EnemyAction ExaflareFirst = new(ActionId.ExaflareFirst)
    {
        Cast = new() { AnimationLock = 1.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.52f },
    };

    public static readonly EnemyAction ExaflareRest = new(ActionId.ExaflareRest)
    {
        Cast = new() { AnimationLock = 1.5f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.22f },
    };

    // -- Bahamut Prime: Quickmarch and Blackfire --

    public static readonly EnemyAction MegaflareDive = Dive(ActionId.MegaflareDive);
    public static readonly EnemyAction LunarDive = Dive(ActionId.LunarDive);
    public static readonly EnemyAction TwistingDive = Dive(ActionId.TwistingDive);

    private static EnemyAction Dive(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.5f },
        DeathExplanation = "stay out of the dives' lines",
    };

    // UNVERIFIED: the blast's damage; the arena edge is close, so it is played as lethal.
    public static readonly EnemyAction TwisterBlast = new(ActionId.TwisterBlast)
    {
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a Twister was touched (step off your spot as the dives land, and keep clear of the Twisters)",
    };

    // UNVERIFIED: the puddle's damage.
    public static readonly EnemyAction MegaflarePuddle = new(ActionId.MegaflarePuddle)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay out of the Megaflare puddles",
    };

    // UNVERIFIED: a second player caught in a spread dies.
    public static readonly EnemyAction MegaflareSpread = new(ActionId.MegaflareSpread)
    {
        Effects = [OnTarget(Damage()), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "caught in someone's Megaflare spread (spread out after the dives)",
    };

    // UNVERIFIED: anything short of every marked player kills the stack.
    public static readonly EnemyAction MegaflareStackOfThree = MegaflareStack(3);
    public static readonly EnemyAction MegaflareStackOfFour = MegaflareStack(4);

    private static EnemyAction MegaflareStack(int marked) => new(ActionId.MegaflareStack)
    {
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(split: Stack(marked))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the marked players stack together",
    };

    // UNVERIFIED: the half-angle; it lies between 27° and 88°.
    public const float EarthShakerHalfAngle = MathF.PI / 4f;

    public static readonly EnemyAction EarthShakerCone = new(ActionId.EarthShakerCone)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = EarthShakerHalfAngle },
        Effects = [Damage(VulnerableTo(StatusId.EarthResistanceDownII)), ApplyStatus(StatusId.EarthResistanceDownII, 11.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "hit by two Earth Shakers (point yours away from everyone)",
    };

    public static readonly EnemyAction TempestWing = new(ActionId.TempestWing)
    {
        Area = new() { CastTypeOverride = CastType.SingleTarget },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.4f },
    };

    // UNVERIFIED: its damage; the holder is spared.
    public static readonly EnemyAction TempestWingBlast = new(ActionId.TempestWingBlast)
    {
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "stood next to a Tempest Wing tether holder",
    };

    // UNVERIFIED: the half-angle, and that anyone but the target dies in it.
    public static readonly EnemyAction FlareBreath = new(ActionId.FlareBreath)
    {
        Area = new() { Size = MathF.PI / 4f },
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.5f },
        DeathExplanation = "stay out from in front of Bahamut (Flare Breath)",
    };

    public static readonly EnemyAction LiquidHell = new(ActionId.LiquidHell)
    {
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    // UNVERIFIED: the minimum.
    public static readonly EnemyAction ThermionicBeam = new(ActionId.ThermionicBeam)
    {
        Effects = [Damage(split: Stack(4))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack with the party for Thermionic Beam",
    };

    public static readonly EnemyAction Hypernova = new(ActionId.Hypernova)
    {
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction MegaflareTower = new(ActionId.MegaflareTower)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    // UNVERIFIED: an unsoaked tower's punishment.
    public static readonly EnemyAction MegaflareStrike = new(ActionId.MegaflareStrike)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a Megaflare tower went unsoaked",
    };

    public static readonly EnemyAction Gigaflare = new(ActionId.Gigaflare)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.6f },
    };
}
