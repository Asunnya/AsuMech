using AsuMech.Scenarios.Fru.P1FallOfFaith;
using AsuMech.Scenarios.Fru.P1TurnOfTheHeavens;
using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Tests;

public class FruP1WholePhaseTests
{
    private static WholePhaseTimeline? recorded;
    private static WholePhaseTimeline Phase => recorded ??= WholePhaseTimeline.Record(typeof(FruP1FallOfFaithScenario));

    [Test]
    public void ItClearsStraightThrough()
        => Assert.That(Phase.Run.Passed, Phase.Run.ToString());

    // Retail starts it 1.3s before Utopian Sky's Sinbound resolves, which a part can't overlap: the
    // closest a part gets is casting it the moment it starts.
    [Test]
    public void TurnOfTheHeavensCastsItsCyclonicBreakAsItStarts()
    {
        var name = new FruP1TurnOfTheHeavensScenario().Name;
        var cast = Phase.FirstCast(name, ActionId.CyclonicBreakImageFire, ActionId.CyclonicBreakImageLightning);
        Assert.That(cast - Phase.PartBegan[Phase.PartIndex(name)], Is.LessThan(0.1f));
    }

    [Test]
    public void TurnOfTheHeavensKeepsUtopianSkysFogLifting()
    {
        var part = Phase.PartIndex(new FruP1TurnOfTheHeavensScenario().Name);
        Assert.That(Phase.Rows.Where(r => r.Part == part && r.Kind == "weather" && r.Id == WeatherId.Fog), Is.Empty);
    }

    [Test]
    public void FallOfFaithComesAsLongAfterBurnishedGloryAsInRetail()
        => Assert.That(Phase.Gap(new FruP1TurnOfTheHeavensScenario().Name, [ActionId.BurnishedGlory],
            [ActionId.FallOfFaithFire, ActionId.FallOfFaithLightning]), Is.EqualTo(6.55f).Within(0.5f));

    [Test]
    public void FatebreakerIsBackAsFallOfFaithStarts()
    {
        var part = Phase.PartIndex(new FruP1FallOfFaithScenario().Name);
        var spawn = Phase.Rows.First(r => r.Part == part && r.Kind == "spawn" && r.Id == BNpcBaseId.Fatebreaker);
        Assert.That(spawn.At - Phase.PartStarts[part], Is.LessThan(-1.5f));
    }
}
