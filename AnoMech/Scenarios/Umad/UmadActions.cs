using System;
using AnoMech.Core;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using P1 = AnoMech.Scenarios.Umad.P1TeleTrouncing.UmadP1TeleTrouncingConstants;
using P3 = AnoMech.Scenarios.Umad.P3LimitCut.UmadP3LimitCutConstants;
using P5C = AnoMech.Scenarios.Umad.P5Celestriad.UmadP5CelestriadConstants;

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

    public static readonly EnemyAction IdyllicWill = new(P1.ActionId.IdyllicWill)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic), MagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction ThrummingThunderReal1 = ThrummingThunder(P1.ActionId.ThrummingThunderReal1);
    public static readonly EnemyAction ThrummingThunderReal2 = ThrummingThunder(P1.ActionId.ThrummingThunderReal2);

    private static EnemyAction ThrummingThunder(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = P1.AnimationLock.Helper },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.65f },
    };

    public static readonly EnemyAction IndolentWill = StatueGaze(P1.ActionId.IndolentWill, lookAway: true);
    public static readonly EnemyAction AveMaria = StatueGaze(P1.ActionId.AveMaria, lookAway: false);

    private static EnemyAction StatueGaze(uint actionId, bool lookAway) => new(actionId)
    {
        Effects = [Gaze(lookAway)],
        Timing = new() { DamageDelay = 0.84f },
    };

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

    public static readonly EnemyAction Nothingness = new(UmadConstants.ActionId.Nothingness)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [new NothingnessEffect()],
        Timing = new() { DamageDelay = 0.5f },
    };

    // -- P3 Limit Cut --

    public static readonly EnemyAction UmbraSmash = new(P3.ActionId.UmbraSmash)
    {
        Cast = new() { AnimationLock = P3.AnimationLock.UmbraSmash },
        Effects = [Damage(DamageType.Physical, split: Falloff(20f))],
        Timing = new() { DamageDelay = 1.78f },
    };

    public static readonly EnemyAction VacuumWave = new(P3.ActionId.VacuumWave)
    {
        Cast = new() { AnimationLock = P3.AnimationLock.VacuumWave },
        Effects =
        [
            Knockback(VacuumWavePush, P3.Timing.KnockbackSpeed, knockbackDelay: 0.78f),
            RemoveStatus(P3.StatusId.Headwind),
            RemoveStatus(P3.StatusId.Tailwind),
        ],
        Timing = new() { DamageDelay = 0.84f },
    };

    private static float? VacuumWavePush(EnemyActionContext ctx, SimCharacter target)
    {
        var headwind = target.HasStatus(P3.StatusId.Headwind);
        if (!headwind && !target.HasStatus(P3.StatusId.Tailwind)) return 20f;
        var facing = target.Placement();
        var from = ctx.Caster.Position;
        return (headwind ? facing.IsLookingAwayFrom(from) : facing.IsLookingAt(from)) ? 10f : 40f;
    }

    public static readonly EnemyAction UltimaBlaster = new(P3.ActionId.UltimaBlaster)
    {
        Cast = new() { AnimationLock = P3.AnimationLock.CloneAppear },
        Effects = [Damage(Magic)],
        Timing = new() { DamageDelay = 0.38f },
    };

    public static readonly EnemyAction UltimaBlasterCharge = new(P3.ActionId.UltimaBlasterCharge)
    {
        Cast = new() { AnimationLock = P3.AnimationLock.CloneCharge },
        Effects = [Damage(Magic, split: Falloff(35f)), ApplyStatus(UmadConstants.StatusId.MagicVulnerabilityUp, 2.96f)],
        Timing = new() { DamageDelay = 0.36f },
    };

    public static readonly EnemyAction Cyclone = new(UmadConstants.ActionId.Cyclone)
    {
        Cast = new() { AnimationLock = P3.AnimationLock.Cyclone },
        Effects =
        [
            Damage(DamageType.Wind.VulnerableTo(P3.StatusId.WindResistanceDownII, 0.80f), split: Stack(2, understacked: TankBuster.MinMit(0.80f))),
            ApplyStatus(P3.StatusId.WindResistanceDownII, 0.96f),
        ],
        Timing = new() { DamageDelay = 0.63f },
    };

    // -- P4 Kefka Says --

    public static readonly EnemyAction BlizzardIIIBlowout = MysteryBlizzard(UmadConstants.ActionId.BlizzardIIIBlowout_Real);
    public static readonly EnemyAction BlizzardIIIBlowoutLie = MysteryBlizzard(UmadConstants.ActionId.BlizzardIIIBlowout_FakeAnim);

    private static EnemyAction MysteryBlizzard(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 4 },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction ThrummingThunderIII = MysteryThunder(UmadConstants.ActionId.ThrummingThunderIII_Real);
    public static readonly EnemyAction ThrummingThunderIIILie = MysteryThunder(UmadConstants.ActionId.ThrummingThunderIII_FakeAnim);

    private static EnemyAction MysteryThunder(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction EdgeOfDeath = new(UmadConstants.ActionId.EdgeOfDeath)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Magic, Lethal)],
    };

    public static readonly EnemyAction WhiteAntilight = Antilight(UmadConstants.ActionId.WhiteAntilight, debuffsTrue: true, UmadConstants.StatusId.WhiteWound);
    public static readonly EnemyAction WhiteAntilightLie = Antilight(UmadConstants.ActionId.WhiteAntilight, debuffsTrue: false, UmadConstants.StatusId.WhiteWound);
    public static readonly EnemyAction BlackAntilight = Antilight(UmadConstants.ActionId.BlackAntilight, debuffsTrue: true, UmadConstants.StatusId.BlackWound);
    public static readonly EnemyAction BlackAntilightLie = Antilight(UmadConstants.ActionId.BlackAntilight, debuffsTrue: false, UmadConstants.StatusId.BlackWound);

    private static EnemyAction Antilight(uint actionId, bool debuffsTrue, ushort wound) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects =
        [
            new AntilightEffect(
                lethalWound: debuffsTrue ? wound : OtherWound(wound),
                cleansedBy: debuffsTrue ? UmadConstants.StatusId.BeyondDeath : UmadConstants.StatusId.AllaganField,
                leaves: wound),
        ],
        Timing = new() { DamageDelay = 0.13f },
    };

    private static ushort OtherWound(ushort wound)
        => wound == UmadConstants.StatusId.WhiteWound ? UmadConstants.StatusId.BlackWound : UmadConstants.StatusId.WhiteWound;

    public static readonly EnemyAction DeathBoltStack = DeathElement(UmadConstants.ActionId.DeathBolt, stack: true);
    public static readonly EnemyAction DeathBoltSpread = DeathElement(UmadConstants.ActionId.DeathBolt, stack: false);
    public static readonly EnemyAction DeathWaveStack = DeathElement(UmadConstants.ActionId.DeathWave, stack: true);
    public static readonly EnemyAction DeathWaveSpread = DeathElement(UmadConstants.ActionId.DeathWave, stack: false);

    private static EnemyAction DeathElement(uint actionId, bool stack) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [stack ? Damage(Magic, split: Stack(3)) : Damage(Magic), LongMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction DeathShriekLookAway = DeathShriek(lookAway: true);
    public static readonly EnemyAction DeathShriekLookAt = DeathShriek(lookAway: false);

    private static EnemyAction DeathShriek(bool lookAway) => new(UmadConstants.ActionId.DeathShriek)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Gaze(lookAway)],
    };

    public static readonly EnemyAction StrayFlamesChariot = StrayBait(UmadConstants.ActionId.StrayFlames_Chariot);
    public static readonly EnemyAction StrayFlamesDonut = StrayBait(UmadConstants.ActionId.StrayFlames_Donut);
    public static readonly EnemyAction StraySprayDonut = StrayBait(UmadConstants.ActionId.StraySpray_Donut);
    public static readonly EnemyAction StraySprayChariot = StrayBait(UmadConstants.ActionId.StraySpray_Chariot);

    private static EnemyAction StrayBait(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.1f, OmenDelay = 4f },
        Area = new() { Size = 6f },
        Effects = [Damage(Magic, Lethal)],
    };

    public static readonly EnemyAction DeathBomb = new(UmadConstants.ActionId.DeathBomb)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { DamageDelay = 0.09f },
        DeathExplanation = "failed Acceleration Bomb",
    };

    public static readonly EnemyAction DeathSurge = new(UmadConstants.ActionId.DeathSurge)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(DamageType.Unique)],
        Timing = new() { DamageDelay = 0.8f },
    };

    public static readonly EnemyAction DeathSurgeWipe = DeathSurge with
    {
        Effects = [Damage(DamageType.Unique, Lethal)],
        DeathExplanation = "Allagan Field holder died",
    };

    // -- P5 Celestriad --

    public static readonly EnemyAction CelestriadFireTower = CelestriadTower(
        P5C.CelestriadActionId.FireIII, P5C.CelestriadActionId.StardustFireIII,
        DamageType.Fire, P5C.CelestriadStatusId.FireResistanceDownII);

    public static readonly EnemyAction CelestriadIceTower = CelestriadTower(
        P5C.CelestriadActionId.BlizzardIII, P5C.CelestriadActionId.StardustBlizzardIII,
        DamageType.Ice, P5C.CelestriadStatusId.IceResistanceDownII);

    public static readonly EnemyAction CelestriadLightningTower = CelestriadTower(
        P5C.CelestriadActionId.ThunderIII, P5C.CelestriadActionId.StardustThunderIII,
        DamageType.Lightning, UmadConstants.StatusId.LightningResistanceDownII);

    private static EnemyAction CelestriadTower(uint actionId, uint stardustId, DamageType element, ushort resistanceDown) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects =
        [
            Damage(new DamageSpec(DamageType.Magic, element).VulnerableTo(resistanceDown), split: Stack(2)),
            ApplyStatus(resistanceDown, P5C.CelestriadTiming.DebuffDuration),
            FollowUp(Stardust(stardustId), ctx => ctx.Hits.Count == 0),
        ],
    };

    private static EnemyAction Stardust(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects =
        [
            Damage(DamageType.Magic),
            ApplyStatus(UmadConstants.StatusId.DamageDown, P5C.CelestriadTiming.DamageDownDuration),
        ],
    };

    public static readonly EnemyAction CatastrophicChoiceAero = new(P5C.CelestriadActionId.CatastrophicChoiceAeroResolution)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = 10f },
        Effects = [Damage(Magic, Lethal)],
    };

    public static readonly EnemyAction CatastrophicChoiceEarth = new(P5C.CelestriadActionId.CatastrophicChoiceEarthResolution)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic, Lethal)],
    };

    private sealed class AntilightEffect(ushort lethalWound, ushort cleansedBy, ushort leaves) : IEnemyActionEffect
    {
        public void Apply(EnemyActionContext ctx)
        {
            foreach (var target in ctx.Hits)
            {
                if (ctx.IsKilled(target) || !target.IsAlive()) continue;
                var lethal = target.HasStatus(lethalWound);
                target.RemoveStatus(UmadConstants.StatusId.WhiteWound);
                target.RemoveStatus(UmadConstants.StatusId.BlackWound);
                if (!lethal)
                    target.AddStatus(leaves, 0f);
                else if (target.HasStatus(cleansedBy))
                    target.RemoveStatus(cleansedBy);
                else
                    ctx.Kill(target, $"carried {StatusLookup.Name(lethalWound)}");
            }
        }
    }

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
