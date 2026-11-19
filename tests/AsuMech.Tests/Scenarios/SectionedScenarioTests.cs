using AsuMech.Scenarios;

namespace AsuMech.Tests.Scenarios;

public class SectionedScenarioTests
{
    private static IEnumerable<ISectionedScenario> Sectioned() => ScenarioCatalog.Create().Scenarios.OfType<ISectionedScenario>();

    [Test]
    public void TeaP3TeaP4TopP6AndUcobP2AreSectioned()
        => Assert.That(Sectioned().Select(s => s.GetType().Name), Is.EquivalentTo(new[]
            { "TeaP3AlexanderPrimeScenario", "TeaP4PerfectAlexanderScenario", "TopP6AlphaOmegaScenario", "UcobP2NaelScenario" }));

    [TestCaseSource(nameof(Sectioned))]
    public void APickedSectionIsThePracticeTheRunReads(ISectionedScenario scenario)
    {
        var overrides = scenario.SettingsOverrides!.GetType();
        var practice = overrides.GetProperty("Practice") ?? overrides.GetProperty("Push")!;
        for (var i = scenario.Sections.Count - 1; i >= 0; i--)
        {
            scenario.SelectedSection = i;
            Assert.That((int)practice.GetValue(scenario.SettingsOverrides)!, Is.EqualTo(i));
        }
    }
}
