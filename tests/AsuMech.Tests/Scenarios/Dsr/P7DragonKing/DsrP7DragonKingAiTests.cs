using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Dsr.P7DragonKing;
using Ai = AsuMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingAi;
using State = AsuMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingState;

namespace AsuMech.Tests;

public class DsrP7DragonKingAiTests
{
    private const float BotSpread = 0.71f;

    // Every one of the 512 exaflare patterns: the rear-left starts on the first blasts, the rear
    // exaflare's centre on the second step, one step back from the third, and the forward step to
    // the Trinity spots (walked from the back) from the fifth.
    [Test]
    public void TheRearLeftDodgeClearsEveryExaflarePattern()
    {
        var failures = new List<string>();
        for (var pattern = 0; pattern < 512; pattern++)
        {
            int[] headings = [pattern % 8, pattern / 8 % 8, pattern / 64];
            var state = new State(new Rng(0), new DsrP7DragonKingStateOverrides { ExaflareHeadings = [headings, headings, headings] });
            var all = Enum.GetValues<EdgeSpot>().SelectMany(s => state.ExaflareExplosions(0, s)).ToList();
            bool Clear(Vector2 at, Func<Explosion, bool> when)
                => all.Where(when).All(e => Vector2.Distance(at, e.At) > State.ExaflareRadius + BotSpread);
            bool PathClear(Vector2 from, Vector2 to, Func<Explosion, bool> when)
                => Enumerable.Range(0, 21).All(i => Clear(Vector2.Lerp(from, to, i / 20f), when));
            var second = State.ExaflareDelays[1];
            var third = State.ExaflareDelays[2];
            var fifth = State.ExaflareDelays[4];
            var rear = State.Spot(EdgeSpot.Tanks);
            Vector2[] trinitySpots = [Ai.MiddleOfTheDragonKing, Ai.MainTankFrontLeft, Ai.OffTankFrontRight, Ai.BehindOutsideTheDragonKing];
            if (!Clear(Ai.IceExaflareStartInsideRearLeft, e => e.Delay == 0f)
                || !Clear(Ai.FlamesExaflareStartOutsideRearLeft, e => e.Delay == 0f)
                || !Clear(rear, e => e.Delay == second)
                || !Clear(Ai.StepBackFromTheRearExaflare, e => e.Delay >= third)
                || !trinitySpots.All(spot => PathClear(Ai.StepBackFromTheRearExaflare, spot, e => e.Delay >= fifth)))
                failures.Add(string.Join(",", headings));
        }
        Assert.That(failures, Is.Empty);
    }

    [Test]
    public void TheExaflareStartsSitOnTheRightSideOfTheAscalonRing()
    {
        Assert.That(Ai.IceExaflareStartInsideRearLeft.Length() + BotSpread, Is.LessThan(State.SpotRadius));
        Assert.That(Ai.FlamesExaflareStartOutsideRearLeft.Length() - BotSpread, Is.GreaterThan(State.SpotRadius));
    }

    [Test]
    public void OnlyTheTrinityBaitStandsInsideTheDragonKing()
    {
        Assert.That(Ai.BehindOutsideTheDragonKing.Length() - BotSpread, Is.GreaterThan(State.SpotRadius));
        Assert.That(Ai.MainTankFrontLeft.Length(), Is.GreaterThan(State.SpotRadius));
        Assert.That(Ai.OffTankFrontRight.Length(), Is.GreaterThan(State.SpotRadius));
    }
}
