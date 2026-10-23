using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M11s.ArenaSplit;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;

namespace AnoMech.Tests;

public class M11sBossTankingTests
{
    private static SimEnemy Tyrant(ScenarioProbe p)
        => p.World.Children.OfType<SimEnemy>().First(e => e.SpawnConfig.BNpcBaseId == BNpcBaseId.Tyrant);

    [Test]
    public void TheTyrantFollowsTheMainTankBetweenMechanics()
    {
        var distance = float.NaN;
        var run = ScenarioRun.Execute(typeof(M11sOpenerScenario), 0, 1, new ScenarioRunOptions
        {
            PlayerRole = MainTank,
            StopAt = 15f,
            Takeovers = [new Takeover(5f, new Vector2(0f, -16f))],
            Probe = p =>
            {
                if (!p.Crossed(14.5f)) return;
                var boss = Tyrant(p).Position;
                var tank = p.Member(MainTank)!.Position;
                distance = Vector2.Distance(new Vector2(boss.X, boss.Z), new Vector2(tank.X, tank.Z));
            },
        });
        Assert.That(run.Failure, Is.Null);
        Assert.That(distance, Is.LessThan(6.5f));
    }

    [Test]
    public void TheTyrantsHitboxDoublesWhileTheMiddleIsGone()
    {
        float before = 0f, during = 0f, after = 0f;
        ScenarioRun.Execute(typeof(M11sArenaSplitScenario), 0, 1, new ScenarioRunOptions
        {
            Probe = p =>
            {
                if (p.Crossed(M11sArenaSplitScenario.BossGrowsAt - 0.5f)) before = Tyrant(p).HitboxRadius;
                if (p.Crossed(M11sArenaSplitScenario.BossGrowsAt + 0.5f)) during = Tyrant(p).HitboxRadius;
                if (p.Crossed(M11sArenaSplitScenario.BossShrinksAt + 0.5f)) after = Tyrant(p).HitboxRadius;
            },
        });
        Assert.That(before, Is.EqualTo(5f).Within(0.01f));
        Assert.That(during, Is.EqualTo(10f).Within(0.01f));
        Assert.That(after, Is.EqualTo(5f).Within(0.01f));
    }
}
