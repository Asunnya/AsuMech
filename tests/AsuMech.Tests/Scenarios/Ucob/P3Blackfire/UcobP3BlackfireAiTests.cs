using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Blackfire;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Tests;

public class UcobP3BlackfireAiTests
{
    private const int Tuufless = 1;
    private const float TowerRadius = 3f;

    [TestCase(new[] { OffTank, ShieldHealer, MeleeDpsB, CasterDps }, MainTank, RegenHealer, MeleeDpsA, PhysRangedDps)]
    [TestCase(new[] { MainTank, RegenHealer, MeleeDpsA, MeleeDpsB }, OffTank, ShieldHealer, PhysRangedDps, CasterDps)]
    public void TuuflessTankTakesNorthWestHealerSouthWestAndDpsByPriorityNorthEastThenSouthEast(
        PartyRole[] stack, PartyRole northWest, PartyRole southWest, PartyRole northEast, PartyRole southEast)
    {
        var beside = new Dictionary<PartyRole, Vector2>();
        var inside = new Dictionary<PartyRole, Vector2>();
        var run = ScenarioRun.Execute(typeof(UcobP3BlackfireScenario), Tuufless, 1, new ScenarioRunOptions
        {
            PlayerRole = northWest,
            Overrides = o =>
            {
                var overrides = (UcobP3BlackfireStateOverrides)o;
                overrides.NaelFrom = Direction.N;
                overrides.StackTargets = stack;
            },
            Probe = p =>
            {
                if (p.Crossed(UcobP3BlackfireScenario.Hypernovas[1] - 0.1f)) Record(p, beside);
                if (p.Crossed(UcobP3BlackfireScenario.TowersResolve - 0.1f)) Record(p, inside);
            },
        });
        Assert.That(run.Passed, run.ToString);

        var towers = new Dictionary<PartyRole, float> { [northWest] = -45f, [southWest] = -135f, [northEast] = 45f, [southEast] = 135f };
        Assert.Multiple(() =>
        {
            foreach (var (role, bearing) in towers)
            {
                var tower = Tower(bearing);
                Assert.That(Vector2.Distance(inside[role], tower), Is.LessThan(TowerRadius), $"{role} is not in the {bearing}° tower");
                Assert.That(Vector2.Distance(beside[role], tower), Is.GreaterThan(TowerRadius), $"{role} stood in the {bearing}° tower before the second Hypernova");
                Assert.That(Vector2.Distance(beside[role], tower), Is.LessThan(6f), $"{role} is not beside the {bearing}° tower");
            }
        });
    }

    private static Vector2 Tower(float bearing)
    {
        var at = UcobP3BlackfireScenario.AroundNael(Direction.N, bearing, UcobP3BlackfireScenario.TowerDistance);
        return new Vector2(at.X, at.Z);
    }

    private static void Record(ScenarioProbe probe, Dictionary<PartyRole, Vector2> into)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (probe.Member(role) is { } member)
                into[role] = new Vector2(member.Position.X, member.Position.Z);
    }
}
