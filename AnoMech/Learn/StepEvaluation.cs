using System.Numerics;

namespace AnoMech.Learn;

public enum StepResult { OnTime, Late, WrongPlace, MechanicFailed }

// Where the player was against one order's target, judged at its deadline: in place then, there
// later, never there before the next order, or caught by a mechanic meanwhile.
public sealed class StepEvaluation(Vector3 target, float tolerance, float deadline)
{
    public Vector3 Target => target;
    public float Tolerance => tolerance;
    public float Deadline => deadline;
    public StepResult? Result { get; private set; }
    public float? ArrivedAt { get; private set; }
    public float LastDistance { get; private set; } = float.MaxValue;
    public bool Within => LastDistance <= tolerance;

    public void Sample(float time, Vector3 position)
    {
        LastDistance = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(target.X, target.Z));
        if (Result != null) return;
        if (time < deadline)
        {
            if (Within) ArrivedAt ??= time;
            else ArrivedAt = null;
            return;
        }
        if (!Within) return;
        ArrivedAt ??= time;
        // Still there at the deadline counts; arriving after it is late.
        Result = ArrivedAt <= deadline ? StepResult.OnTime : StepResult.Late;
    }

    public void MarkFailed()
    {
        if (Result is null) Result = StepResult.MechanicFailed;
    }

    // The next order arrived or the run ended.
    public StepResult Close()
    {
        Result ??= StepResult.WrongPlace;
        return Result.Value;
    }
}
