using System;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru;

public static class FruActions
{
    // A spread: the marked player takes it, anyone else caught dies.
    private static IEnemyActionEffect[] Spread(Severity? onTarget = null)
        => [OnTarget(Damage(onTarget)), OnOthers(Damage(Lethal))];

    private static readonly IEnemyActionEffect AddLightsteeped = ApplyStatusOrOverload(StatusId.Lightsteeped, maxStacks: 4, duration: 30f);

    // -- P2 Diamond Dust --

    public static readonly EnemyAction MirrorImage = new(ActionId.MirrorImage) { Cast = new() { AnimationLock = 2.1f } };

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
        // UNVERIFIED: no recorded hit; Axe Kick's.
        Timing = new() { DamageDelay = 1.09f },
    };

    public static readonly EnemyAction HouseOfLightDiamondDust = new(ActionId.HouseOfLightDiamondDust)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { Size = MathF.PI / 12f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction FrigidStone = new(ActionId.FrigidStone)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.26f },
    };

    public static readonly EnemyAction IcicleImpact = new(ActionId.IcicleImpact)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction FrigidNeedleCircle = new(ActionId.FrigidNeedleCircle)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction FrigidNeedleCross = new(ActionId.FrigidNeedleCross)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction FrigidNeedleCrossDiagonal = FrigidNeedleCross with
    {
        Cast = new() { AnimationLock = 1.1f, Rotation = MathF.PI / 4f },
    };

    public static readonly EnemyAction SinboundHolyCast = new(ActionId.SinboundHolyCast) { Cast = new() { AnimationLock = 3.1f } };

    public static readonly EnemyAction HeavenlyStrike = new(ActionId.HeavenlyStrike)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(), Knockback(KnockbackId.HeavenlyStrike)],
        Timing = new() { DamageDelay = 0.9f },
    };

    public static readonly EnemyAction SinboundHoly = new(ActionId.SinboundHoly)
    {
        Cast = new() { AnimationLock = 1.6f },
        Effects = [Damage(split: Stack(4))],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction ShiningArmor = new(ActionId.ShiningArmor)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Gaze(lookAway: true)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction FrostArmor = new(ActionId.FrostArmor) { Cast = new() { AnimationLock = 2.1f } };

    // The range-30 halves hit everything but the 90° behind the Mirror; the range-40 halves hit
    // only that 90°.
    private static EnemyAction TwinFront(uint actionId, float animationLock) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock },
        Area = new() { Size = 3f * MathF.PI / 4f },
        Effects = [Damage(Lethal)],
    };

    private static EnemyAction TwinBack(uint actionId, float animationLock) => new(actionId)
    {
        Cast = new() { AnimationLock = animationLock, Rotation = MathF.PI },
        Area = new() { Size = MathF.PI / 4f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction TwinStillnessFirst = TwinFront(ActionId.TwinStillnessFirst, 2.1f);
    public static readonly EnemyAction TwinStillnessSecond = TwinBack(ActionId.TwinStillnessSecond, 3.1f);
    public static readonly EnemyAction TwinSilenceFirst = TwinBack(ActionId.TwinSilenceFirst, 2.1f);
    public static readonly EnemyAction TwinSilenceSecond = TwinFront(ActionId.TwinSilenceSecond, 3.1f);

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
        Cast = new() { AnimationLock = 1.6f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction PowerfulLight = new(ActionId.PowerfulLight)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(split: Stack(4)), AddLightsteeped],
        Timing = new() { DamageDelay = 0.65f },
    };

    public static readonly EnemyAction Burst = new(ActionId.Burst)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction BanishPairsCast = new(ActionId.BanishPairsCast) { Cast = new() { AnimationLock = 3.1f } };
    public static readonly EnemyAction BanishSpreadCast = new(ActionId.BanishSpreadCast) { Cast = new() { AnimationLock = 3.1f } };

    public static readonly EnemyAction BanishPairs = new(ActionId.BanishPairs)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(split: Stack(2))],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction BanishSpread = new(ActionId.BanishSpread)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction HouseOfLightCast = new(ActionId.HouseOfLightCast) { Cast = new() { AnimationLock = 3.1f } };

    public static readonly EnemyAction HouseOfLight = new(ActionId.HouseOfLight)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [.. Spread(), AddLightsteeped],
        Timing = new() { DamageDelay = 0.64f },
    };

    // -- P3 --

    public static readonly EnemyAction UltimateRelativity = new(ActionId.UltimateRelativity)
    {
        Cast = new() { AnimationLock = 4.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.2f },
    };

    public static readonly EnemyAction Speed = new(ActionId.Speed) { Cast = new() { AnimationLock = 3.1f } };
    public static readonly EnemyAction Quicken = new(ActionId.Quicken) { Cast = new() { AnimationLock = 1.1f } };
    public static readonly EnemyAction Slow = new(ActionId.Slow) { Cast = new() { AnimationLock = 1.1f } };

    public static readonly EnemyAction DarkFire = new(ActionId.DarkFire)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction DarkBlizzard = new(ActionId.DarkBlizzard)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = 3f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction UnholyDarkness = new(ActionId.UnholyDarkness)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(split: Stack(5))],
        Timing = new() { DamageDelay = 0.76f },
    };

    public static readonly EnemyAction DarkWater = new(ActionId.DarkWater)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(split: Stack(4))],
        Timing = new() { DamageDelay = 0.51f },
    };

    public static readonly EnemyAction DarkEruption = new(ActionId.DarkEruption)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.65f },
    };

    public static readonly EnemyAction Shadoweye = new(ActionId.Shadoweye)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [OnOthers(Gaze(lookAway: true))],
    };

    public static readonly EnemyAction SinboundMeltdownCast = new(ActionId.SinboundMeltdownCast) { Cast = new() { AnimationLock = 0.2f } };

    public static readonly EnemyAction SinboundMeltdownFirst = new(ActionId.SinboundMeltdownFirst)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction SinboundMeltdownRest = new(ActionId.SinboundMeltdownRest)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction ShellCrusherCast = new(ActionId.ShellCrusherCast) { Cast = new() { AnimationLock = 0.2f } };

    public static readonly EnemyAction ShellCrusher = new(ActionId.ShellCrusher)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(split: Stack(8))],
        Timing = new() { DamageDelay = 0.86f },
    };

    public static readonly EnemyAction SpellInWaitingRefrain = new(ActionId.SpellInWaitingRefrain) { Cast = new() { AnimationLock = 3.1f } };
    public static readonly EnemyAction DarkWaterCast = new(ActionId.DarkWaterCast) { Cast = new() { AnimationLock = 3.1f } };
    public static readonly EnemyAction ApocalypseCast = new(ActionId.ApocalypseCast) { Cast = new() { AnimationLock = 2.1f } };

    public static readonly EnemyAction Apocalypse = new(ActionId.Apocalypse)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.57f },
    };

    public static readonly EnemyAction SpiritTakerCast = new(ActionId.SpiritTakerCast) { Cast = new() { AnimationLock = 0.2f } };

    public static readonly EnemyAction SpiritTaker = new(ActionId.SpiritTaker)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = Spread(),
        Timing = new() { DamageDelay = 0.88f },
    };

    public static readonly EnemyAction DarkEruptionCast = new(ActionId.DarkEruptionCast) { Cast = new() { AnimationLock = 3.1f } };
    public static readonly EnemyAction DarkestDanceCast = new(ActionId.DarkestDanceCast) { Cast = new() { AnimationLock = 0.2f } };

    public static readonly EnemyAction DarkestDance = new(ActionId.DarkestDance)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = Spread(TankBuster),
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

    public static readonly EnemyAction AkhRhaiCast = new(ActionId.AkhRhaiCast)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction AkhRhai = new(ActionId.AkhRhai)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction DarklitDragonsong = new(ActionId.DarklitDragonsong)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.21f },
    };

    public static readonly EnemyAction DarklitDragonsongOracle = new(ActionId.DarklitDragonsongOracle) { Cast = new() { AnimationLock = 3.1f } };
    public static readonly EnemyAction PathOfLightCast = new(ActionId.PathOfLightCast) { Cast = new() { AnimationLock = 2.1f } };

    public static readonly EnemyAction PathOfLight = new(ActionId.PathOfLight)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [.. Spread(), AddLightsteeped],
        Timing = new() { DamageDelay = 0.67f },
    };

    public static readonly EnemyAction BrightHungerDarklit = BrightHunger with
    {
        Effects = [Damage(split: Stack(2)), AddLightsteeped],
    };

    // The sheet's rect runs ahead of the Usurper; the wing covers the half of the arena on its
    // side of her, which she faces north across.
    private static EnemyAction HallowedWings(uint actionId, float sideRotation) => new(actionId)
    {
        Cast = new() { AnimationLock = 3.1f },
        Area = new()
        {
            AdjustTargets = (ctx, _) => ctx.Party.Find.InsideRect(
                new Placement(ctx.Origin.Position, ctx.Origin.Rotation + sideRotation), 40f, 40f).ToList(),
        },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction HallowedWingsLeft = HallowedWings(ActionId.HallowedWingsLeft, MathF.PI / 2f);
    public static readonly EnemyAction HallowedWingsRight = HallowedWings(ActionId.HallowedWingsRight, -MathF.PI / 2f);

    public static readonly EnemyAction SomberDanceCast = new(ActionId.SomberDanceCast) { Cast = new() { AnimationLock = 0.2f } };

    public static readonly EnemyAction SomberDanceFar = new(ActionId.SomberDanceFar)
    {
        Cast = new() { AnimationLock = 3.3f },
        Effects = Spread(TankBuster),
        Timing = new() { DamageDelay = 1.16f },
    };

    public static readonly EnemyAction SomberDanceNear = new(ActionId.SomberDanceNear)
    {
        Cast = new() { AnimationLock = 2.8f },
        Effects = Spread(TankBuster),
        Timing = new() { DamageDelay = 1.3f },
    };

    public static readonly EnemyAction AkhMornUsurper = new(ActionId.AkhMornUsurper) { Cast = new() { AnimationLock = 4.1f } };
    public static readonly EnemyAction AkhMornOracle = new(ActionId.AkhMornOracle) { Cast = new() { AnimationLock = 4.1f } };

    // The Usurper's goes on the main tank and the party shares it; the Oracle's on the off-tank,
    // who takes it alone.
    public static readonly EnemyAction AkhMornUsurperHit = new(ActionId.AkhMornUsurperHit)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(split: Stack(7))],
        Timing = new() { DamageDelay = 0.63f },
    };

    public static readonly EnemyAction AkhMornOracleHit = new(ActionId.AkhMornOracleHit)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = Spread(TankBuster),
        Timing = new() { DamageDelay = 0.63f },
    };

    // Crystallize Time's 7-1 the other way round: the main tank takes the Usurper's alone.
    public static readonly EnemyAction AkhMornUsurperSolo = AkhMornUsurperHit with { Effects = Spread(TankBuster) };
    public static readonly EnemyAction AkhMornOracleShared = AkhMornOracleHit with { Effects = [Damage(split: Stack(7))] };

    // Both bars are cut from retail's 10s to 4.5s, so the practice reaches the debuffs sooner.
    public static readonly EnemyAction CrystallizeTimeUsurper = new(ActionId.CrystallizeTimeUsurper)
    {
        Cast = new() { AnimationLock = 3.1f, CastSecondsOverride = 4.2f },
    };

    public static readonly EnemyAction CrystallizeTimeOracle = new(ActionId.CrystallizeTimeOracle)
    {
        Cast = new() { AnimationLock = 4.1f, CastSecondsOverride = 4.2f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 1.15f },
    };

    public static readonly EnemyAction Maelstrom = new(ActionId.Maelstrom)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
    };

    // Only a Wyrmclaw holder survives catching the head; it splashes the others.
    public static readonly EnemyAction LongingOfTheLost = new(ActionId.LongingOfTheLost)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [OnTarget(Damage(ProtectedBy(StatusId.Wyrmclaw), Lethal)), OnOthers(Damage()), OnTarget(RemoveStatus(StatusId.Wyrmclaw))],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction DrachenWandererDisappear = new(ActionId.DrachenWandererDisappear) { Cast = new() { AnimationLock = 4.1f } };

    public static readonly EnemyAction DarkAero = new(ActionId.DarkAero)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [OnOthers(Damage()), OnOthers(Knockback(KnockbackId.DarkAero))],
        Timing = new() { DamageDelay = 0.61f },
    };

    public static readonly EnemyAction TidalLightCast = new(ActionId.TidalLightCast) { Cast = new() { AnimationLock = 2.1f } };

    public static readonly EnemyAction TidalLightFirst = new(ActionId.TidalLightFirst)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction TidalLightRest = new(ActionId.TidalLightRest)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction Quietus = new(ActionId.Quietus)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction HallowedWingsCast = new(ActionId.HallowedWingsCast) { Cast = new() { AnimationLock = 1.4f } };

    // The tank leads; whoever the first swing already marked dies to the second.
    public static readonly EnemyAction HallowedWingsKnockback = new(ActionId.HallowedWingsKnockback)
    {
        Cast = new() { AnimationLock = 2.1f },
        Area = new() { AdjustTargets = (_, hits) => hits.Take(4).ToList() },
        Effects =
        [
            Damage(VulnerableTo(StatusId.MagicVulnerabilityUp), split: WildCharge(front: 1, frontHit: TankBuster)),
            ApplyStatus(StatusId.MagicVulnerabilityUp, 6f),
            Knockback(KnockbackId.HallowedWings),
        ],
        Timing = new() { DamageDelay = 1.8f },
    };

    // -- P5 --

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
        Timing = new() { DamageDelay = 0.71f },
    };

    public static readonly EnemyAction PathOfDarknessRest = PathOfLightRest with { ActionId = ActionId.PathOfDarknessRest };

    public static readonly EnemyAction AkhMornPandora = new(ActionId.AkhMornPandora) { Cast = new() { AnimationLock = 3.1f } };

    public static readonly EnemyAction AkhMornPandoraLeft = new(ActionId.AkhMornPandoraLeft)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(split: Stack(4))],
        Timing = new() { DamageDelay = 0.34f },
    };

    public static readonly EnemyAction AkhMornPandoraRight = AkhMornPandoraLeft with
    {
        ActionId = ActionId.AkhMornPandoraRight,
        Timing = new() { DamageDelay = 0.36f },
    };

    public static readonly EnemyAction ParadiseRegained = new(ActionId.ParadiseRegained) { Cast = new() { AnimationLock = 3.1f } };
    public static readonly EnemyAction WingsDarkThenLight = new(ActionId.WingsDarkThenLight) { Cast = new() { AnimationLock = 0.2f } };
    public static readonly EnemyAction WingsLightThenDark = new(ActionId.WingsLightThenDark) { Cast = new() { AnimationLock = 0.2f } };

    // 120° either side of a line 60° off the boss's facing; the boss keeps facing the tank, as
    // the cleave resource carries the turn itself.
    public static readonly EnemyAction WingsCleaveLight = new(ActionId.WingsCleaveLight)
    {
        Cast = new() { AnimationLock = 3.6f, Rotation = MathF.PI / 3f },
        Area = new() { Size = 2f * MathF.PI / 3f },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.82f },
    };

    public static readonly EnemyAction WingsCleaveDark = WingsCleaveLight with
    {
        ActionId = ActionId.WingsCleaveDark,
        Cast = new() { AnimationLock = 3.6f, Rotation = -MathF.PI / 3f },
        Timing = new() { DamageDelay = 0.74f },
    };

    public static readonly EnemyAction WingsBusterLight = new(ActionId.WingsBusterLight)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = Spread(TankBuster),
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
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.64f },
    };

    public static readonly EnemyAction ParadiseTowerFailure = new(ActionId.ParadiseTowerFailure)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
        DeathExplanation = "tower not soaked by exactly two",
    };

    public static readonly EnemyAction PolarizingStrikes = new(ActionId.PolarizingStrikes) { Cast = new() { AnimationLock = 2.1f } };
    public static readonly EnemyAction PolarizingPaths = new(ActionId.PolarizingPaths) { Cast = new() { AnimationLock = 2.1f } };

    // A line stack; only its front bait takes the resistance down, and a second hit of the same
    // colour kills.
    private static EnemyAction CruelPath(uint actionId, ushort resistanceDown) => new(actionId)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects =
        [
            Damage(VulnerableTo(resistanceDown), split: Stack(4)),
            OnTarget(ApplyStatus(resistanceDown, 30f)),
        ],
        Timing = new() { DamageDelay = 0.95f },
    };

    public static readonly EnemyAction CruelPathOfLight = CruelPath(ActionId.CruelPathOfLight, StatusId.LightResistanceDown);
    public static readonly EnemyAction CruelPathOfDarkness = CruelPath(ActionId.CruelPathOfDarkness, StatusId.DarkResistanceDown);

    public static readonly EnemyAction CruelPathOfLightEcho = new(ActionId.CruelPathOfLightEcho)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage(Lethal)],
    };

    public static readonly EnemyAction CruelPathOfDarknessEcho = CruelPathOfLightEcho with { ActionId = ActionId.CruelPathOfDarknessEcho };
}
