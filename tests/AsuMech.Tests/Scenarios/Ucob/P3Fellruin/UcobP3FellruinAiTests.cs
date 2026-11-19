using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P3Fellruin;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Tests;

public class UcobP3FellruinAiTests
{
    private const int Tuufless = 1;

    [Test]
    public void OnElementalMarkersEachBossStandsOverItsOwnNeurolink([Values(0, 1, 2)] int slot, [Values] bool naelClockwise)
    {
        var state = new UcobP3FellruinState(new Rng(1), new UcobP3FellruinStateOverrides { BahamutSlot = slot, NaelClockwiseOfBahamut = naelClockwise });
        var bosses = new[] { (state.BahamutBearing, state.Bahamut), (state.NaelBearing, state.Nael), (state.TwintaniaBearing, state.Twintania) };
        var links = bosses.Select(b => UcobP3FellruinState.NeurolinkNearest(b.Item1, UcobMarkers.Elemental)).ToList();
        Assert.That(links, Is.Unique);
        for (var i = 0; i < bosses.Length; i++)
            Assert.That(Vector2.Distance(links[i], bosses[i].Item2), Is.LessThan(2f));
    }

    [Test]
    public void TuuflessTanksTakeTheNeurolinksUnderBahamutAndTwintaniaAndThePartyNaels(
        [Values(0, 1, 2)] int slot, [Values] bool naelClockwise, [Values(0, 1)] int quote)
    {
        var at = new Dictionary<PartyRole, Vector2>();
        UcobP3FellruinState? state = null;
        var run = ScenarioRun.Execute(typeof(UcobP3FellruinScenario), Tuufless, 1, new ScenarioRunOptions
        {
            PlayerRole = CasterDps,
            Overrides = o =>
            {
                var overrides = (UcobP3FellruinStateOverrides)o;
                overrides.BahamutSlot = slot;
                overrides.NaelClockwiseOfBahamut = naelClockwise;
                overrides.Quote = quote;
                state = new UcobP3FellruinState(new Rng(0), overrides);
            },
            Probe = p =>
            {
                if (!p.Crossed(UcobP3FellruinScenario.TempestWing - 0.1f)) return;
                foreach (var role in Enum.GetValues<PartyRole>())
                    if (p.Member(role) is { } member) at[role] = new Vector2(member.Position.X, member.Position.Z);
            },
        });
        Assert.That(run.Passed, run.ToString);

        Vector2 Under(float bearing) => UcobP3FellruinState.NeurolinkNearest(bearing, UcobMarkers.Elemental);
        Assert.Multiple(() =>
        {
            Assert.That(Vector2.Distance(at[MainTank], Under(state!.BahamutBearing)), Is.LessThan(Geometry.NeurolinkRadius), "MT");
            Assert.That(Vector2.Distance(at[OffTank], Under(state.TwintaniaBearing)), Is.LessThan(Geometry.NeurolinkRadius), "ST");
            foreach (var role in Enum.GetValues<PartyRole>().Where(r => !r.IsTank()))
                Assert.That(Vector2.Distance(at[role], Under(state.NaelBearing)), Is.LessThan(Geometry.NeurolinkRadius), $"{role}");
        });
    }
}
