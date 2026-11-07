using System;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Native.Interfaces;
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

    // -- Twintania --

    // Inside a Neurolink one Hatch is survivable; it leaves Mana Hypersensitivity for the next.
    public static readonly EnemyAction Hatch = new(ActionId.Hatch)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects =
        [
            Damage(VulnerableTo(StatusId.ManaHypersensitivity)),
            Damage(ProtectedBy(StatusId.Neurolink), Lethal),
            ApplyStatus(StatusId.ManaHypersensitivity, 15.96f),
        ],
        DeathExplanation = "caught in a Hatch outside a Neurolink or with Mana Hypersensitivity (one Hatch each, inside a Neurolink)",
    };

    public static readonly EnemyAction DeepHatch = new(ActionId.DeepHatch)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "Deep Hatch: an Oviform reached someone outside a Neurolink",
    };

    public static readonly EnemyAction TwisterBlast = new(ActionId.TwisterBlast)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(Lethal)],
        DeathExplanation = "launched into the wall by a Twister someone touched (step off your spot as it drops, then hold still)",
    };

    // UNVERIFIED: every dive is lethal.
    public static readonly EnemyAction TwistingDive = Dive(ActionId.TwistingDive, "Twintania's Twisting Dive");

    // -- Nael and the dragons --

    // UNVERIFIED: a Meteor Stream on someone else's spot kills.
    public static readonly EnemyAction MeteorStream = new(ActionId.MeteorStream)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [OnTarget(Damage()), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.5f },
        DeathExplanation = "caught in someone else's Meteor Stream (stay spread)",
    };

    public static readonly EnemyAction LunarDive = Dive(ActionId.LunarDive, "Nael's Lunar Dive");

    public static readonly EnemyAction[] Cauterize =
    [
        Dive(ActionId.CauterizeFirehorn, "Firehorn's Cauterize"),
        Dive(ActionId.CauterizeIceclaw, "Iceclaw's Cauterize"),
        Dive(ActionId.CauterizeThunderwing, "Thunderwing's Cauterize"),
        Dive(ActionId.CauterizeTail, "Tail of Darkness's Cauterize"),
        Dive(ActionId.CauterizeFang, "Fang of Light's Cauterize"),
    ];

    // -- Bahamut Prime --

    public static readonly EnemyAction Gigaflare = new(ActionId.Gigaflare)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    // UNVERIFIED: a 90° cone.
    public static readonly EnemyAction EarthShakerCone = new(ActionId.EarthShakerCone)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 4f },
        Effects = [Damage(VulnerableTo(StatusId.EarthResistanceDownII)), ApplyStatus(StatusId.EarthResistanceDownII, 11.96f)],
        DeathExplanation = "took a second Earth Shaker with Earth Resistance Down II (aim your cone away from the others and from 1)",
    };

    public static readonly EnemyAction MegaflareDive = Dive(ActionId.MegaflareDive, "Bahamut's Megaflare Dive");

    public static readonly EnemyAction MegaflareTower = new(ActionId.MegaflareTower)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
    };

    // UNVERIFIED: an unsoaked tower wipes.
    public static readonly EnemyAction MegaflareStrike = new(ActionId.MegaflareStrike)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "a Megaflare tower went unsoaked",
    };

    // UNVERIFIED: shared by two it is survivable; a tank LB3 lets one take it alone.
    public static readonly EnemyAction MegaflareStack = new(ActionId.MegaflareStack)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects =
        [
            Damage(ProtectedBy(StatusId.LastBastion).ProtectedBy(StatusId.LandWaker).ProtectedBy(StatusId.DarkForce).ProtectedBy(StatusId.GunmetalSoul),
                split: Distribution.Stack(2)),
        ],
        DeathExplanation = "a Megaflare stack wasn't shared (stack the four marks together, or tank LB3)",
    };

    private static EnemyAction Dive(uint actionId, string name) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        DeathExplanation = $"stood in {name} (it dives at where its marked player stood)",
    };
}
