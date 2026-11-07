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

    // -- Golden Bahamut --

    // UNVERIFIED: the sheet's shape is Custom; everyone stood within 3y of the target.
    // Fewer than all eight and the stack is too heavy for the rest.
    public static readonly EnemyAction MornAfah = new(ActionId.MornAfah)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(split: Distribution.Stack(8))],
        DeathExplanation = "Morn Afah is shared by the whole party: stack on the marked player",
    };

    // Akh Morn is a tank share: two in it, or a lone tank under an invuln.
    public static readonly EnemyAction AkhMorn = new(ActionId.AkhMorn)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [AkhMornHit],
        DeathExplanation = "Akh Morn: share it with the other tank, or take it alone under an invuln",
    };

    public static readonly EnemyAction AkhMornFollowUp = new(ActionId.AkhMornFollowUp)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [AkhMornHit],
        DeathExplanation = "Akh Morn: share it with the other tank, or take it alone under an invuln",
    };

    private static IEnemyActionEffect AkhMornHit => Damage(TankBuster, Distribution.Stack(2, understacked: TankBuster.MinMit(1f)));

    public static readonly EnemyAction MornAfahEnrage = new(ActionId.MornAfahEnrage)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction FlamesOfRebirth = new(ActionId.FlamesOfRebirth)
    {
        Cast = new() { AnimationLock = 1.0f }, // UNVERIFIED
        Effects = [ApplyStatus(StatusId.Transcendent, 6f), ApplyStatus(StatusId.PhoenixsBlessing, 0f)],
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
    // -- P3 Bahamut Prime's trios --

    public static readonly EnemyAction LunarDynamo = new(ActionId.LunarDynamo)
    {
        Area = new() { Size = Geometry.LunarDynamoInner },
        Effects = [Damage(Lethal)],
        DeathExplanation = "outside Nael's melee range for Lunar Dynamo (\"hallowed moon\": get in)",
    };

    // UNVERIFIED: whoever else it lands on is knocked back like Heavensfall.
    public static readonly EnemyAction RavenDive = new(ActionId.RavenDive)
    {
        Effects = [OnTarget(Damage()), OnOthers(Knockback(HeavensfallKnockback))],
    };

    // UNVERIFIED: a second player under one kills both.
    public static readonly EnemyAction MeteorStream = new(ActionId.MeteorStream)
    {
        Effects = [Damage(split: Distribution.Exactly(1))],
        DeathExplanation = "shared a Meteor Stream (spread for them)",
    };

    // UNVERIFIED: a non-tank holding it dies.
    public static readonly EnemyAction TempestWingSplash = new(ActionId.TempestWingSplash)
    {
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        DeathExplanation = "stood near a Tempest Wing tether as it went off (tanks take them away from the party)",
    };

    public static readonly EnemyAction AethericProfusion = new(ActionId.AethericProfusion)
    {
        Effects = [Damage(ProtectedBy(StatusId.Neurolink), Lethal)],
        DeathExplanation = "outside a Neurolink for Aetheric Profusion",
    };

    public static readonly EnemyAction Flatten = new(ActionId.Flatten)
    {
        Effects = [Damage(TankBuster)],
    };

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

    public static readonly EnemyAction Heavensfall = new(ActionId.Heavensfall)
    {
        Effects = [Damage(), Knockback(HeavensfallKnockback)],
    };

    public static readonly EnemyAction ThermionicBurst = new(ActionId.ThermionicBurst)
    {
        Area = new() { Size = Geometry.ThermionicBurstHalfAngle },
        Effects = [Damage(Lethal)],
        DeathExplanation = "stood in a Thermionic Burst slice (follow behind the sweep)",
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

    public static readonly EnemyAction Fireball = new(ActionId.Fireball)
    {
        Effects = [Damage(split: Distribution.Stack(FireballMinimumStack))],
        DeathExplanation = "Fireball wasn't shared (stack on the marker)",
    };

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

    public static readonly EnemyAction[] Cauterize =
    [
        Dive(ActionId.CauterizeFirehorn, "Firehorn's Cauterize"),
        Dive(ActionId.CauterizeIceclaw, "Iceclaw's Cauterize"),
        Dive(ActionId.CauterizeThunderwing, "Thunderwing's Cauterize"),
        Dive(ActionId.CauterizeTail, "Tail of Darkness's Cauterize"),
        Dive(ActionId.CauterizeFang, "Fang of Light's Cauterize"),
    ];

    // UNVERIFIED: shared by two it is survivable; a tank LB3 lets one take it alone.
    public static readonly EnemyAction MegaflareStackOfTwo = new(ActionId.MegaflareStack)
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

    // UNVERIFIED: a Meteor Stream on someone else's spot kills them; its target lives.
    public static readonly EnemyAction MeteorStreamSpread = new(ActionId.MeteorStream)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [OnTarget(Damage()), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.5f },
        DeathExplanation = "caught in someone else's Meteor Stream (stay spread)",
    };
}
