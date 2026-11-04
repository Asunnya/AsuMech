using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn;

public enum LearnLevel { Observe, Guided, Assisted, Exam }

// ShowAt: sim time from which the hint (target and rule) is drawn; Assisted pushes it later per mastery.
public sealed record ActiveStep(GuidanceOrder Order, LessonStep? Step, StepEvaluation Evaluation, string Title, string? Rule, string? Cue, float ShowAt)
{
    public bool HintVisible(float now) => now >= ShowAt;
}

// Turns the strat's orders to the player into lesson steps, judges each, and slows the sim while
// a Guided player is still on the way. Speed goes out through a callback so this stays headless.
public sealed class LessonDirector : IPlayerGuidanceSink
{
    public const float GuidedSlowScale = 0.25f;
    public const float DefaultWindowSeconds = 6f;

    private readonly Lesson lesson;
    private readonly PartyRole role;
    private readonly Action<float> setSpeed;
    private readonly MasteryStore? mastery;
    private readonly List<(ActiveStep Step, StepResult Result)> results = [];
    private float lastSpeed = 1f;

    public LessonDirector(Lesson lesson, PartyRole role, LearnLevel level, Action<float> setSpeed, MasteryStore? mastery = null)
    {
        this.mastery = mastery;
        this.lesson = lesson;
        this.role = role;
        Level = level;
        this.setSpeed = setSpeed;
    }

    public LearnLevel Level { get; }
    // Observe is the strat playing the slot: nothing to judge or to count as mastery.
    public bool Judging => Level != LearnLevel.Observe;
    public ActiveStep? Current { get; private set; }
    public IReadOnlyList<(ActiveStep Step, StepResult Result)> Results => results;

    public event Action<ActiveStep>? StepChanged;
    public event Action<ActiveStep, StepResult>? StepResolved;

    public void OnMove(GuidanceOrder order)
    {
        CloseCurrent();
        var step = lesson.StepFor(order.Key);
        if (step is { Skip: true })
        {
            Current = null;
            Speed(1f);
            return;
        }
        var deadline = order.ArrivalTime ?? order.IssuedAt + DefaultWindowSeconds;
        var tolerance = step?.ToleranceFor(role) ?? 1.5f;
        var title = step?.Title ?? order.Key.Method ?? $"Move at {order.Key.Time:0.#}s";
        var showAt = order.IssuedAt;
        if (Level == LearnLevel.Assisted && step != null && mastery != null)
        {
            var fraction = mastery.HintDelayFraction(lesson, role, step.Key);
            showAt = fraction >= 1f ? float.MaxValue : order.IssuedAt + (deadline - order.IssuedAt) * fraction;
        }
        Current = new ActiveStep(order, step, new StepEvaluation(order.Target, tolerance, deadline), title, step?.RuleFor(role), step?.CueFor(role), showAt);
        StepChanged?.Invoke(Current);
    }

    public void OnMechanicFailed()
    {
        if (!Judging || Current is not { Evaluation.Result: null } current) return;
        current.Evaluation.MarkFailed();
        Resolve(current);
    }

    // Once per frame with the sim clock and the player's local position.
    public void Tick(float now, Vector3 playerPosition)
    {
        if (Current is not { } current)
        {
            Speed(1f);
            return;
        }
        if (!Judging) return;
        var wasResolved = current.Evaluation.Result != null;
        current.Evaluation.Sample(now, playerPosition);
        if (!wasResolved && current.Evaluation.Result != null) Resolve(current);
        var onTheWay = current.Evaluation.Result == null && !current.Evaluation.Within;
        Speed(Level == LearnLevel.Guided && onTheWay ? GuidedSlowScale : 1f);
    }

    public void End()
    {
        CloseCurrent();
        Current = null;
        Speed(1f);
    }

    private void CloseCurrent()
    {
        if (!Judging || Current is not { } current) return;
        if (current.Evaluation.Result == null)
        {
            current.Evaluation.Close();
            Resolve(current);
        }
    }

    private void Resolve(ActiveStep step)
    {
        var result = step.Evaluation.Result!.Value;
        results.Add((step, result));
        if (step.Step is { Skip: false } lessonStep) mastery?.Record(lesson, role, lessonStep.Key, result);
        StepResolved?.Invoke(step, result);
    }

    private void Speed(float scale)
    {
        if (Math.Abs(scale - lastSpeed) < 0.001f) return;
        lastSpeed = scale;
        setSpeed(scale);
    }
}
