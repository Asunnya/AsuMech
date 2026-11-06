using System.Collections.Generic;
using AsuMech.Core.Game.Party;
using AsuMech.Learn;
using NUnit.Framework;

namespace AsuMech.Tests.Learn;

public class MasteryStoreTests
{
    private static readonly LessonStepKey Key = new("Ai", Time: 11.2f);
    private static readonly Lesson Lesson = new("S", "A", [], [new LessonStep(Key, Rule: "r"), new LessonStep(new LessonStepKey("Ai", Time: 20f), Rule: "r")]);

    [Test]
    public void HintDelayGrowsWithConsecutiveHitsAndResetsOnAMiss()
    {
        var saves = 0;
        var store = new MasteryStore(new Dictionary<string, int>(), () => saves++);
        Assert.That(store.HintDelayFraction(Lesson, PartyRole.MainTank, Key), Is.EqualTo(0f));
        store.Record(Lesson, PartyRole.MainTank, Key, StepResult.OnTime);
        store.Record(Lesson, PartyRole.MainTank, Key, StepResult.OnTime);
        Assert.That(store.HintDelayFraction(Lesson, PartyRole.MainTank, Key), Is.EqualTo(2f / 3f).Within(0.001f));
        Assert.That(store.Mastered(Lesson, PartyRole.MainTank, Key), Is.False);
        store.Record(Lesson, PartyRole.MainTank, Key, StepResult.OnTime);
        Assert.That(store.Mastered(Lesson, PartyRole.MainTank, Key), Is.True);
        Assert.That(store.MasteredCount(Lesson, PartyRole.MainTank), Is.EqualTo(1));
        store.Record(Lesson, PartyRole.MainTank, Key, StepResult.Late);
        Assert.That(store.Streak(Lesson, PartyRole.MainTank, Key), Is.EqualTo(0));
        Assert.That(saves, Is.EqualTo(4));
    }

    [Test]
    public void RolesAndRangesAreKeyedApart()
    {
        var store = new MasteryStore(new Dictionary<string, int>(), () => { });
        store.Record(Lesson, PartyRole.MainTank, Key, StepResult.OnTime);
        Assert.That(store.Streak(Lesson, PartyRole.OffTank, Key), Is.EqualTo(0));
        Assert.That(MasteryStore.StepId(new LessonStepKey("Ai", From: 119.7f, To: 123.9f)), Is.EqualTo("Ai@119.7-123.9"));
    }
}
