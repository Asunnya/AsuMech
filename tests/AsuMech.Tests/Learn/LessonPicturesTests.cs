using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Learn;
using NUnit.Framework;

namespace AsuMech.Tests.Learn;

public class LessonPicturesTests
{
    private static IEnumerable<Lesson> WithPictures => LessonLoader.All.Where(l => l.Pictures != null);

    [Test]
    public void TheUmadP1LessonShowsPictures()
        => Assert.That(WithPictures.Any(l => l.Scenario == "UmadP1TeleTrouncingScenario" && l.Toolbox is null), Is.True);

    [TestCaseSource(nameof(WithPictures))]
    public void EveryPageIsEmbeddedAndTitled(Lesson lesson)
    {
        Assert.That(lesson.Pictures!.Pages, Is.Not.Empty);
        Assert.That(lesson.Pictures.Credit, Is.Not.Empty);
        foreach (var page in lesson.Pictures.Pages)
        {
            Assert.That(page.Title, Is.Not.Empty);
            Assert.That(LessonPictures.Embedded(page.Image), Is.True, page.Image);
        }
    }

    [TestCaseSource(nameof(WithPictures))]
    public void StepLinksPointAtRealStepsAndPages(Lesson lesson)
    {
        var ids = lesson.Steps.Select(s => MasteryStore.StepId(s.Key)).ToHashSet();
        foreach (var (step, page) in lesson.Pictures!.Steps)
        {
            Assert.That(ids, Does.Contain(step));
            Assert.That(page, Is.InRange(0, lesson.Pictures.Pages.Count - 1), step);
        }
    }

    [TestCaseSource(nameof(WithPictures))]
    public void RoleTextsUsePartyRoles(Lesson lesson)
    {
        foreach (var page in lesson.Pictures!.Pages.Where(p => p.Roles != null))
            foreach (var (role, text) in page.Roles!)
            {
                Assert.That(Enum.TryParse<PartyRole>(role, out _), Is.True, $"{page.Title}: {role}");
                Assert.That(text, Is.Not.Empty);
            }
    }
}
