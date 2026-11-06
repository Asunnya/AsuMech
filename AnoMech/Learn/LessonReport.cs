using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech.Learn;

// One area that caught the player: when, during which step, what the step wanted, and why, in words.
public sealed record LessonHit(float Time, string? StepTitle, System.Numerics.Vector3? StepTarget, string Action, System.Numerics.Vector3 Position, string Phrase, bool Fatal);

// One judged run: every step the director resolved, in order, and every hit the player took.
public sealed record LessonReport(IScenario Scenario, IScenarioAi Strat, Lesson Lesson, LearnLevel Level, PartyRole Role,
    IReadOnlyList<(ActiveStep Step, StepResult Result)> Results, IReadOnlyList<LessonHit>? Hits = null)
{
    public IReadOnlyList<LessonHit> Hits { get; init; } = Hits ?? [];

    public int InPlace => Results.Count(r => r.Result == StepResult.OnTime);

    public string Summary => $"{InPlace}/{Results.Count} in place";

    public string Title => $"{Scenario.Name} · {Strat.Name} · {Level} as {Role}";
}
