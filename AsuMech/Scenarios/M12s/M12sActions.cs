using System.Linq;
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
    public const float DoubleSobatHalfAngle = MathF.PI / 2f;
    public const float WindPush = 25f;
    public const float HotBloodedSeconds = 5f;
    public const float PortentSeconds = 10f;
    public const float DoomSeconds = 8f;
    public const float ArcanumLightDownSeconds = 23.96f;
    public const float ThunderHalfAngle = MathF.PI / 12f;
    public const float PowerGusherHalfAngle = MathF.PI / 4f;
    public const float AeroSafeRadius = 5f;
    public const float BowtieHalfAngle = MathF.PI / 3f;
    public const float ProjectionReplayHalfAngle = 22.5f * MathF.PI / 180f;
    public const float ProjectionHalfAngle = 25f * MathF.PI / 180f;
    public const float ScaldingHalfAngle = 5f * MathF.PI / 180f;
    public const float HalfRoom = MathF.PI / 2f;
    private static IEnemyActionEffect MagicVuln(float seconds) => ApplyStatus(StatusId.MagicVulnerabilityUp, seconds);
    public const float CoilGapHalfAngle = MathF.PI / 8f;
    public const float CoilInner = 9f;
    private static readonly VulnSpec VulnToDark = VulnerableTo(StatusId.DarkResistanceDownII);
    private static readonly VulnSpec VulnToFire = VulnerableTo(StatusId.FireResistanceDownII);
    private const float DarkResistanceDownSeconds = 24f;
    private const float FireResistanceDownSeconds = 25f;
    private const float CleansedPoisonSeconds = 1.95f;
    private const float ChainBreakVulnSeconds = 8.96f;
    private static readonly VulnSpec VulnToPoison = VulnerableTo(StatusId.PoisonResistanceDownII);
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private const float PoisonSeconds = 12f;
    private const float MagicVulnSeconds = 1.96f;
    private const float DamageDownSeconds = 30f;

    public static readonly EnemyAction TheFixer = new(ActionId.TheFixer)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    public static readonly EnemyAction MortalSlayerGreen = new(ActionId.MortalSlayerGreen)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPoison), ApplyStatus(StatusId.PoisonResistanceDownII, PoisonSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "one orb per player: the closest to the boss on the orb's side takes it, 6y from the others",
    };

    public static readonly EnemyAction MortalSlayerPurple = new(ActionId.MortalSlayerPurple)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPoison, TankBuster), ApplyStatus(StatusId.PoisonResistanceDownII, PoisonSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "purple orbs are tank busters: a tank steps up to take each",
    };

    // UNVERIFIED: how hard an orb that finds nobody on its side hits the party.
    public static readonly EnemyAction ArmySlayerGreen = new(ActionId.ArmySlayerGreen)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(VulnToPoison)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "an orb found nobody on its side",
    };

    public static readonly EnemyAction ArmySlayerPurple = ArmySlayerGreen with { ActionId = ActionId.ArmySlayerPurple };

    public const float RavenousReachHalfAngle = MathF.PI / 3f;

    public static readonly EnemyAction RavenousReachCone = new(ActionId.RavenousReachCone)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = RavenousReachHalfAngle },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stand behind the dragon head",
    };

    public const float SpotlightRadius = 5f;

    public static readonly EnemyAction PhagocyteSpotlight = new(ActionId.PhagocyteSpotlight)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, DamageDownSeconds, delayToEffectResult: true)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "keep moving: the puddles drop where you stand",
    };

    public static readonly EnemyAction DramaticLysis = new(ActionId.DramaticLysis)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "Bursting Grotesquerie spreads 6y from everyone",
    };

    // UNVERIFIED: the minimum; the role that doesn't spread takes it together.
    public static readonly EnemyAction FourthWallFusion = new(ActionId.FourthWallFusion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the role that doesn't spread stacks on Shared Grotesquerie",
    };

    public const float HemorrhagicHalfAngle = MathF.PI / 12f;

    // UNVERIFIED: lethal; a single hit doesn't always kill in retail.
    public static readonly EnemyAction HemorrhagicProjection = new(ActionId.HemorrhagicProjection)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Area = new() { Size = HemorrhagicHalfAngle, ExcludeCaster = true },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "turn so your arrow's cleave points at nobody",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction Burst = new(ActionId.Burst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stay 12y from every blood vessel",
    };

    public static readonly EnemyAction VisceralBurst = new(ActionId.VisceralBurst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(TankBuster)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the tank busters go to the tanks' spot, away from the party",
    };

    // Everyone but the tanks takes it.
    public static readonly EnemyAction Act1FourthWallFusion = new(ActionId.Act1FourthWallFusion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(6)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the party stacks on the healer while the tanks take the busters apart",
    };

    // Curtain Call's cleave cures Rotting Flesh; anyone else it catches leaves with Damage Down.
    public static readonly EnemyAction CurtainCallRavenousReach = new(ActionId.RavenousReachCone)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = RavenousReachHalfAngle },
        Effects =
        [
            Damage(),
            OnLackingStatus(StatusId.RottingFlesh, ApplyStatus(StatusId.DamageDown, DamageDownSeconds, delayToEffectResult: true)),
            OnHavingStatus(StatusId.RottingFlesh,
                RemoveStatus(StatusId.RottingFlesh, atImpact: true),
                ApplyStatus(StatusId.PoisonResistanceDownII, CleansedPoisonSeconds, delayToEffectResult: true)),
        ],
        Timing = new() { DamageDelay = 0.15f },
        DeathExplanation = "Rotting Flesh stands in the dragon's cleave, Bursting Grotesquerie stays out",
    };

    // UNVERIFIED: what two overlapping spreads do; no vuln was seen on the cured.
    public static readonly EnemyAction CellShedding = new(ActionId.CellShedding)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the cured spread 6y apart",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction CellDeath = new(ActionId.CellDeath)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "Rotting Flesh ran out: stand in the dragon's cleave to cure it",
    };

    public static readonly EnemyAction ChainBreak = new(ActionId.ChainBreak)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, ChainBreakVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "break your chain 4y from everyone else's",
    };

    public static readonly EnemyAction SplattershedRaidwide = new(ActionId.SplattershedRaidwide)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction SlaughtershedRaidwide = SplattershedRaidwide with { ActionId = ActionId.SlaughtershedRaidwide };

    public static readonly EnemyAction CurtainCallDramaticLysis = DramaticLysis with
    {
        ActionId = ActionId.CurtainCallDramaticLysis,
        DeathExplanation = "the spread role takes the corner without a blood vessel, 6y apart",
    };

    public static readonly EnemyAction CurtainCallFourthWallFusion = new(ActionId.CurtainCallFourthWallFusion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(4)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the stack role stacks in the corner with the blood vessel",
    };

    // UNVERIFIED: lethal; nobody stood in one.
    public static readonly EnemyAction SerpentineScourge = new(ActionId.SerpentineScourgeRect)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the dragon heads cleave half the room each, one side then the other",
    };

    // UNVERIFIED: the push distance and speed.
    public const float RaptorKnucklesPush = 30f;

    public static readonly EnemyAction RaptorKnuckles = new(ActionId.RaptorKnucklesKnockback)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Push(RaptorKnucklesPush, 30f)],
        Timing = new() { DamageDelay = 0.2f },
    };

    public static readonly EnemyAction RefreshingOverkillRaidwide = new(ActionId.RefreshingOverkillRaidwide)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

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

    public static readonly EnemyAction CruelCoilBind = new(ActionId.CruelCoilBind)
    {
        Effects = [ApplyStatus(StatusId.Bind, 5f)],
        Timing = new() { DamageDelay = 0f },
    };

    // The coil's body: everywhere in the ring but the gap the boss faces. UNVERIFIED: the gap's side and width.
    public static readonly EnemyAction SkinsplitterDonut = new(ActionId.SkinsplitterDonut)
    {
        Area = new()
        {
            Size = CoilInner,
            AdjustTargets = (ctx, hits) => hits.Where(h => !InCoilGap(ctx.Origin.Rotation, h.Position.X - ctx.Origin.Position.X, h.Position.Z - ctx.Origin.Position.Z)).ToList(),
        },
        Effects = [Damage(VulnToMagic)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "leave the coil only through the gap the boss faces",
    };

    public static readonly EnemyAction DramaticLysisChainBreak = new(ActionId.DramaticLysisChainBreak)
    {
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 8.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a chain's end bursts where it snaps: keep clear of it",
    };

    // UNVERIFIED: retail keeps hurting an unbroken chain's ends each second; here it kills at once.
    public static readonly EnemyAction DramaticLysisChainFail = new(ActionId.DramaticLysisChainFail)
    {
        Area = new() { CastTypeOverride = CastType.SingleTarget },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stretch your chain until it snaps",
    };

    public static readonly EnemyAction RoilingMassBlob = new(ActionId.RoilingMassBlob)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "soak the tower without a vulnerability",
    };

    public static readonly EnemyAction RoilingMassChain = RoilingMassBlob with { ActionId = ActionId.RoilingMassChain };

    public static readonly EnemyAction UnmitigatedExplosion = new(ActionId.UnmitigatedExplosion)
    {
        Effects = [Damage(VulnToMagic)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a tower went unsoaked",
    };

    // UNVERIFIED: lethal; nobody stayed inside.
    public static readonly EnemyAction ConstrictorKill = new(ActionId.ConstrictorKill)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "get out of the coil before it constricts",
    };

    public static readonly EnemyAction GrandEntrance = new(ActionId.GrandEntranceDisappear)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(), ApplyStatus(StatusId.DamageDown, 30f)],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction BringDownTheHouseLarge = new(ActionId.BringDownTheHouseLarge)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(Lethal)],
        // Lands before the platforms take over the arena's edge.
        Timing = new() { DamageDelay = 0f },
        DeathExplanation = "the floor falls: stand on a platform that stays",
    };

    public static readonly EnemyAction BringDownTheHouseMedium = BringDownTheHouseLarge with { ActionId = ActionId.BringDownTheHouseMedium };

    public static readonly EnemyAction BringDownTheHouseSmall = BringDownTheHouseLarge with { ActionId = ActionId.BringDownTheHouseSmall };

    // UNVERIFIED: that a second burst on top of the first kills.
    public static readonly EnemyAction MitoticDramaticLysis = new(ActionId.MitoticDramaticLysis)
    {
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(VulnToPoison), ApplyStatus(StatusId.PoisonResistanceDownII, 3.96f)],
        Timing = new() { DamageDelay = 0.2f },
        DeathExplanation = "one player per platform corner: the bursts must not overlap",
    };

    public static readonly EnemyAction MetamitosisTower = new(ActionId.MetamitosisTower)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    public static readonly EnemyAction SplitScourge = new(ActionId.SplitScourge)
    {
        Effects = [Damage(VulnToMagic, TankBuster), ApplyStatus(StatusId.MagicVulnerabilityUp, 4.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a tank stands closest to each snake head",
    };

    public static readonly EnemyAction VenomousScourge = new(ActionId.VenomousScourge)
    {
        Cast = new() { FaceTarget = false },
        Area = new() { CastTypeOverride = CastType.Circle },
        Effects = [Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, 1.96f)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the three furthest east and west spread; the tanks stay in the middle",
    };

    public static bool InCoilGap(float facing, float dx, float dz)
    {
        var bearing = MathF.Atan2(dx, dz);
        var off = MathF.IEEERemainder(bearing - facing, 2f * MathF.PI);
        return MathF.Abs(off) <= CoilGapHalfAngle;
    }

    public static readonly EnemyAction FirefallSplashJump = new(ActionId.FirefallSplashJump)
    {
        Cast = new() { AnimationLock = 0.8f },
        Effects = [Damage(VulnToMagic), MagicVuln(4.96f)],
        Timing = new() { DamageDelay = 0.2f },
        DeathExplanation = "only the boss tether stands where Lindwurm lands",
    };

    // The jump's target is spared: the boss lands on them.
    public static readonly EnemyAction ScaldingWaves = new(ActionId.ScaldingWaves)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ScaldingHalfAngle, ExcludeCaster = true },
        Effects = [Damage(VulnToMagic), MagicVuln(4.96f)],
        Timing = new() { DamageDelay = 0.2f },
        DeathExplanation = "one thin cleave each, nobody behind another",
    };

    // UNVERIFIED: lethal to anyone else inside.
    public static readonly EnemyAction ManaBurst = new(ActionId.ManaBurst)
    {
        Cast = new() { AnimationLock = 3.1f },
        Effects = [OnTarget(Damage(VulnToMagic), MagicVuln(1.96f)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "take a defamation far from everyone",
    };

    public static readonly EnemyAction HeavySlam = new(ActionId.HeavySlam)
    {
        Cast = new() { AnimationLock = 4.1f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(3)), MagicVuln(4.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the light party shares the stack",
    };

    public static readonly EnemyAction TimelessSpite = new(ActionId.TimelessSpite)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(VulnToMagic, split: Stack(2)), MagicVuln(5.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the defamations bait it with a partner each",
    };

    // UNVERIFIED: the replays' own areas are lethal.
    public static readonly EnemyAction FirefallSplashReplay = new(ActionId.FirefallSplashReplay)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "stay off the north clone when it lands",
    };

    public static readonly EnemyAction ScaldingWavesReplay = new(ActionId.ScaldingWavesReplay)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ScaldingHalfAngle },
        Effects = [Damage(VulnToMagic), MagicVuln(5.96f)],
        Timing = new() { DamageDelay = 0.2f },
        DeathExplanation = "the replay cleaves the same four: same lineup",
    };

    public static readonly EnemyAction ManaBurstReplay = new(ActionId.ManaBurstReplay)
    {
        Cast = new() { AnimationLock = 0.6f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a defamation clone goes off: keep 20y from it",
    };

    public static readonly EnemyAction HemorrhagicProjectionReplay = new(ActionId.HemorrhagicProjectionReplay)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ProjectionReplayHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a cone clone fires through the middle",
    };

    public static readonly EnemyAction HeavySlamReplay = new(ActionId.HeavySlamReplay)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(VulnToMagic), MagicVuln(5.96f)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "soak the stack clones",
    };

    // UNVERIFIED: lethal to all.
    public static readonly EnemyAction UnmitigatedImpact = new(ActionId.UnmitigatedImpact)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "a stack went off with nobody in it",
    };

    public static readonly EnemyAction BloodyBurst = new(ActionId.BloodyBurst)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: the black holes' releases are lethal.
    public static readonly EnemyAction LindwurmsWater = new(ActionId.LindwurmsWaterIII)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the hole that sucked in the blue orb drops a circle",
    };

    public static readonly EnemyAction LindwurmsAero = new(ActionId.LindwurmsAeroIII)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = AeroSafeRadius },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the green orb's donut covers the arena: get under its hole",
    };

    public static readonly EnemyAction StraightforwardThunder = new(ActionId.StraightforwardThunderII)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = BowtieHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the purple orb cleaves north and south of its hole",
    };

    public static readonly EnemyAction SidewaysFire = new(ActionId.SidewaysFireII)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = BowtieHalfAngle },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the orange orb cleaves east and west of its hole",
    };

    public static readonly EnemyAction WailingWave = new(ActionId.WailingWave)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.3f },
    };

    // UNVERIFIED: retail deals heavy damage (guides say a Damage Down); it kills here so the miss shows.
    public static readonly EnemyAction UnmitigatedExplosionAlpha = new(ActionId.MutationUnmitigatedExplosion)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "Mutation α took damage",
    };

    public static readonly EnemyAction UnmitigatedExplosionBeta = UnmitigatedExplosionAlpha with
    {
        DeathExplanation = "Mutation β took no damage",
    };

    // Fired from the cone tether's holder the way they face. UNVERIFIED: lethal.
    public static readonly EnemyAction CloneHemorrhagicProjection = new(ActionId.CloneHemorrhagicProjection)
    {
        Cast = new() { AnimationLock = 1.1f, FaceTarget = false },
        Area = new() { Size = ProjectionHalfAngle, ExcludeCaster = true },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.3f },
        DeathExplanation = "the cone holder faces away from the party",
    };

    // UNVERIFIED: how hard a puddle hits; nobody stood in one.
    public static readonly EnemyAction Act2PhagocyteSpotlight = new(ActionId.Act2PhagocyteSpotlight)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic)],
        DeathExplanation = "stay out of the blob's puddle",
    };

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

    public static readonly EnemyAction DoubleSobatFirst = new(ActionId.DoubleSobatFirst)
    {
        Cast = new() { AnimationLock = 0.6f },
        Area = new() { Size = DoubleSobatHalfAngle, CastTypeOverride = Core.Native.Interfaces.CastType.Cone },
        Effects = [OnTarget(Damage(TankBuster)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "only the main tank in front of the boss",
    };

    public static readonly EnemyAction DoubleSobatSecond = new(ActionId.DoubleSobatSecond)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false, CastSecondsOverride = 4.3f },
        Area = new() { Size = HalfRoom },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "the second kick follows the first: step out of it",
    };

    public static readonly EnemyAction ArcadianHell = IdyllicDream with { ActionId = ActionId.ArcadianHell };

    public static readonly EnemyAction ArcadianHellFour = new(ActionId.ArcadianHellFour)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
    };

    public static readonly EnemyAction ArcadianHellEight = ArcadianHellFour with { ActionId = ActionId.ArcadianHellEight };

    // UNVERIFIED: lethal to the others it catches.
    public static readonly EnemyAction IdyllicManaBurst = new(ActionId.IdyllicManaBurst)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [OnTarget(Damage(VulnToMagic), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)), OnOthers(Damage(Lethal))],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "take a defamation far from everyone",
    };

    // UNVERIFIED: the minimum (three survive it).
    public static readonly EnemyAction IdyllicHeavySlam = new(ActionId.IdyllicHeavySlam)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(VulnToMagic, split: Stack(3)), ApplyStatus(StatusId.MagicVulnerabilityUp, MagicVulnSeconds)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "stack with your light party",
    };

    // UNVERIFIED: lethal.
    public static readonly EnemyAction IdyllicUnmitigatedExplosion = new(ActionId.IdyllicUnmitigatedExplosion)
    {
        Cast = new() { AnimationLock = 0.6f, FaceTarget = false },
        Effects = [Damage(Lethal)],
        Timing = new() { DamageDelay = 0.4f },
        DeathExplanation = "a tower went unsoaked",
    };
}
