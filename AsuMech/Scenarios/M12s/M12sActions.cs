using System;
using System.Linq;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Native.Interfaces;
using static AsuMech.Core.EnemyActions.EnemyActionEffects;
using static AsuMech.Core.EnemyActions.Severity;
using static AsuMech.Scenarios.M12s.M12sConstants;

namespace AsuMech.Scenarios.M12s;

public static class M12sActions
{
    private static readonly VulnSpec VulnToMagic = VulnerableTo(StatusId.MagicVulnerabilityUp);
    private static readonly VulnSpec VulnToPoison = VulnerableTo(StatusId.PoisonResistanceDownII);

    public static readonly EnemyAction TheFixer = new(ActionId.TheFixer)
    {
        Cast = new() { AnimationLock = 2.1f },
        Effects = [Damage()],
    };

    // UNVERIFIED: how hard a puddle hits; nobody stood in one.
    public static readonly EnemyAction PhagocyteSpotlight = new(ActionId.PhagocyteSpotlight)
    {
        Cast = new() { FaceTarget = false },
        Effects = [Damage(VulnToMagic)],
        DeathExplanation = "stay out of the blob's puddle",
    };

    public static readonly EnemyAction CruelCoilBind = new(ActionId.CruelCoilBind)
    {
        Effects = [ApplyStatus(StatusId.Bind, 5f)],
        Timing = new() { DamageDelay = 0f },
    };

    public const float CoilInner = 9f;
    public const float CoilGapHalfAngle = MathF.PI / 8f;

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

    public static bool InCoilGap(float facing, float dx, float dz)
    {
        var bearing = MathF.Atan2(dx, dz);
        var off = MathF.IEEERemainder(bearing - facing, 2f * MathF.PI);
        return MathF.Abs(off) <= CoilGapHalfAngle;
    }

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

    public static readonly EnemyAction SplattershedRaidwide = new(ActionId.SplattershedRaidwide)
    {
        Effects = [Damage()],
        Timing = new() { DamageDelay = 0.4f },
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
}
