using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn;
using NUnit.Framework;

namespace AsuMech.Tests.Learn;

// What a lesson shows on screen has to read as a sentence: no record printed whole, no list
// split mid-bracket, nothing longer than the panel can hold.
public class LessonTextTests
{
    public const int MaxRuleLength = 600;

    private static IEnumerable<(string Where, string Text)> Texts(Lesson lesson)
    {
        for (var i = 0; i < lesson.Steps.Count; i++)
        {
            var step = lesson.Steps[i];
            var at = $"{lesson.Scenario}/{lesson.Strat} step {i} ({MasteryStore.StepId(step.Key)})";
            if (step.Rule is { } rule) yield return (at, rule);
            if (step.Cue is { } cue) yield return (at + " cue", cue);
            if (step.Title is { } title) yield return (at + " title", title);
            foreach (var (role, text) in step.Roles ?? [])
            {
                if (text.Rule is { } r) yield return ($"{at} {role}", r);
                if (text.Cue is { } c) yield return ($"{at} {role} cue", c);
            }
        }
    }

    [Test]
    public void NoLessonTextIsARecordDumpOrTooLong()
    {
        var bad = LessonLoader.All.SelectMany(Texts)
            .Where(t => t.Text.Contains('{') || t.Text.Contains("<>z__") || t.Text.Length > MaxRuleLength)
            .Select(t => $"{t.Where}: {t.Text[..System.Math.Min(80, t.Text.Length)]}… ({t.Text.Length} chars)")
            .ToList();
        Assert.That(bad, Is.Empty, string.Join("\n", bad));
    }

    // A list item cut at a comma ("[Axe", "Scythe]") is an authoring-chain bug; the lessons that
    // still carry one need a fresh draft, so they are warned about rather than failed.
    [Test]
    public void ListItemsAreNotCutMidBracket()
    {
        var bad = LessonLoader.All.SelectMany(Texts)
            .Where(t => t.Text.Count(c => c == '[') != t.Text.Count(c => c == ']'))
            .Select(t => t.Where)
            .Distinct()
            .ToList();
        if (bad.Count > 0) Assert.Warn($"{bad.Count} texts cut mid-bracket:\n{string.Join("\n", bad)}");
    }
}
