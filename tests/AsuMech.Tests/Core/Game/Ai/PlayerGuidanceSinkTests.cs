using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AsuMech.Tests.Core.Game.Ai;

public class PlayerGuidanceSinkTests
{
    private sealed class Recorder : IPlayerGuidanceSink
    {
        public readonly List<GuidanceOrder> Orders = [];
        public void OnMove(GuidanceOrder order) => Orders.Add(order);
    }

    private static Recorder RunGaruda(PartyRole role, float stopAt)
    {
        var recorder = new Recorder();
        var run = ScenarioRun.Execute(typeof(UwuP1GarudaScenario), 0, seed: 7, new ScenarioRunOptions
        {
            PlayerRole = role,
            StopAt = stopAt,
            Probe = probe => probe.World.Guidance ??= recorder,
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        return recorder;
    }

    [Test]
    public void EveryMoveToThePlayersSlotReachesTheSinkWithItsKey()
    {
        var orders = RunGaruda(PartyRole.MainTank, stopAt: 12f).Orders;
        Assert.That(orders.Select(o => o.Key.Time), Is.EqualTo(new[] { 0.5f, 5.4f, 11.2f }));
        Assert.That(orders, Has.All.Matches<GuidanceOrder>(o => o.Role == PartyRole.MainTank && o.Key.StratClass == nameof(UwuP1GarudaAi)));
        var drag = orders[2];
        Assert.That(drag.Key.Method, Is.EqualTo("DragGarudaWestPartyBehind"));
        Assert.That(drag.Key.Source, Does.Match(@"^UwuP1GarudaAi\.cs:\d+$"));
        Assert.That(orders[0].Key.Method, Is.Null, "a lambda carries no method name");
        Assert.That(drag.IssuedAt, Is.EqualTo(11.2f).Within(0.05f));
    }

    [Test]
    public void TargetIsTheExactSpotWithoutJitter()
    {
        var opener = RunGaruda(PartyRole.MainTank, stopAt: 2f).Orders[0];
        Assert.That(new Vector2(opener.Target.X, opener.Target.Z), Is.EqualTo(new Vector2(-1f, -3.4f)));
    }

    [Test]
    public void OnlyThePlayersSlotIsReported()
    {
        var orders = RunGaruda(PartyRole.CasterDps, stopAt: 2f).Orders;
        Assert.That(orders, Has.Count.EqualTo(1));
        Assert.That(orders[0].Role, Is.EqualTo(PartyRole.CasterDps));
    }
}
