using System;
using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn;

// What a lesson says about one strat call. Matches by strat class and the call's time literal, or
// a time range for calls repeated in a loop; the line is a tiebreaker when two calls share a time.
public sealed record LessonStepKey(string Cls, float? Time = null, float? From = null, float? To = null, int? Line = null)
{
    private const float TimeSlack = 0.01f;

    public bool Matches(GuidanceKey key)
    {
        if (key.StratClass != Cls) return false;
        if (Time is { } t) return Math.Abs(key.Time - t) <= TimeSlack;
        if (From is { } from && To is { } to) return key.Time >= from - TimeSlack && key.Time <= to + TimeSlack;
        return false;
    }

    public bool SameLine(GuidanceKey key) => Line is { } line && key.Source.EndsWith($":{line}", StringComparison.Ordinal);
}

public sealed record LessonRoleText(string? Rule = null, string? Cue = null, float? Tolerance = null);

public sealed record LessonStep(
    LessonStepKey Key,
    string? Title = null,
    string? Cue = null,
    string? Rule = null,
    Dictionary<string, LessonRoleText>? Roles = null,
    float Tolerance = 1.5f,
    bool Skip = false,
    string? Where = null)
{
    public LessonRoleText? For(PartyRole role) => Roles is { } r && r.TryGetValue(role.ToString(), out var text) ? text : null;
    public string? RuleFor(PartyRole role) => For(role)?.Rule ?? Rule;
    public string? CueFor(PartyRole role) => For(role)?.Cue ?? Cue;
    public float ToleranceFor(PartyRole role) => For(role)?.Tolerance ?? Tolerance;
}

public sealed record LessonSource(string Label, string Url);

// Draft: the text came out of the authoring chain and nobody has reviewed it in game yet.
public sealed record Lesson(string Scenario, string Strat, List<LessonSource> Sources, List<LessonStep> Steps, Toolbox.LessonToolbox? Toolbox = null, bool Draft = false)
{
    // A drafted rule that still lists rolls and spots instead of telling the player what to read.
    public static bool IsTable(string? rule) => rule != null && (rule.StartsWith("Depends on ", StringComparison.Ordinal) || rule.Contains("read the strat", StringComparison.OrdinalIgnoreCase));

    // The step for an order: by time, then the line breaks ties.
    public LessonStep? StepFor(GuidanceKey key)
    {
        LessonStep? first = null;
        foreach (var step in Steps)
        {
            if (!step.Key.Matches(key)) continue;
            if (step.Key.SameLine(key)) return step;
            first ??= step;
        }
        return first;
    }
}
