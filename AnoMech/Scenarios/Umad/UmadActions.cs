using System;
using AnoMech.Core.EnemyActions;
using static AnoMech.Core.EnemyActions.Distribution;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using static AnoMech.Core.EnemyActions.Severity;
using P1 = AnoMech.Scenarios.Umad.P1TeleTrouncing.UmadP1TeleTrouncingConstants;

namespace AnoMech.Scenarios.Umad;

public static class UmadActions
{
    private static readonly DamageSpec Magic = DamageType.Magic.VulnerableTo(UmadConstants.StatusId.MagicVulnerabilityUp);

    private static readonly IEnemyActionEffect MagicVulnerabilityUp =
        ApplyStatus(UmadConstants.StatusId.MagicVulnerabilityUp, P1.MagicVulnerabilityUpSeconds);

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
    private static readonly IEnemyActionEffect ForsakenMagicVulnerabilityUp =
        ApplyStatus(UmadConstants.StatusId.MagicVulnerabilityUp, 1.96f);

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
        Effects = [Damage(Magic, split: Stack(3)), ForsakenMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.62f },
    };

    public static readonly EnemyAction Spellscatter = new(UmadConstants.ActionId.Spellscatter)
    {
        Cast = new() { AnimationLock = 1.1f },
        Effects = [Damage(Magic), ForsakenMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.62f },
    };

    // Cast from its holder's spot; the holder stands at the apex and is never hit by it.
    public static readonly EnemyAction Spellwave = new(UmadConstants.ActionId.Spellwave)
    {
        Cast = new() { AnimationLock = 1.1f },
        Area = new() { Size = MathF.PI / 4, ExcludeCaster = true },
        Effects = [Damage(Magic), ForsakenMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.62f },
    };

    public sealed record EndActions(EnemyAction KefkaHit, EnemyAction CloneHit, EnemyAction AllThingsEnding);

    // Future's End cleaves the half facing the player as the cast starts, Past's End the other half.
    public static readonly EndActions FuturesEnd = new(
        EndHit(UmadConstants.ActionId.FutureSEnd_Resolve),
        EndHit(UmadConstants.ActionId.FutureSEnd_CloneResolve),
        AllThingsEnding(UmadConstants.ActionId.AllThingsEnding_Future, 0f));

    public static readonly EndActions PastsEnd = new(
        EndHit(UmadConstants.ActionId.PastSEnd_Resolve),
        EndHit(UmadConstants.ActionId.PastSEnd_CloneResolve),
        AllThingsEnding(UmadConstants.ActionId.AllThingsEnding_Past, MathF.PI));

    private static EnemyAction EndHit(uint actionId) => new(actionId)
    {
        Cast = new() { AnimationLock = 6f },
        Effects = [Damage(Magic), ForsakenMagicVulnerabilityUp],
        Timing = new() { DamageDelay = 0.75f },
    };

    // Snapshots as the ability lands, not at bar end: from bar end there is no time left to reach
    // the tower that follows. UNVERIFIED in game.
    private static EnemyAction AllThingsEnding(uint actionId, float rotation) => new(actionId)
    {
        Cast = new() { AnimationLock = 3f },
        Area = new() { Size = UmadConstants.Geometry.AllThingsEndHalfCone, Rotation = rotation },
        Effects = [Damage(Magic, Lethal)],
        Timing = new() { ResolveSnapshotOffset = CastSpec.ReleaseLead },
    };
}
