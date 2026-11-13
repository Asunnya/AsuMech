using System;
using System.Numerics;
using AsuMech.Core.Game.Ai;

namespace AsuMech.Learn;

public enum StepResult { OnTime, Late, WrongPlace, MechanicFailed }

// Where the player was against one order's target, judged at its deadline: in place then, there
// later, never there before the next order, or caught by a mechanic meanwhile.
public sealed class StepEvaluation(Vector3 target, float tolerance, float deadline)
{
    public Vector3 Target => target;
    public float Tolerance => tolerance;
    // Never sooner than a run from where the player stood at the order, plus a moment to react.
    public float Deadline { get; private set; } = deadline;
    public StepResult? Result { get; private set; }
    public float? ArrivedAt { get; private set; }
    public float LastDistance { get; private set; } = float.MaxValue;
    // Distance when the result was settled; LastDistance keeps moving afterwards.
    public float? ResolvedDistance { get; private set; }
    public bool Within => LastDistance <= tolerance;

    // When the player first closed this much on the spot since the order, or was already on it:
    // the reaction, measured from the order (the moment the strat moves the bots).
    public const float ReactYalms = 1f;
    public const float ReactionGraceSeconds = 0.5f;
    public float? FirstSampledAt { get; private set; }
    public float? ReactedAt { get; private set; }
    private float startDistance;
    private float lastSampledAt;

    // speedFactor: the player's movement speed against the run speed (Slow, Sprint).
    public void Sample(float time, Vector3 position, float speedFactor = 1f)
    {
        lastSampledAt = time;
        LastDistance = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(target.X, target.Z));
        if (FirstSampledAt is null)
        {
            FirstSampledAt = time;
            startDistance = LastDistance;
            if (!Within) Deadline = MathF.Max(Deadline, time + LastDistance / (AiManager.RunSpeed * MathF.Max(speedFactor, 0.1f)) + ReactionGraceSeconds);
        }
        if (ReactedAt is null && (Within || LastDistance <= startDistance - ReactYalms)) ReactedAt = time;
        if (Result != null) return;
        if (time < Deadline)
        {
            if (Within) ArrivedAt ??= time;
            else ArrivedAt = null;
            return;
        }
        if (!Within) return;
        ArrivedAt ??= time;
        // Still there at the deadline counts; arriving after it is late.
        Result = ArrivedAt <= Deadline ? StepResult.OnTime : StepResult.Late;
        ResolvedDistance = LastDistance;
    }

    // A mechanic is taking the player. Holding the spot at that moment is the verdict, whatever the
    // deadline says; off it, the step stays open, since the order may name where the push lands.
    public StepResult? Settle()
    {
        if (Result != null) return Result;
        if (FirstSampledAt == null || !Within) return null;
        Result = StepResult.OnTime;
        ResolvedDistance = LastDistance;
        return Result;
    }

    public void MarkFailed()
    {
        if (Result != null) return;
        Result = StepResult.MechanicFailed;
        ResolvedDistance = LastDistance;
    }

    // The next order arrived or the run ended. On the spot: on time, whatever the deadline. Never
    // sampled, superseded before a player could react, or still on the way before the deadline: no
    // verdict, the step had no time to be failed.
    public StepResult? Close()
    {
        if (Result != null) return Result;
        if (FirstSampledAt == null) return null;
        if (!Within && (lastSampledAt - FirstSampledAt < ReactionGraceSeconds || ReactedAt != null && lastSampledAt < Deadline)) return null;
        Result = Within ? StepResult.OnTime : StepResult.WrongPlace;
        ResolvedDistance = LastDistance;
        return Result;
    }
}
