using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn;

// Why a bot is where it is, from the last order the strat gave its seat: which call, when, to
// where, how far it still has to go, and what the lesson says about that step for its role.
public static class BotInspection
{
    public static IReadOnlyList<(string Label, string Text)> Describe(PartyRole role, GuidanceOrder? order, Vector3 position, float now, Lesson? lesson)
    {
        var lines = new List<(string, string)> { ("Seat", role.ToString()) };
        if (order is not { } o)
        {
            lines.Add(("Order", "none yet: the strat hasn't moved this seat in this run"));
            return lines;
        }
        lines.Add(("Call", $"{o.Key.StratClass}{(o.Key.Method is { } m ? $".{m}" : "")} at {F(o.Key.Time)}s ({o.Key.Source})"));
        lines.Add(("Issued", $"{F(o.IssuedAt)}s, {F(MathF.Max(0f, now - o.IssuedAt))}s ago{(o.Sprint ? ", sprinting" : "")}"));
        var distance = Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(o.Target.X, o.Target.Z));
        lines.Add(("Spot", $"({F(o.Target.X)}, {F(o.Target.Z)}), {(distance <= 0.5f ? "there" : $"{F(distance)} y away")}{(o.ArrivalTime is { } at ? $", due by {F(at)}s" : "")}"));
        if (lesson?.StepFor(o.Key) is { } step)
        {
            lines.Add(("Step", step.Title ?? MasteryStore.StepId(step.Key)));
            if (step.CueFor(role) is { } cue) lines.Add(("Cue", cue));
            if (step.RuleFor(role) is { } rule) lines.Add(("Rule", rule));
        }
        else if (lesson is not null) lines.Add(("Step", "no lesson step for this call"));
        return lines;
    }

    private static string F(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
