using System.Collections.Generic;
using System.Linq;
using AnoMech.Learn.Mitigation;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Mitigation;

public class MitEvaluatorTests
{
    private const uint Reprisal = 7535, SacredSoil = 188;

    private static MitExpectation Exp(int index, float opens, float closes, string? target = null, uint id = Reprisal, List<string>? forbid = null)
        => new($"m{index}", $"mech {index}", new MitItem(Accept: forbid is null ? [MitCatalog.Find(id)!.Name] : null, Target: target, Forbid: forbid), forbid is null ? [id] : [], opens, closes, target, index);

    private static List<MitVerdict> Run(IReadOnlyList<MitExpectation> exps, params MitUse[] uses)
        => MitEvaluator.Evaluate(exps, uses, id => id == 1 ? "MainTank" : "OffTank", _ => 60f);

    [Test]
    public void OnTimeWhenUsedInsideTheWindow()
        => Assert.That(Run([Exp(0, 20f, 35f)], new MitUse(Reprisal, 0, 30f))[0].Result, Is.EqualTo(MitResult.OnTime));

    [Test]
    public void MissedWhenNothingWasUsed()
        => Assert.That(Run([Exp(0, 20f, 35f)])[0].Result, Is.EqualTo(MitResult.Missed));

    [Test]
    public void LateWhenUsedAfterTheHitBeforeTheNextItem()
    {
        var v = Run([Exp(0, 20f, 35f), Exp(1, 80f, 95f)], new MitUse(Reprisal, 0, 37f));
        Assert.That(v[0].Result, Is.EqualTo(MitResult.Late));
        Assert.That(v[1].Result, Is.EqualTo(MitResult.Missed));
    }

    [Test]
    public void TooEarlyWhenTheEffectExpiresBeforeTheHit()
        => Assert.That(Run([Exp(0, 20f, 35f)], new MitUse(Reprisal, 0, 10f))[0].Result, Is.EqualTo(MitResult.TooEarly));

    [Test]
    public void WrongTargetWhenAimedAtTheOtherTank()
    {
        var v = Run([Exp(0, 20f, 35f, target: "MT", id: SacredSoil)], new MitUse(SacredSoil, 2, 30f));
        Assert.That(v[0].Result, Is.EqualTo(MitResult.WrongTarget));
        Assert.That(Run([Exp(0, 20f, 35f, target: "MT", id: SacredSoil)], new MitUse(SacredSoil, 1, 30f))[0].Result, Is.EqualTo(MitResult.OnTime));
    }

    [Test]
    public void ForbiddenWhenAForbiddenActionLandsInTheWindow()
    {
        var v = Run([Exp(0, 20f, 35f, forbid: ["Reprisal"])], new MitUse(Reprisal, 0, 30f));
        Assert.That(v[0].Result, Is.EqualTo(MitResult.Forbidden));
        Assert.That(Run([Exp(0, 20f, 35f, forbid: ["Reprisal"])])[0].Result, Is.EqualTo(MitResult.OnTime));
    }

    [Test]
    public void CooldownConflictWhenTheButtonWasBurnedOffPlan()
    {
        var v = Run([Exp(0, 100f, 115f)], new MitUse(Reprisal, 0, 60f));
        Assert.That(v[0].Result, Is.EqualTo(MitResult.CooldownConflict));
        Assert.That(v[0].Cause, Does.Contain("60.0s"));
    }

    [Test]
    public void OneUseSatisfiesOnlyOneItem()
    {
        var v = Run([Exp(0, 20f, 35f), Exp(1, 30f, 45f)], new MitUse(Reprisal, 0, 33f));
        Assert.That(v.Select(x => x.Result), Is.EqualTo(new[] { MitResult.OnTime, MitResult.Missed }));
    }

    [Test]
    public void RemindersNeverError()
    {
        var exp = new MitExpectation("m", "m", new MitItem(Reminder: "Heal yourself"), [], 0f, 10f, null, 0);
        Assert.That(Run([exp])[0].IsError, Is.False);
    }
}
