using System.Numerics;
using AsuMech.Scenarios.M12s.ReplicationOne;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.ReplicationOne.M12sReplicationOneScenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sReplicationOneScenarioTests
{
    // Fire south, dark north (on the north pairs), cleaves north-south, outer fire south-east, follow-up kick on the same side.
    private static void Pinned(M12sReplicationOneStateOverrides o)
    {
        o.FireCardinal = 2;
        o.VerticalCleaves = true;
        o.OuterFireIntercard = 1;
        o.SnakingKickEast = true;
        o.SobatTurns = 0;
    }

    [Test]
    public void TheStratDodgesEveryConeAndBaitsTheOtherElementInEveryLayout()
    {
        for (var fire = 0; fire < 4; fire++)
        foreach (var vertical in new[] { true, false })
        for (var outer = 0; outer < 4; outer++)
        {
            var layout = $"fire {fire}, vertical {vertical}, outer fire {outer}";
            var checks = 0;
            var run = ScenarioRun.Execute(typeof(Scenario), 0, fire * 8 + outer, new ScenarioRunOptions
            {
                Overrides = o =>
                {
                    var overrides = (M12sReplicationOneStateOverrides)o;
                    overrides.FireCardinal = fire;
                    overrides.VerticalCleaves = vertical;
                    overrides.OuterFireIntercard = outer;
                },
                Probe = p =>
                {
                    if (p.Crossed(Scenario.FirstDarkAt + 0.5f))
                    {
                        var members = p.World.Party.ActiveMembers().ToList();
                        Assert.That(members.Count(m => m.HasStatus(StatusId.DarkResistanceDownII)), Is.EqualTo(4), layout);
                        Assert.That(members.Count(m => m.HasStatus(StatusId.FireResistanceDownII)), Is.EqualTo(2), layout);
                        checks++;
                    }
                    if (p.Crossed(Scenario.SecondDarkAt + 0.5f))
                    {
                        var members = p.World.Party.ActiveMembers().ToList();
                        Assert.That(members.Count(m => m.HasStatus(StatusId.DarkResistanceDownII)), Is.EqualTo(8), layout);
                        Assert.That(members.Count(m => m.HasStatus(StatusId.FireResistanceDownII)), Is.EqualTo(6), layout);
                        checks++;
                    }
                    if (p.Crossed(Scenario.SnakingKickAt + 1f) || p.Crossed(Scenario.SecondDarkAt + 1f) || p.Crossed(Scenario.SobatRepeatAt + 1f))
                    {
                        Assert.That(p.World.Party.ActiveMembers().Where(m => m.HasStatus(StatusId.DamageDown)), Is.Empty, layout);
                        checks++;
                    }
                },
            });
            Assert.That(run.Passed, $"{layout}: {run}");
            Assert.That(checks, Is.EqualTo(5), layout);
        }
    }

    [Test]
    public void TheFollowUpKickNeverCatchesTheParty()
    {
        for (var turns = 0; turns < 4; turns++)
        {
            var checkedIt = false;
            var run = ScenarioRun.Execute(typeof(Scenario), 0, turns, new ScenarioRunOptions
            {
                Overrides = o => ((M12sReplicationOneStateOverrides)o).SobatTurns = turns,
                Probe = p =>
                {
                    if (!p.Crossed(Scenario.SobatRepeatAt + 1f)) return;
                    Assert.That(p.World.Party.ActiveMembers().Where(m => m.HasStatus(StatusId.DamageDown)), Is.Empty, $"turns {turns}");
                    checkedIt = true;
                },
            });
            Assert.That(run.Passed, $"turns {turns}: {run}");
            Assert.That(checkedIt, Is.True);
        }
    }

    [Test]
    public void FireBaitedAloneKills()
        => Negative<Scenario>(ShieldHealer)
            .Overrides<M12sReplicationOneStateOverrides>(Pinned)
            .TeleportAt(Scenario.FirstFireAt - 1f, to: new Vector2(0f, 8f))
            .ShouldKill(ActionId.TopTierSlam, ShieldHealer);

    [Test]
    public void BaitingDarkAgainWithADarkDebuffKillsTheBaiterAndTheCentrePartner()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M12sReplicationOneStateOverrides>(Pinned)
            .TeleportAt(Scenario.SecondDarkAt - 2f, to: new Vector2(0.5f, 4f))
            .ShouldKill(ActionId.MightyMagic, MeleeDpsA, MainTank);

    [Test]
    public void StandingInFrontOfTheBossForDoubleSobatKills()
        => Negative<Scenario>(CasterDps)
            .Overrides<M12sReplicationOneStateOverrides>(Pinned)
            .TeleportAt(Scenario.SobatKickAt - 1f, to: new Vector2(3f, -6f))
            .ShouldKill(ActionId.DoubleSobatBuster, CasterDps);

    [Test]
    public void TheMarkedTankNeedsAnInvulnForDoubleSobat()
        => Negative<Scenario>(MainTank)
            .Overrides<M12sReplicationOneStateOverrides>(Pinned)
            .WithMitigationChecks()
            .TeleportAt(Scenario.SobatKickAt - 1f, to: new Vector2(0f, -4f))
            .ShouldKill(ActionId.DoubleSobatBuster, MainTank);

    [Test]
    public void ATankWhoStaysWithThePartyKillsItWithEsotericFinisher()
        => Negative<Scenario>(OffTank)
            .Overrides<M12sReplicationOneStateOverrides>(Pinned)
            .TeleportAt(Scenario.SobatRepeatAt + 0.5f, to: new Vector2(0f, 10f))
            .ShouldKill(ActionId.EsotericFinisher, AllBut(MainTank, OffTank));

    [Test]
    public void WalkingOffTheRoundArenaKills()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M12sReplicationOneStateOverrides>(Pinned)
            .TeleportAt(5f, to: new Vector2(15f, 15f))
            .ShouldKill(TheEnvironment, PhysRangedDps);
}
