using System;
using System.Collections.Generic;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// Everything one enemy action does, cast through SimEnemy.Cast(EnemyAction, ...). Definitions are
// immutable and meant to live as static readonly fields in a scenario's <Scenario>Actions.cs.
public sealed record EnemyAction(uint ActionId)
{
    public CastSpec Cast { get; init; } = new();
    public AreaSpec Area { get; init; } = new();
    public IReadOnlyList<IEnemyActionEffect> Effects { get; init; } = [];
    public TimingSpec Timing { get; init; } = new();

    // Said of every death this action deals, ahead of the hit's own reason: "Hello, World (Failed
    // Hello World mechanic; had vuln up debuff)".
    public string? DeathExplanation { get; init; }
}

// The bar runs ReleaseLead short of the Action sheet's Cast100ms, the action effect lands at the
// sheet's full cast time; a sheet-instant action has no bar and releases at once.
public sealed record CastSpec
{
    public const float ReleaseLead = 0.3f;

    public float AnimationLock { get; init; } = 0.6f;
    public float OmenDelay { get; init; }
}

// Shape defaults to the Action sheet (see CharacterFind.InsideActionAoe), centred on the cast
// target when there is one, otherwise on the caster. A cone or line cast on a target runs from the
// caster towards it instead.
public sealed record AreaSpec
{
    // The dimension the sheet lacks; meaning depends on CastType (see InsideActionAoe).
    public float? Size { get; init; }

    // Replaces the sheet's CastType, for an action whose sheet shape is custom (CastType 6).
    public byte? CastType { get; init; }

    // Turns the omen and the area from the caster's facing, in radians.
    public float Rotation { get; init; }

    // Runs before any effect, so it also decides who counts toward a stack.
    public Func<EnemyActionContext, IReadOnlyList<SimCharacter>, IReadOnlyList<SimCharacter>>? AdjustTargets { get; init; }
}

// Offsets from bar end (the Cast() call itself for an instant action), on the scenario clock: the
// bar itself runs in real time, so under an EventTimeScale other than 1 they drift from it.
public sealed record TimingSpec
{
    // Who is hit, statuses, and who dies are all decided here.
    // This is quite hard to measure so leave default unless there is good reason to change
    public float ResolveSnapshotOffset { get; init; }
    // From resolve: when damage lands (flytext, knockback without its own delay) and a character the hit kills dies.
    // The default is a placeholder; set each action's own once it's measured from replay data
    public float DamageDelay { get; init; } = 1f;
}
