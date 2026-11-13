using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn.Toolbox;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Toolbox;

public class LessonToolboxTests
{
    private static LessonToolbox Link(Dictionary<string, int> steps, List<int>? pages = null) => new("x", [], steps, Pages: pages);

    [Test]
    public void ARolledKeyWinsOverThePlainOne()
    {
        var link = Link(new() { ["Ai@15.74"] = 0, ["Ai@15.74|AxeTrophyFirst=false"] = 1 });
        Assert.Multiple(() =>
        {
            Assert.That(link.PageFor("Ai@15.74", new Dictionary<string, string> { ["AxeTrophyFirst"] = "false" }), Is.EqualTo(1));
            Assert.That(link.PageFor("Ai@15.74", new Dictionary<string, string> { ["AxeTrophyFirst"] = "true" }), Is.EqualTo(0));
            Assert.That(link.PageFor("Ai@15.74", new Dictionary<string, string>()), Is.EqualTo(0));
        });
    }

    [Test]
    public void EveryRollOfAKeyMustMatch()
    {
        var link = Link(new() { ["Ai@1|A=1&B=2"] = 3 });
        Assert.Multiple(() =>
        {
            Assert.That(link.PageFor("Ai@1", new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" }), Is.EqualTo(3));
            Assert.That(link.PageFor("Ai@1", new Dictionary<string, string> { ["A"] = "1", ["B"] = "3" }), Is.Null);
        });
    }

    [Test]
    public void AStepWithNoKeyKeepsThePage() => Assert.That(Link(new() { ["Ai@1"] = 2 }).PageFor("Ai@9", new Dictionary<string, string>()), Is.Null);

    [Test]
    public void ExtraPagesCanBePagedThrough()
        => Assert.That(Link(new() { ["Ai@1"] = 2 }, [4, 5]).AllPages.Order(), Is.EqualTo(new[] { 2, 4, 5 }));
}
