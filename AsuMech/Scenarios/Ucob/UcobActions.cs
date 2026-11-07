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

    public static readonly EnemyAction Gigaflare = new(ActionId.Gigaflare)
    {
        Effects = [Damage()],
    };

    public static readonly EnemyAction Flatten = new(ActionId.Flatten)
    {
        Effects = [Damage(TankBuster)],
    };

    // UNVERIFIED: lethal; nobody was caught by a dive.
    public static readonly EnemyAction MegaflareDive = new(ActionId.MegaflareDive)
    {
        Effects = [Damage(Lethal)],
        DeathExplanation = "stood in Bahamut Prime's Megaflare Dive",
    };

    // UNVERIFIED: lethal; nobody was caught by a dive.
    public static readonly EnemyAction TwistingDive = new(ActionId.TwistingDive)
    {
        Effects = [Damage(Lethal)],
        DeathExplanation = "stood in Twintania's Twisting Dive",
    };

    public static readonly EnemyAction TwisterBlast = new(ActionId.TwisterBlast)
    {
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(Lethal)],
        DeathExplanation = "a Twister went off nearby (step off yours, and keep off the others)",
    };

    public static readonly EnemyAction MegaflareTower = new(ActionId.MegaflareTower)
    {
        Effects = [Damage()],
    };

    public static readonly EnemyAction MegaflareStrike = new(ActionId.MegaflareStrike)
    {
        Effects = [Damage(Lethal)],
        DeathExplanation = "a Megaflare tower went unsoaked",
    };

    // UNVERIFIED: lethal; nobody stood in one.
    public static readonly EnemyAction MegaflarePuddle = new(ActionId.MegaflarePuddle)
    {
        Effects = [Damage(Lethal)],
        DeathExplanation = "stood in a Megaflare puddle (bait them away from the party)",
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
    };

    public static readonly EnemyAction Fireball = new(ActionId.Fireball)
    {
        Effects = [Damage(split: Distribution.Stack(FireballMinimumStack))],
        DeathExplanation = "Fireball wasn't shared (stack on the marker)",
    };
}
