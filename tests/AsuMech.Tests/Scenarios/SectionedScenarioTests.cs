using AsuMech.Scenarios;

namespace AsuMech.Tests.Scenarios;

public class SectionedScenarioTests
{
    private static IEnumerable<ISectionedScenario> Sectioned() => ScenarioCatalog.Create().Scenarios.OfType<ISectionedScenario>();

    [Test]
    public void TeaP3TeaP4AndTopP6AreSectioned()
        => Assert.That(Sectioned().Select(s => s.GetType().Name), Is.EquivalentTo(new[]
            { "TeaP3AlexanderPrimeScenario", "TeaP4PerfectAlexanderScenario", "TopP6AlphaOmegaScenario" }));

    [TestCaseSource(nameof(Sectioned))]
    public void APickedSectionIsThePracticeTheRunReads(ISectionedScenario scenario)
    {
        Assert.That(scenario.Sections[0].Label, Is.EqualTo("Whole phase"));
        var practice = scenario.SettingsOverrides!.GetType().GetProperty("Practice")!;
        for (var i = scenario.Sections.Count - 1; i >= 0; i--)
        {
            scenario.SelectedSection = i;
            Assert.That((int)practice.GetValue(scenario.SettingsOverrides)!, Is.EqualTo(i));
        }
    }
}
