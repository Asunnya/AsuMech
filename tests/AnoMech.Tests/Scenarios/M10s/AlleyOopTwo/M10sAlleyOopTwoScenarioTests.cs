using System.Numerics;
using AnoMech.Scenarios.M10s.AlleyOopTwo;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.AlleyOopTwo.M10sAlleyOopTwoScenario;

namespace AnoMech.Tests;

public class M10sAlleyOopTwoScenarioTests
{
    [Test]
    public void StayingOnYourSpreadBurns()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M10sAlleyOopTwoStateOverrides>(o => o.DoubleDip = false)
            .FreezeAt(Scenario.SecondConesAt + 0.1f)
            .ShouldKill(ActionId.AlleyOopInferno, PhysRangedDps);

    [Test]
    public void ThePuddlesSteamBurstAfterTheFire()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M10sAlleyOopTwoStateOverrides>(o => o.DoubleDip = false)
            .TeleportAt(Scenario.SteamHitAt - 1.5f, to: new Vector2(-6.5f, -6.5f))
            .ShouldKill(ActionId.SteamBurst, PhysRangedDps);

    [Test]
    public void TheFurthestPlayerTakesDeepImpact()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sAlleyOopTwoStateOverrides>(o => o.DoubleDip = false)
            .TeleportAt(Scenario.DeepImpactHitAt - 0.5f, to: new Vector2(-15f, 15f))
            .ShouldKill(ActionId.DeepImpactHit, CasterDps);

    [Test]
    public void BothConeSetsMissTheStratForEitherAlleyOop()
    {
        foreach (var doubleDip in new[] { true, false })
        {
            var checkedIt = false;
            var run = ScenarioRun.Execute(typeof(Scenario), 0, 9, new ScenarioRunOptions
            {
                Overrides = o => ((M10sAlleyOopTwoStateOverrides)o).DoubleDip = doubleDip,
                Probe = p =>
                {
                    if (!p.Crossed(Scenario.SecondConesAt + 1f)) return;
                    Assert.That(p.World.Party.ActiveMembers().Where(m => m.HasStatus(StatusId.DamageDown)), Is.Empty, $"double-dip {doubleDip}");
                    checkedIt = true;
                },
            });
            Assert.That(run.Passed, run.ToString());
            Assert.That(checkedIt, Is.True);
        }
    }
}
