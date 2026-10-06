using System;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using P1 = AnoMech.Scenarios.Umad.P1TeleTrouncing.UmadP1TeleTrouncingConstants;

namespace AnoMech.Scenarios.Umad;

public static class UmadActions
{
    internal static readonly DamageSpec Magic = DamageType.Magic.VulnerableTo(UmadConstants.StatusId.MagicVulnerabilityUp);

    private static readonly IEnemyActionEffect MagicVulnerabilityUp =
        ApplyStatus(UmadConstants.StatusId.MagicVulnerabilityUp, P1.MagicVulnerabilityUpSeconds);

    internal static readonly IEnemyActionEffect LongMagicVulnerabilityUp =
        ApplyStatus(UmadConstants.StatusId.MagicVulnerabilityUp, 1.96f);

    public static readonly EnemyAction AutoAttack = new(UmadConstants.ActionId.AutoAttack1)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.AutoAttack },
        Effects = [Damage(DamageType.Physical)],
        Timing = new() { DamageDelay = 0.8f },
    };

    // -- P1 Tele-trouncing --

    // Cast on the holder, who counts toward the stack but is neither pushed nor given the vuln.
    public static readonly EnemyAction DoubleTroubleTrapStack = new(P1.ActionId.DoubleTroubleTrapStack)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects =
        [
            Damage(Magic, split: Stack(4)),
            OnOthers(MagicVulnerabilityUp),
            OnOthers(Knockback(P1.KnockbackId.DoubleTroubleTrapStack, knockbackDelay: 0f)),
        ],
        Timing = new() { DamageDelay = 0.67f },
    };

    public static readonly EnemyAction IndulgentWill = new(P1.ActionId.IndulgentWill)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic)],
        Timing = new() { DamageDelay = 0.63f },
    };

    // A spread on the sleeper it's cast on: lethal only through its vuln, to a sleeper it hits twice
    // or a Confused player whose Indulgent Will lands after it.
    public static readonly EnemyAction IdyllicWill = new(P1.ActionId.IdyllicWill)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic), MagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.63f },
    };

    // Truth: 2x Real1. Lie: Real2 (no telegraph) at the real slots, Fake at the other two.
    public static readonly EnemyAction ThrummingThunderReal1 = ThrummingThunder(P1.ActionId.ThrummingThunderReal1);
    public static readonly EnemyAction ThrummingThunderReal2 = ThrummingThunder(P1.ActionId.ThrummingThunderReal2);

    private static EnemyAction ThrummingThunder(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.65f },
    };

    // Cast by the gazing statue, whose position is the gaze's source.
    public static readonly EnemyAction IndolentWill = StatueGaze(P1.ActionId.IndolentWill, lookAway: true);
    public static readonly EnemyAction AveMaria = StatueGaze(P1.ActionId.AveMaria, lookAway: false);

    private static EnemyAction StatueGaze(uint actionId, bool lookAway) => new(actionId)
    {
        Effects = [Gaze(lookAway)],
        Timing = new() { DamageDelay = 0.84f },
    };

    // One per player: a second circle on anyone is lethal through the first's vuln.
    public static readonly EnemyAction FlagrantFireSpread = new(P1.ActionId.FlagrantFireSpread)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic), MagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction FlagrantFireStack = new(P1.ActionId.FlagrantFireStack)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic, split: Stack(4)), MagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.71f },
    };

    // -- P2 Forsaken --

    public static readonly EnemyAction Forsaken = new(UmadConstants.ActionId.Forsaken)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(Magic)],
        Timing = new() { DamageDelay = 1.61f },
    };

    // Whatever Spells' Trouble is left by now makes it lethal.
    public static readonly EnemyAction LightOfJudgment = new(UmadConstants.ActionId.LightOfJudgment)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(Magic.VulnerableTo(UmadConstants.StatusId.SpellsTrouble))],
        Timing = new() { DamageDelay = 0.8f },
    };

    private static readonly EnemyAction TheRiverOfLight = new(UmadConstants.ActionId.TheRiverOfLight)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, Lethal)],
        DeathExplanation = "a tower not taken by exactly two",
    };

    // Takes exactly two; any other count sets off the River of Light.
    public static readonly EnemyAction ThePathOfLight = new(UmadConstants.ActionId.ThePathOfLight)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [FollowUp(TheRiverOfLight, ctx => ctx.Hits.Count != 2)],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction Spelldriver = new(UmadConstants.ActionId.Spelldriver)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, split: Stack(3)), LongMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction Spellscatter = new(UmadConstants.ActionId.Spellscatter)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic), LongMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.62f },
    };

    // Cast from its holder's spot; the holder stands at the apex and is never hit by it.
    public static readonly EnemyAction Spellwave = new(UmadConstants.ActionId.Spellwave)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 4, ExcludeCaster = true },
        Effects = [Damage(Magic), LongMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.62f },
    };

    // -- P3 Black Hole --

    public static readonly EnemyAction ExdeathAutoAttack = new(UmadConstants.ActionId.AutoAttack2)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.AutoAttack },
        Effects = [Damage(DamageType.Physical)],
        Timing = new() { DamageDelay = 0.8f },
    };

    public static readonly EnemyAction SlapHappySlap = new(UmadConstants.ActionId.SlapHappy_Slap)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(DamageType.Physical, Lethal)],
    };

    public static readonly EnemyAction SlapHappyFinalSlap = new(UmadConstants.ActionId.SlapHappy_FinalSlap)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(DamageType.Physical, Lethal)],
    };

    // Half-angle estimated from the animation.
    private const float SlapConeHalfAngle = MathF.PI / 6;

    public static readonly EnemyAction ShockwaveCone = new(UmadConstants.ActionId.ShockwaveCone)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = SlapConeHalfAngle },
        Effects = [Damage(Magic), LongMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.65f },
    };

    public static readonly EnemyAction ShockingImpact = new(UmadConstants.ActionId.ShockingImpact)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = SlapConeHalfAngle },
        Effects = [Damage(Magic, split: Stack(8)), LongMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction DamningEdict = new(UmadConstants.ActionId.DamningEdict)
    {
        Cast = new() { AnimationLock = 3.1f, OmenDelay = 4f },
        Effects = [Damage(Magic, Lethal)],
    };

    public static readonly EnemyAction LookUponMeAndDespair = new(UmadConstants.ActionId.LookUponMeAndDespair_Omen)
    {
        Cast = new() { AnimationLock = 1.1f, OmenDelay = 4f },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.1f },
    };

    // 929,000 unmitigated against a 325,047 tank is 65%; eased while only self mitigation counts.
    // The Lightning Resistance Down it leaves outlasts the 3s to the set's second hit, so one tank
    // takes both only behind an invuln.
    public static readonly EnemyAction ThunderIII = new(UmadConstants.ActionId.ThunderIII_Resolve)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects =
        [
            Damage(Magic.VulnerableTo(UmadConstants.StatusId.LightningResistanceDownII), TankBuster.MinMit(0.60f)),
            ApplyStatus(UmadConstants.StatusId.LightningResistanceDownII, 3.96f),
        ],
        Timing = new() { DamageDelay = 0.22f },
    };

    public static readonly EnemyAction ImplosionShockwave = new(UmadConstants.ActionId.Shockwave)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 4 },
        Effects = [Damage(Magic, Lethal)],
    };

    public static readonly EnemyAction BlizzardIII = new(UmadConstants.ActionId.BlizzardIII)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, Lethal)],
    };

    public static readonly EnemyAction KnockDown = new(UmadConstants.ActionId.KnockDown)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, split: Stack(4)), LongMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.48f },
    };

    private static readonly EnemyAction UnmitigatedImpact = new(UmadConstants.ActionId.UnmitigatedImpact)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, Lethal)],
        DeathExplanation = "a tower nobody took",
    };

    public static readonly EnemyAction StompAMole = new(UmadConstants.ActionId.StompAMole)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects =
        [
            Damage(Magic, split: Stack(2)),
            ApplyStatus(UmadConstants.StatusId.MagicVulnerabilityUp, 3f),
            FollowUp(UnmitigatedImpact, ctx => ctx.Hits.Count == 0),
        ],
        Timing = new() { DamageDelay = 0.17f },
    };

    // Cast on the Accretion holder it cleanses, who takes no damage; a crust's cleanse has no target.
    // Anyone still carrying the last one's Earth Resistance Down dies.
    public static readonly EnemyAction EarthquakeCleanse = new(UmadConstants.ActionId.Earthquake_Cleanse)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects =
        [
            OnOthers(Damage(DamageType.Earth.VulnerableTo(UmadConstants.StatusId.EarthResistanceDownII))),
            OnOthers(ApplyStatus(UmadConstants.StatusId.EarthResistanceDownII, 1.96f)),
            OnTarget(RemoveStatus(UmadConstants.StatusId.Accretion)),
        ],
        Timing = new() { DamageDelay = 0.64f },
    };

    // Each hit climbs Unbecoming -> Meanest Existence; one more spends the Primordial Crust, and a hit
    // past that kills.
    public static readonly EnemyAction Nothingness = new(UmadConstants.ActionId.Nothingness)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [new NothingnessEffect()],
        Timing = new() { DamageDelay = 0.5f },
    };

    private sealed class NothingnessEffect : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx)
        {
            foreach (var target in ctx.Hits)
            {
                if (ctx.IsKilled(target) || !target.IsAlive()) continue;
                if (target.HasStatus(UmadConstants.StatusId.MeanestExistence))
                {
                    if (!target.HasStatus(UmadConstants.StatusId.PrimordialCrust))
                    {
                        ctx.Kill(target, "hit with Meanest Existence and no Primordial Crust");
                        continue;
                    }
                    target.RemoveStatus(UmadConstants.StatusId.PrimordialCrust);
                    target.RemoveStatus(UmadConstants.StatusId.FirstInLine);
                    target.RemoveStatus(UmadConstants.StatusId.SecondInLine);
                    target.RemoveStatus(UmadConstants.StatusId.ThirdInLine);
                }
                else if (target.HasStatus(UmadConstants.StatusId.Unbecoming))
                {
                    target.RemoveStatus(UmadConstants.StatusId.Unbecoming);
                    target.AddStatus(UmadConstants.StatusId.MeanestExistence);
                }
                else
                {
                    target.AddStatus(UmadConstants.StatusId.Unbecoming);
                }
            }
        }
    }
}
