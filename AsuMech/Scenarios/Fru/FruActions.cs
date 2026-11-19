using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.EnemyActions.Distribution;
using static AsuMech.Core.EnemyActions.EnemyActionEffects;
using static AsuMech.Core.EnemyActions.Severity;
using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Scenarios.Fru;

public static class FruActions
{
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private static readonly VulnSpec VulnToMagicFire = VulnToMagic.VulnerableTo(StatusId.FireResistanceDownII);

    public static readonly EnemyAction Lightsteep = new(ActionId.Lightsteep)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.67f },
        DeathExplanation = "a fifth Lightsteeped stack exploded",
    };

    private static readonly IEnemyActionEffect AddLightsteeped = ApplyStatus(StatusId.Lightsteeped, 36f, delayToEffectResult: true);

    // -- P1 Fatebreaker --

    private static readonly VulnSpec VulnToPhysical = VulnerableTo(StatusId.PhysicalVulnerabilityUp);

    // UNVERIFIED width: 22.5°, so each later set sits one cone's width from the last.
    private const float CyclonicConeHalfAngle = MathF.PI / 16f;

    public static readonly EnemyAction CyclonicBreakBait = new(ActionId.CyclonicBreakBait)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = CyclonicConeHalfAngle },
        Effects = [Damage(VulnToPhysical), ApplyStatus(StatusId.PhysicalVulnerabilityUp, 1.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction CyclonicBreakRepeat = new(ActionId.CyclonicBreakRepeat)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = CyclonicConeHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stood in a later Cyclonic Break cone",
    };

    public static readonly EnemyAction CyclonicSinsmite = new(ActionId.CyclonicSinsmite)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction CyclonicSinsmoke = new(ActionId.CyclonicSinsmoke)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(2)), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction PowderMarkTrail = new(ActionId.PowderMarkTrail)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects =
        [
            Damage(TankBuster),
            ApplyStatus(StatusId.PowderMarkTrail, 15.98f),
            ApplyStatus(StatusId.PhysicalVulnerabilityUp, 13.98f, delayToEffectResult: true),
        ],
        Timing = new() { DamageDelay = 0.77f },
    };

    public static readonly EnemyAction BurnMark = new(ActionId.BurnMark)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction BlastingZone = new(ActionId.BlastingZone)
    {
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction SinboundFireIII = new(ActionId.SinboundFireIII)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction SinboundThunderIII = new(ActionId.SinboundThunderIII)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    private static EnemyAction BurntStrikeLine(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction BurntStrikeWest = BurntStrikeLine(ActionId.BurntStrikeWest);
    public static readonly EnemyAction BurnoutWest = BurntStrikeLine(ActionId.BurnoutWest);
    public static readonly EnemyAction BurntStrikeNorth = BurntStrikeLine(ActionId.BurntStrikeNorth);
    public static readonly EnemyAction BurntStrikeFire = BurntStrikeLine(ActionId.BurntStrikeFire);
    public static readonly EnemyAction BurntStrikeLightning = BurntStrikeLine(ActionId.BurntStrikeLightning);
    public static readonly EnemyAction BurnoutTowers = BurntStrikeLine(ActionId.BurnoutTowers);

    // A Knockback row each side of its line, pushing away from it.
    private static EnemyAction Blastburn(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects =
        [
            Damage(Harmless),
            Knockback(KnockbackId.BlastburnRight, (ctx, who) => RightOfCaster(ctx, who)),
            Knockback(KnockbackId.BlastburnLeft, (ctx, who) => !RightOfCaster(ctx, who)),
        ],
        Timing = new() { DamageDelay = 0.6f },
    };

    private static bool RightOfCaster(EnemyActionContext ctx, SimCharacter who)
    {
        var offset = who.Position - ctx.Caster.Position;
        return -offset.X * MathF.Cos(ctx.Caster.Rotation) + offset.Z * MathF.Sin(ctx.Caster.Rotation) > 0f;
    }

    public static readonly EnemyAction BlastburnNorth = Blastburn(ActionId.BlastburnNorth);
    public static readonly EnemyAction BlastburnTowers = Blastburn(ActionId.BlastburnTowers);

    private static EnemyAction Brightfire(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction BrightfireSmall = Brightfire(ActionId.BrightfireSmall);
    public static readonly EnemyAction BrightfireLarge = Brightfire(ActionId.BrightfireLarge);

    // Fewer than four in it leaves Mark of Mortality in retail.
    public static readonly EnemyAction BoundOfFaithSinsmoke = new(ActionId.BoundOfFaithSinsmoke)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction BurnishedGlory = new(ActionId.BurnishedGlory)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(), ApplyStatus(StatusId.BurnishedGloryBleeding, 14.95f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.8f },
    };

    // Snapshots with the blast, not the bar's end: a kill just after the bar still stops it.
    public static readonly EnemyAction BurnishedGloryEnrage = new(ActionId.BurnishedGloryEnrage)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { ResolveSnapshotOffset = CastSpec.ActionEffectOffset, DamageDelay = 0.5f },
        DeathExplanation = "Fatebreaker's enrage",
    };

    public static readonly EnemyAction FallOfFaithSinsmite = new(ActionId.FallOfFaithSinsmite)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 3.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction BowShock = new(ActionId.BowShock)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = MathF.PI / 3f, ExcludeCaster = true },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 3.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    // An LP stack: the cone needs three besides its holder, who stands at its point.
    public static readonly EnemyAction Sinblaze = new(ActionId.Sinblaze)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { Size = MathF.PI / 4f, AdjustTargets = WithCastTarget },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 3.94f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    private static IReadOnlyList<SimCharacter> WithCastTarget(
        EnemyActionContext ctx, IReadOnlyList<SimCharacter> hits)
        => ctx.Target is { } holder && holder.IsAlive() && !hits.Contains(holder) ? [holder, .. hits] : hits;

    private static EnemyAction Tower(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.5f },
    };

    public static readonly EnemyAction[] FireTowers = ActionId.FireTower.Select(Tower).ToArray();
    public static readonly EnemyAction[] LightningTowers = ActionId.LightningTower.Select(Tower).ToArray();

    // Retail's raidwide leaves Damage Down instead.
    private static EnemyAction UnmitigatedExplosion(uint actionId) => new(actionId)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "a tower went unsoaked",
    };

    public static readonly EnemyAction UnmitigatedExplosionFire = UnmitigatedExplosion(ActionId.UnmitigatedExplosionFire);
    public static readonly EnemyAction UnmitigatedExplosionLightning = UnmitigatedExplosion(ActionId.UnmitigatedExplosionLightning);

    // -- P2 Quadruple Slap --

    // Each hit lands for about 2.3x a tank's HP after stance, twice that on the slap's own vuln.
    private static readonly Severity QuadrupleSlapHit = TankBuster.MinMit(1f - 1f / 2.3f);

    public static readonly EnemyAction QuadrupleSlap = new(ActionId.QuadrupleSlap)
    {
        Effects =
        [
            Damage(QuadrupleSlapHit),
            ApplyStatus(StatusId.QuadrupleSlapVulnerability, 15f, delayToEffectResult: true),
        ],
        Timing = new() { DamageDelay = 1.06f },
    };

    public static readonly EnemyAction QuadrupleSlapSecond = new(ActionId.QuadrupleSlapSecond)
    {
        Effects = [Damage(VulnerableTo(StatusId.QuadrupleSlapVulnerability, 1f - 1f / 4.6f), QuadrupleSlapHit)],
        Timing = new() { DamageDelay = 1.06f },
    };

    // -- P2 Diamond Dust --

    public static readonly EnemyAction DiamondDust = new(ActionId.DiamondDust)
    {
        Cast = new() { AnimationLock = 2.6f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.18f },
    };

    public static readonly EnemyAction AxeKick = new(ActionId.AxeKick)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 1.09f },
    };

    public static readonly EnemyAction ScytheKick = new(ActionId.ScytheKick)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { Size = 4f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 1.09f },
    };

    public static readonly EnemyAction HouseOfLightDiamondDust = new(ActionId.HouseOfLightDiamondDust)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { Size = MathF.PI / 12f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction FrigidStone = new(ActionId.FrigidStone)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.26f },
    };

    public static readonly EnemyAction IcicleImpact = new(ActionId.IcicleImpact)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.93f },
    };

    public static readonly EnemyAction FrigidNeedleCircle = new(ActionId.FrigidNeedleCircle)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.93f },
    };

    public static readonly EnemyAction FrigidNeedleCross = new(ActionId.FrigidNeedleCross)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.93f },
    };

    public static readonly EnemyAction HeavenlyStrike = new(ActionId.HeavenlyStrike)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(), Knockback(KnockbackId.HeavenlyStrike, knockbackDelay: 0.78f)],
        Timing = new() { DamageDelay = 0.9f },
    };

    public static readonly EnemyAction SinboundHoly = new(ActionId.SinboundHoly)
    {
        Cast = new() { AnimationLock = 1.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction ShiningArmor = new(ActionId.ShiningArmor)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Gaze(lookAway: true)],
        Timing = new() { DamageDelay = 0.49f },
    };

    // The range-30 halves hit everything but the 90° behind the Mirror; the range-40 halves hit
    // only that 90°.
    private static EnemyAction TwinFront(uint actionId, float animationLock, float damageDelay) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock },
        Area = new() { Size = 3f * MathF.PI / 4f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = damageDelay },
    };

    private static EnemyAction TwinBack(uint actionId, float animationLock, float damageDelay) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock },
        Area = new() { Size = MathF.PI / 4f, Rotation = MathF.PI },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = damageDelay },
    };

    public static readonly EnemyAction TwinStillnessFirst = TwinFront(ActionId.TwinStillnessFirst, 2.1f, 1.1f);
    public static readonly EnemyAction TwinStillnessSecond = TwinBack(ActionId.TwinStillnessSecond, 3.1f, 0.9f);
    public static readonly EnemyAction TwinSilenceFirst = TwinBack(ActionId.TwinSilenceFirst, 2.1f, 1.1f);
    public static readonly EnemyAction TwinSilenceSecond = TwinFront(ActionId.TwinSilenceSecond, 3.1f, 0.81f);

    // -- P2 Light Rampant --

    public static readonly EnemyAction LightRampant = new(ActionId.LightRampant)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.19f },
    };

    public static readonly EnemyAction BrightHunger = new(ActionId.BrightHunger)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(), AddLightsteeped],
        Timing = new() { DamageDelay = 0.65f },
    };

    public static readonly EnemyAction BrightHungerCenter = BrightHunger with
    {
        Effects = [Damage(split: Stack(4)), AddLightsteeped],
    };

    public static readonly EnemyAction LuminousHammer = new(ActionId.LuminousHammer)
    {
        Cast = new() { AnimationLock = 1.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction PowerfulLight = new(ActionId.PowerfulLight)
    {
        Cast = new() { AnimationLock = 2.1f, FaceTarget = false },
        Effects = [Damage(split: Stack(4)), AddLightsteeped],
        Timing = new() { DamageDelay = 0.65f },
    };

    public static readonly EnemyAction Burst = new(ActionId.Burst)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.79f },
    };

    // Retail hits hard and leaves Damage Down rather than killing outright.
    public static readonly EnemyAction RefulgentFate = new(ActionId.RefulgentFate)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.66f },
        DeathExplanation = "an Everlasting Light chain broke",
    };

    public static readonly EnemyAction InescapableIllumination = new(ActionId.InescapableIllumination)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.63f },
        DeathExplanation = "a tower went unsoaked",
    };

    public static readonly EnemyAction BanishPairs = new(ActionId.BanishPairs)
    {
        Cast = new() { AnimationLock = 2.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(2)), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction BanishSpread = new(ActionId.BanishSpread)
    {
        Cast = new() { AnimationLock = 2.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction HouseOfLight = new(ActionId.HouseOfLight)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f), AddLightsteeped],
        Timing = new() { DamageDelay = 0.64f },
    };

    // -- P2 Mirror, Mirror --

    // UNVERIFIED how few it takes to kill: retail's whole party shares it.
    public static readonly EnemyAction HallowedRay = new(ActionId.HallowedRay)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(split: Stack(5))],
        Timing = new() { DamageDelay = 0.6f },
    };

    public static readonly EnemyAction ReflectedScytheKick = ScytheKick with { ActionId = ActionId.ReflectedScytheKick };

    public static readonly EnemyAction ReflectedScytheKickCast = ScytheKick with { ActionId = ActionId.ReflectedScytheKickCast };

    // -- Intermission --

    public static readonly EnemyAction AbsoluteZero = new(ActionId.AbsoluteZeroHit)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction SwellingFrost = new(ActionId.SwellingFrost)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Harmless), Push(10f, 10f), ApplyStatus(StatusId.DeepFreeze, 15.9f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.52f },
    };

    // UNVERIFIED that it kills: no pull has anyone standing in it.
    public static readonly EnemyAction HiemalStorm = new(ActionId.HiemalStorm)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "stood in Hiemal Storm",
    };

    public static readonly EnemyAction HiemalRay = new(ActionId.HiemalRay)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.6f },
    };

    // Hurts no player. A Crystal of Light it reaches can no longer be destroyed.
    public static readonly EnemyAction SinboundBlizzard = new(ActionId.SinboundBlizzard)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 18f },
        Effects = [OnNpcs(BNpcBaseId.CrystalOfLight, ApplyStatus(StatusId.VulnerabilityDown, 9999f))],
        Timing = new() { DamageDelay = 0.3f },
    };

    public static readonly EnemyAction EndlessIceAge = new(ActionId.EndlessIceAge)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.6f },
        DeathExplanation = "the Ice Veil was not broken in time",
    };

    // -- P3 --

    public static readonly EnemyAction UltimateRelativity = new(ActionId.UltimateRelativity)
    {
        Cast = new() { AnimationLock = 4.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.2f },
    };

    public static readonly EnemyAction DarkFire = new(ActionId.DarkFire)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects =
        [
            Damage(VulnToMagicFire),
            ApplyStatus(StatusId.MagicVulnerabilityUp, 0.95f),
            ApplyStatus(StatusId.FireResistanceDownII, 11.95f, delayToEffectResult: true),
        ],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction DarkBlizzard = new(ActionId.DarkBlizzard)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = 3f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction UnholyDarkness = new(ActionId.UnholyDarkness)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(5)), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.95f)],
        Timing = new() { DamageDelay = 0.76f },
    };

    public static readonly EnemyAction DarkWater = new(ActionId.DarkWater)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.51f },
    };

    public static readonly EnemyAction DarkEruption = new(ActionId.DarkEruption)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f)],
        Timing = new() { DamageDelay = 0.65f },
    };

    // Cast from a helper on its holder's spot, so the holder is the one excluded.
    public static readonly EnemyAction Shadoweye = new(ActionId.Shadoweye)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { ExcludeCaster = true },
        Effects = [Gaze(lookAway: true)],
    };

    public static readonly EnemyAction Quicken = new(ActionId.Quicken) { Cast = new() { AnimationLock = 1.1f, FaceTarget = false } };
    public static readonly EnemyAction Slow = new(ActionId.Slow) { Cast = new() { AnimationLock = 1.1f, FaceTarget = false } };

    public static readonly EnemyAction SinboundMeltdownFirst = new(ActionId.SinboundMeltdownFirst)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(VulnToMagicFire), ApplyStatus(StatusId.FireResistanceDownII, 11.96f)],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction SinboundMeltdownRest = new(ActionId.SinboundMeltdownRest)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction ShellCrusher = new(ActionId.ShellCrusher)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(split: Stack(8))],
        Timing = new() { DamageDelay = 0.86f },
    };

    public static readonly EnemyAction Apocalypse = new(ActionId.Apocalypse)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction SpiritTaker = new(ActionId.SpiritTaker)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = [Damage(), OnOthers(Knockback(KnockbackId.SpiritTaker))],
        Timing = new() { DamageDelay = 0.88f },
    };

    public static readonly EnemyAction DarkestDance = new(ActionId.DarkestDance)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = [Damage(TankBuster), ApplyStatus(StatusId.PhysicalVulnerabilityUp, 3.98f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 1.15f },
    };

    public static readonly EnemyAction DarkestDanceKnockback = new(ActionId.DarkestDanceKnockback)
    {
        Cast = new() { AnimationLock = 3.3f },
        Effects = [Damage(), Knockback(KnockbackId.DarkestDance)],
        Timing = new() { DamageDelay = 0.82f },
    };

    public static readonly EnemyAction ShockwavePulsar = new(ActionId.ShockwavePulsar)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.09f },
    };

    // -- P4 --

    public static readonly EnemyAction AkhRhai = new(ActionId.AkhRhai)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.18f },
    };

    public static readonly EnemyAction DarklitDragonsong = new(ActionId.DarklitDragonsong)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.21f },
    };

    public static readonly EnemyAction PathOfLight = new(ActionId.PathOfLight)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f), ApplyStatus(StatusId.Lightsteeped, 12f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.67f },
    };

    public static readonly EnemyAction BrightHungerDarklit = BrightHunger with
    {
        Effects = [Damage(split: Stack(2)), AddLightsteeped],
    };

    private static EnemyAction HallowedWings(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 1.77f },
    };

    public static readonly EnemyAction HallowedWingsLeft = HallowedWings(ActionId.HallowedWingsLeft);
    public static readonly EnemyAction HallowedWingsRight = HallowedWings(ActionId.HallowedWingsRight);

    public static readonly EnemyAction SomberDanceFar = new(ActionId.SomberDanceFar)
    {
        Cast = new() { AnimationLock = 3.3f },
        Effects = [Damage(TankBuster), ApplyStatus(StatusId.PhysicalVulnerabilityUp, 3.96f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 1.16f },
    };

    public static readonly EnemyAction SomberDanceNear = new(ActionId.SomberDanceNear)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = [Damage(VulnerableTo(StatusId.PhysicalVulnerabilityUp, requiredMitigation: 1f), TankBuster)],
        Timing = new() { DamageDelay = 1.16f },
    };

    public static readonly EnemyAction AkhMornUsurperHit = new(ActionId.AkhMornUsurperHit)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [
            Damage(VulnerableTo(StatusId.DarknessDesign), split: Stack(4, understacked: TankBuster)),
            ApplyStatus(StatusId.LightsDesign, 3.96f, delayToEffectResult: true)
        ],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction AkhMornOracleHit = new(ActionId.AkhMornOracleHit)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [
            Damage(VulnerableTo(StatusId.LightsDesign), split: Stack(4, understacked: TankBuster)),
            ApplyStatus(StatusId.DarknessDesign, 3.96f, delayToEffectResult: true)
        ],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction MornAfah = new(ActionId.MornAfah)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(split: Stack(8))],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction CrystallizeTimeUsurper = new(ActionId.CrystallizeTimeUsurper)
    {
        Cast = new() { AnimationLock = 3.1f },
    };

    public static readonly EnemyAction CrystallizeTimeOracle = new(ActionId.CrystallizeTimeOracle)
    {
        Cast = new() { AnimationLock = 4.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.15f },
    };

    public static readonly EnemyAction EdgeOfOblivion = new(ActionId.EdgeOfOblivion)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.94f },
    };

    // The Fragment keeps retail's battalion 0, and a friendly caster's ActionEffect plays no
    // animation; a hostile helper on its spot casts this beside the Fragment's own cast to show it.
    public static readonly EnemyAction EdgeOfOblivionAnimation = EdgeOfOblivion with { Effects = [] };

    public static readonly EnemyAction Maelstrom = new(ActionId.Maelstrom)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.93f },
    };

    // Only a Wyrmclaw holder survives the blast, and it takes the Wyrmclaw off.
    public static readonly EnemyAction LongingOfTheLost = new(ActionId.LongingOfTheLost)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(ProtectedBy(StatusId.Wyrmclaw), Lethal), RemoveStatus(StatusId.Wyrmclaw, atImpact: true)],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction JoylessDragonsong = new(ActionId.JoylessDragonsong)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.8f },
        DeathExplanation = "a dragon head kept a catch",
    };

    // Hits its holder too; only the others are pushed, away from the holder.
    public static readonly EnemyAction DarkAero = new(ActionId.DarkAero)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.95f), OnOthers(Knockback(KnockbackId.DarkAero, 0.81f))],
        Timing = new() { DamageDelay = 0.61f },
    };

    public static readonly EnemyAction TidalLightFirst = new(ActionId.TidalLightFirst)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.93f },
    };

    public static readonly EnemyAction TidalLightRest = new(ActionId.TidalLightRest)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.93f },
    };

    public static readonly EnemyAction Quietus = new(ActionId.Quietus)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.62f },
    };

    // The sheet's bar is too short to take the ActionEffect offset off; retail shows it whole.
    public static readonly EnemyAction HallowedWingsSwap = new(ActionId.HallowedWingsSwap)
    {
        Cast = new() { AnimationLock = 1.4f, CastSecondsOverride = 0.2f },
    };

    // Hits and pushes everyone in its 40y x 50y rectangle, in order of distance along the Usurper's
    // facing. The front four take the wing, the first as a tank buster, die to it if they still
    // carry the first wing's Magic Vuln Up, and gain it; the rest take a lighter hit. Retail tanks
    // have survived the vulnerable hit under heavy cooldowns, which the sim doesn't model.
    public static readonly EnemyAction HallowedWingsKnockback = new(ActionId.HallowedWingsKnockback)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects =
        [
            OnFront(4, 
                    Damage(VulnToMagic, split: WildCharge(front: 1, frontHit: TankBuster)), 
                    ApplyStatus(StatusId.MagicVulnerabilityUp, 6f, delayToEffectResult: true)
            ),
            OnBehindFront(4, Damage()),
            Knockback(KnockbackId.HallowedWings, 1.36f)
        ],
        Timing = new() { DamageDelay = 1.8f },
    };

    // -- P5 --

    // Pandora's own swing (PandoraAttack) hits no one; a helper lands this on each tank.
    public static readonly EnemyAction PandoraAutoAttack = new(ActionId.PandoraAutoAttack)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction FulgentBlade = new(ActionId.FulgentBlade)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.12f },
    };

    public static readonly EnemyAction PathOfLightFirst = new(ActionId.PathOfLightFirst)
    {
        Cast = new() { OmenDelay = 5f, AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction PathOfDarknessFirst = PathOfLightFirst with { ActionId = ActionId.PathOfDarknessFirst };

    public static readonly EnemyAction PathOfLightRest = new(ActionId.PathOfLightRest)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction PathOfDarknessRest = PathOfLightRest with
    {
        ActionId = ActionId.PathOfDarknessRest,
        Timing = new() { DamageDelay = 0.62f },
    };

    // Both stacks land on one tick: whoever stands in both dies to the second.
    public static readonly EnemyAction AkhMornPandoraLeft = new(ActionId.AkhMornPandoraLeft)
    {
        Cast = new() { AnimationLock = 2.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, 0.95f)],
        Timing = new() { DamageDelay = 0.34f },
    };

    public static readonly EnemyAction AkhMornPandoraRight = AkhMornPandoraLeft with
    {
        ActionId = ActionId.AkhMornPandoraRight,
        Timing = new() { DamageDelay = 0.36f },
    };

    // 120° either side of a line 60° off the boss's facing; the boss keeps facing the tank, as
    // the cleave resource carries the turn itself.
    public static readonly EnemyAction WingsCleaveLight = new(ActionId.WingsCleaveLight)
    {
        Cast = new() { AnimationLock = 3.6f, Rotation = MathF.PI / 3f },
        Area = new() { Size = 2f * MathF.PI / 3f },
        Effects = [Damage(VulnerableTo(StatusId.PhysicalVulnerabilityUp), TankBuster), ApplyStatus(StatusId.PhysicalVulnerabilityUp, 3.96f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.82f },
    };

    public static readonly EnemyAction WingsCleaveDark = WingsCleaveLight with
    {
        ActionId = ActionId.WingsCleaveDark,
        Cast = new() { AnimationLock = 3.6f, Rotation = -MathF.PI / 3f },
        Timing = new() { DamageDelay = 0.8f },
    };

    public static readonly EnemyAction WingsBusterLight = new(ActionId.WingsBusterLight)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, TankBuster), ApplyStatus(StatusId.MagicVulnerabilityUp, 3.96f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction WingsBusterDark = WingsBusterLight with
    {
        ActionId = ActionId.WingsBusterDark,
        Timing = new() { DamageDelay = 0.46f },
    };

    public static readonly EnemyAction ParadiseTowerExplosion = new(ActionId.ParadiseTowerExplosion)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 6.96f, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction ParadiseTowerFailure = new(ActionId.ParadiseTowerFailure)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "tower soaked by fewer than two",
        Timing = new() { DamageDelay = 0.67f },
    };

    // A line stack; only its front bait takes the resistance down. A second hit of the same colour
    // kills, and so does both lines of one round.
    private static EnemyAction CruelPath(uint actionId, ushort resistanceDown) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects =
        [
            Damage(VulnToMagic.VulnerableTo(resistanceDown), split: Stack(4)),
            ApplyStatus(StatusId.MagicVulnerabilityUp, 0.96f),
            OnNearest(ApplyStatus(resistanceDown, 15.95f, delayToEffectResult: true)),
        ],
        Timing = new() { DamageDelay = 0.95f },
    };

    public static readonly EnemyAction CruelPathOfLight = CruelPath(ActionId.CruelPathOfLight, StatusId.LightResistanceDown);
    public static readonly EnemyAction CruelPathOfDarkness = CruelPath(ActionId.CruelPathOfDarkness, StatusId.DarkResistanceDown);

    public static readonly EnemyAction CruelPathOfLightEcho = new(ActionId.CruelPathOfLightEcho)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.54f },
    };

    public static readonly EnemyAction CruelPathOfDarknessEcho = CruelPathOfLightEcho with { ActionId = ActionId.CruelPathOfDarknessEcho };
}
