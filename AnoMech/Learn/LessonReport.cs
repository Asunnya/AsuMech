using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn;

// One judged run: every step the director resolved, in order.
public sealed record LessonReport(string Scenario, string Strat, LearnLevel Level, PartyRole Role,
    IReadOnlyList<(ActiveStep Step, StepResult Result)> Results)
{
    public int InPlace => Results.Count(r => r.Result == StepResult.OnTime);

    public string Summary => $"{InPlace}/{Results.Count} in place";

    public string Title => $"{Scenario} · {Strat} · {Level} as {Role}";
}
