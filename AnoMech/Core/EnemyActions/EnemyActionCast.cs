using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// One SimEnemy.Cast(EnemyAction, ...), for a later timeline event to read what it did. Filled at
// resolve; an action with no effects never resolves.
public sealed class EnemyActionCast
{
    public bool IsResolved { get; private set; }
    public bool IsCancelled { get; private set; }
    public Placement Origin { get; private set; }

    // At = where Who stood at resolve. Killed is decided at resolve; they die TimingSpec.DamageDelay later.
    public sealed record Hit(SimCharacter Who, Vector3 At, bool Killed);

    // Nearest to Origin first.
    public IReadOnlyList<Hit> Hits { get; private set; } = [];

    internal void Cancelled() => IsCancelled = true;

    internal void Resolve(Placement origin, IReadOnlyList<Hit> hits)
    {
        IsResolved = true;
        Origin = origin;
        Hits = hits;
    }
}
