using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P3Tenstrike;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// H1, D2 and D4 take the Hatches; the rest wait D3 D1 MT ST H2 round the south from the west.
public class UcobP3TenstrikeTuuflessAiTests
{
    private const int Tuufless = 1;
    private static readonly Vector2[] NeurolinksWestToEast = UcobMarkers.Elemental.Place(Geometry.Neurolinks).OrderBy(n => n.X).ToArray();

    private static void H1D2D4Hatch(UcobP3TenstrikeStateOverrides o)
    {
        o.HatchTargets = [RegenHealer, MeleeDpsB, CasterDps];
        o.FirstEarthShakers = [MainTank, OffTank, ShieldHealer, CasterDps];
    }

    [Test]
    public void TheLineupRunsWestToEastD3H1D1MtStD2H2D4()
    {
        var spots = SpotsAt(UcobP3TenstrikeScenario.GenerateCasts[0] - 0.2f);
        PartyRole[] lineup = [PhysRangedDps, RegenHealer, MeleeDpsA, MainTank, OffTank, MeleeDpsB, ShieldHealer, CasterDps];
        Assert.That(lineup.Select(r => spots[r].X), Is.Ordered.Ascending);
        Assert.That(lineup.Select(r => MathF.Abs(spots[r].Y)), Is.All.LessThan(0.5f));
    }

    [Test]
    public void TheFirstHatchesTakeNorthwestSouthAndNortheastFromLeftToRight()
    {
        var spots = SpotsAt(UcobP3TenstrikeScenario.GenerateCasts[1] - 0.2f);
        Assert.That(Vector2.Distance(spots[RegenHealer], NeurolinksWestToEast[0]), Is.LessThan(Geometry.NeurolinkRadius));
        Assert.That(Vector2.Distance(spots[MeleeDpsB], NeurolinksWestToEast[1]), Is.LessThan(Geometry.NeurolinkRadius));
        Assert.That(Vector2.Distance(spots[CasterDps], NeurolinksWestToEast[2]), Is.LessThan(Geometry.NeurolinkRadius));
    }

    [Test]
    public void TheSecondHatchesAreCoveredByTheNearestOfThoseWaiting()
    {
        var spots = SpotsAt(UcobP3TenstrikeScenario.GenerateCasts[1] + 4f);
        Assert.That(Vector2.Distance(spots[PhysRangedDps], NeurolinksWestToEast[0]), Is.LessThan(Geometry.NeurolinkRadius));
        Assert.That(Vector2.Distance(spots[MeleeDpsA], NeurolinksWestToEast[1]), Is.LessThan(Geometry.NeurolinkRadius));
        Assert.That(Vector2.Distance(spots[ShieldHealer], NeurolinksWestToEast[2]), Is.LessThan(Geometry.NeurolinkRadius));
    }

    [Test]
    public void TheUnmarkedWaitOnTheSouthNeurolinkWhileTheMarkedSpreadNorth()
    {
        var spots = SpotsAt(UcobP3TenstrikeScenario.EarthShakers[0] - 0.2f);
        var south = NeurolinksWestToEast.MaxBy(n => n.Y);
        foreach (var role in new[] { RegenHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps })
            Assert.That(Vector2.Distance(spots[role], south), Is.LessThan(Geometry.NeurolinkRadius), $"{role}");
        foreach (var role in new[] { MainTank, OffTank, ShieldHealer, CasterDps })
            Assert.That(spots[role].Y, Is.LessThan(south.Y - 6f), $"{role}");
    }

    private static Dictionary<PartyRole, Vector2> SpotsAt(float time)
    {
        var spots = new Dictionary<PartyRole, Vector2>();
        Negative<UcobP3TenstrikeScenario>(MainTank, Tuufless)
            .Overrides<UcobP3TenstrikeStateOverrides>(H1D2D4Hatch)
            .At(time, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                    if (p.Member(role) is { } member)
                        spots[role] = new Vector2(member.Position.X, member.Position.Z);
            })
            .ShouldKillNoOneElse();
        return spots;
    }
}
