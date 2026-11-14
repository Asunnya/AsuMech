using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Quickmarch;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Tests;

// The dragons dive from the north, so the Tuufless layout reads as drawn: MT group west, ST group east.
public class UcobP3QuickmarchAiTests
{
    private const int Tuufless = 1;

    [Test]
    public void TuuflessLinesUpTankHealerMeleeRangedFromTheDragonsDown()
    {
        var at = PositionsAt(UcobP3QuickmarchScenario.DivesResolve - 0.1f, CasterDps);
        Assert.Multiple(() =>
        {
            foreach (var group in new[] { new[] { MainTank, RegenHealer, MeleeDpsA, PhysRangedDps }, new[] { OffTank, ShieldHealer, MeleeDpsB, CasterDps } })
            {
                var west = group[0] == MainTank;
                Assert.That(group.Select(r => at[r].X), west ? Is.All.LessThan(-15f) : Is.All.GreaterThan(15f), $"{group[0]}'s group is on the wrong side");
                Assert.That(group.Select(r => at[r].Y), Is.Ordered.Ascending, $"{group[0]}'s group is not tank > healer > melee > ranged from the north");
            }
        });
    }

    [Test]
    public void TuuflessBaitsEarthShakersWestEastAndNorthWithTheTanksInFrontOfTheHealersAndTheStackSouth()
    {
        var at = PositionsAt(UcobP3QuickmarchScenario.EarthShakers - 0.1f, PhysRangedDps);
        Assert.Multiple(() =>
        {
            Assert.That(at[RegenHealer].X, Is.LessThan(-12f));
            Assert.That(at[ShieldHealer].X, Is.GreaterThan(12f));
            Assert.That(at[MainTank].X, Is.InRange(at[RegenHealer].X, -4f));
            Assert.That(at[OffTank].X, Is.InRange(4f, at[ShieldHealer].X));
            Assert.That(MathF.Abs(at[MainTank].Y - at[RegenHealer].Y), Is.LessThan(1f));
            Assert.That(MathF.Abs(at[OffTank].Y - at[ShieldHealer].Y), Is.LessThan(1f));
            Assert.That(at[CasterDps].Y, Is.LessThan(-12f), "the unmarked DPS is not north");
            foreach (var marked in new[] { MeleeDpsA, MeleeDpsB, PhysRangedDps })
                Assert.That(Vector2.Distance(at[marked], new Vector2(0f, 8f)), Is.LessThan(1.5f), $"{marked} is not in the south stack");
        });
    }

    private static Dictionary<PartyRole, Vector2> PositionsAt(float time, PartyRole player)
    {
        var at = new Dictionary<PartyRole, Vector2>();
        var run = ScenarioRun.Execute(typeof(UcobP3QuickmarchScenario), Tuufless, 1, new ScenarioRunOptions
        {
            PlayerRole = player,
            Overrides = o =>
            {
                var overrides = (UcobP3QuickmarchStateOverrides)o;
                overrides.DiveFrom = Direction.N;
                overrides.EarthShakerDps = CasterDps;
            },
            Probe = p =>
            {
                if (!p.Crossed(time)) return;
                foreach (var role in Enum.GetValues<PartyRole>())
                    if (p.Member(role) is { } member) at[role] = new Vector2(member.Position.X, member.Position.Z);
            },
        });
        Assert.That(run.Passed, run.ToString);
        return at;
    }
}
