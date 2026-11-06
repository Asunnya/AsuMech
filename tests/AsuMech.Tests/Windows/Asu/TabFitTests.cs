using System.Linq;
using AsuMech.Windows.Asu;
using NUnit.Framework;

namespace AsuMech.Tests.Windows.Asu;

public class TabFitTests
{
    private const float Separator = 10f;
    private const float Button = 20f;
    private static float Label(int group) => 30f;

    private static TabFitResult Fit(float available, int selected, params TabMeasure[] tabs)
        => TabFit.Compute(tabs, Label, Separator, available, Button, selected);

    private static TabMeasure[] Ungrouped(int count) => Enumerable.Repeat(new TabMeasure(50f), count).ToArray();

    [Test]
    public void EverythingFits()
    {
        var result = Fit(250f, 0, Ungrouped(5));
        Assert.That(result.Visible, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        Assert.That(result.Overflows, Is.False);
    }

    [Test]
    public void OverflowReservesTheButtonAndHidesTheRest()
    {
        var result = Fit(200f, 0, Ungrouped(5));
        Assert.That(result.Visible, Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(result.Hidden, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void HiddenSelectionReplacesTheLastVisibleTab()
    {
        var result = Fit(200f, 4, Ungrouped(5));
        Assert.That(result.Visible, Is.EqualTo(new[] { 0, 1, 4 }));
        Assert.That(result.Hidden, Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void WideSelectionPushesOutSeveralTabs()
    {
        TabMeasure[] tabs = [new(50f), new(50f), new(50f), new(120f)];
        Assert.That(Fit(200f, 3, tabs).Visible, Is.EqualTo(new[] { 0, 3 }));
    }

    [Test]
    public void OnlyTheSelectionFits()
    {
        var result = Fit(60f, 3, Ungrouped(5));
        Assert.That(result.Visible, Is.EqualTo(new[] { 3 }));
        Assert.That(result.Hidden, Has.Count.EqualTo(4));
    }

    [Test]
    public void GroupLabelsGoBeforeAnyTab()
    {
        TabMeasure[] tabs = [new(50f, 0), new(50f, 0), new(50f, 1)];
        var withLabels = Fit(250f, 0, tabs);
        Assert.That(withLabels.ShowGroupLabels, Is.True);
        var withoutLabels = Fit(170f, 0, tabs);
        Assert.That(withoutLabels.ShowGroupLabels, Is.False);
        Assert.That(withoutLabels.Overflows, Is.False);
    }

    [Test]
    public void SeparatorsCountBetweenGroups()
    {
        TabMeasure[] tabs = [new(50f, 0), new(50f, 1)];
        Assert.That(Fit(105f, 0, tabs).Overflows, Is.True);
        Assert.That(Fit(110f, 0, tabs).Overflows, Is.False);
    }
}
