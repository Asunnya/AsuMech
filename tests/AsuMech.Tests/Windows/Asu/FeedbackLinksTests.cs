using System;
using AsuMech.Windows.Asu;
using NUnit.Framework;

namespace AsuMech.Tests.Windows.Asu;

public class FeedbackLinksTests
{
    private static readonly FeedbackContext Context = new("Dragonsong's Reprise — P6 & Cauterize", "Strat #1", "MT", false, 1849203, "0.5.1.9");

    [TestCase(0, "scenario-request.md")]
    [TestCase(1, "strat-request.md")]
    [TestCase(2, "bug-report.md")]
    public void PointsAtTheTemplateWithoutLabels(int kind, string template)
    {
        var issue = FeedbackLinks.Build((FeedbackKind)kind, Context);
        Assert.That(issue.Url, Does.StartWith($"{FeedbackLinks.Repository}/issues/new?template={template}&"));
        Assert.That(issue.Url, Does.Not.Contain("labels="));
    }

    [Test]
    public void QueryValuesAreEncoded()
    {
        var url = FeedbackLinks.Build(FeedbackKind.Bug, Context).Url;
        var query = url[(url.IndexOf('?') + 1)..];
        Assert.That(query, Does.Not.Contain(" ").And.Not.Contain("#").And.Not.Contain("'").And.Not.Contain("—"));
        Assert.That(query.Split('&'), Has.Length.EqualTo(3));
        Assert.That(Uri.UnescapeDataString(url), Does.Contain("Seed: 1849203"));
    }

    [Test]
    public void LongBodiesAreCut()
    {
        var huge = Context with { Scenario = new string('x', 10_000) };
        var issue = FeedbackLinks.Build(FeedbackKind.Bug, huge);
        Assert.That(issue.Body, Has.Length.LessThanOrEqualTo(FeedbackLinks.MaxIssueBodyChars));
        Assert.That(issue.Body, Does.EndWith("(truncated)"));
    }

    [Test]
    public void ContextCannotCarryPlayerIdentity()
    {
        var names = Array.ConvertAll(typeof(FeedbackContext).GetProperties(), p => p.Name.ToLowerInvariant());
        Assert.That(names, Has.None.Contains("name").And.None.Contains("world").And.None.Contains("character"));
    }
}
