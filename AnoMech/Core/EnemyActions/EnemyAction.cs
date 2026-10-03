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
}

public sealed record CastSpec
{
    // Null reads Cast100ms from the Action sheet; 0 is an instant action with no cast bar.
    public float? CastTime { get; init; }
    public float AnimationLock { get; init; } = 0.6f;
    public float OmenDelay { get; init; }
    public byte Variation { get; init; }
}

// Shape defaults to the Action sheet (see CharacterFind.InsideActionAoe), centred on the cast
// target when there is one, otherwise on the caster.
public sealed record AreaSpec
{
    // The dimension the sheet lacks; meaning depends on CastType (see InsideActionAoe).
    public float? Size { get; init; }

    // Runs before any effect, so it also decides who counts toward a stack.
    public Func<EnemyActionContext, IReadOnlyList<SimCharacter>, IReadOnlyList<SimCharacter>>? AdjustTargets { get; init; }
}

// Offsets from bar end (the Cast() call itself for an instant action), on the scenario clock: the
// bar itself runs in real time, so under an EventTimeScale other than 1 they drift from it.
public sealed record TimingSpec
{
    public float VfxOffset { get; init; }
    // Who is hit, statuses, and who dies are all decided here.
    public float ResolveOffset { get; init; }
    // From resolve: when damage flytext shows, and when a character the hit kills actually dies.
    public float DamageDelay { get; init; }
    public float DeathDelay { get; init; }
}
