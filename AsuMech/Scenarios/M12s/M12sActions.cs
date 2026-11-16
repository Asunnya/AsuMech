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
    private const float DamageDownSeconds = 30f;
    private const float FireResistanceDownSeconds = 25f;
    private const float DarkResistanceDownSeconds = 24f;
    private const float HalfRoom = MathF.PI / 2f;
    private static readonly VulnSpec VulnToFire = VulnerableTo(StatusId.FireResistanceDownII);
    private static readonly VulnSpec VulnToDark = VulnerableTo(StatusId.DarkResistanceDownII);

    public static readonly EnemyAction ArcadiaAflame = new(ActionId.ArcadiaAflame)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    // Not lethal on its own: a hit leaves Damage Down.
    public static readonly EnemyAction WingedScourge = new(ActionId.WingedScourge)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = MathF.PI / 12f },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay out of the clones' narrow cones",
    };

    // On the player nearest the fire clone, shared with a partner.
    public static readonly EnemyAction TopTierSlam = new(ActionId.TopTierSlam)
    {
        Effects = [Damage(VulnToFire, split: Stack(2)), ApplyStatus(StatusId.FireResistanceDownII, FireResistanceDownSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "share the fire with a partner who has no fire debuff",
    };

    // On each of the two players nearest the dark clone; a partner beside them is meant to be hit too.
    public static readonly EnemyAction MightyMagic = new(ActionId.MightyMagic)
    {
        Effects = [Damage(VulnToDark), ApplyStatus(StatusId.DarkResistanceDownII, DarkResistanceDownSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a second dark hit kills: with a dark debuff, bait fire instead",
    };

    // UNVERIFIED: lethality; of three players hit, two lived with Damage Down.
    public static readonly EnemyAction SnakingKick = new(ActionId.SnakingKick)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = HalfRoom },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "get behind the boss",
    };

    public static readonly EnemyAction DoubleSobat = new(ActionId.DoubleSobat)
    {
        Cast = new() { AnimationLock = 0.6f },
    };

    // The marked tank lives through it only on an invuln; anyone else in the half it kicks dies.
    public static EnemyAction DoubleSobatBuster(int turns) => new(ActionId.DoubleSobatBuster + (uint)turns)
    {
        Area = new() { CastTypeOverride = CastType.Cone, Size = HalfRoom },
        Effects = [OnTarget(Damage(TankBuster.MinMit(1f))), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the marked tank takes the kick on an invuln, everyone else behind the boss",
    };

    public static readonly EnemyAction DoubleSobatRepeat = new(ActionId.DoubleSobatRepeat)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = HalfRoom },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the second kick turns with the first one's animation",
    };

    // UNVERIFIED: a tank caught in the other tank's circle dies.
    public static readonly EnemyAction EsotericFinisher = new(ActionId.EsotericFinisher)
    {
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "each tank takes their circle away from the party and the other tank",
    };
}
