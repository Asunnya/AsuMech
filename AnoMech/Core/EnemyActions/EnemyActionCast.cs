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

    // Nearest to Origin first, each where it stood at resolve.
    public IReadOnlyList<(SimCharacter Who, Vector3 At)> Hits { get; private set; } = [];

    // Decided at resolve; they die TimingSpec.DamageDelay later.
    public IReadOnlyCollection<SimCharacter> Kills { get; private set; } = [];

    internal void Cancelled() => IsCancelled = true;

    internal void Resolved(Placement origin, IReadOnlyList<(SimCharacter Who, Vector3 At)> hits)
    {
        IsResolved = true;
        Origin = origin;
        Hits = hits;
    }

    internal void Killed(IReadOnlyCollection<SimCharacter> kills) => Kills = kills;
}
