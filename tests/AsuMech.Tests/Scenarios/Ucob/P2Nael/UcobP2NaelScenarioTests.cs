using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Ucob.P2Nael;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Nael lands on the main tank at (0, 9).
public class UcobP2NaelScenarioTests
{
    // The clear's push: after Ravensbeak, Nael stops being a target and the dragons leave.
    [Test]
    public void NaelIsPushedAndTheDragonsLeaveAfterRavensbeak()
    {
        bool? targetableBefore = null, targetableAfter = null;
        int? dragonsBefore = null, dragonsAfter = null;
        var dragonIds = new[] { BNpcBaseId.Firehorn, BNpcBaseId.Iceclaw, BNpcBaseId.Thunderwing, BNpcBaseId.TailOfDarkness, BNpcBaseId.FangOfLight };
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, 1, new ScenarioRunOptions
        {
            StopAt = UcobP2NaelScenario.End - 0.1f,
            Probe = p =>
            {
                SimEnemy? Nael() => p.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus);
                int Dragons() => p.World.Children.OfType<SimEnemy>().Count(e => e.IsActive && dragonIds.Contains(e.BNpcBaseId));
                if (p.Crossed(UcobP2NaelScenario.NaelUntargetable - 0.5f)) { targetableBefore = Nael()?.Targetable; dragonsBefore = Dragons(); }
                if (p.Crossed(UcobP2NaelScenario.DragonsLeave + 0.1f)) { targetableAfter = Nael()?.Targetable; dragonsAfter = Dragons(); }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.Multiple(() =>
        {
            Assert.That(targetableBefore, Is.True);
            Assert.That(dragonsBefore, Is.EqualTo(5));
            Assert.That(targetableAfter, Is.False);
            Assert.That(dragonsAfter, Is.EqualTo(0));
        });
    }

    [Test]
    public void StandingInAThermionicBurstSliceKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .TeleportAt(15f, to: new Vector2(0f, 12f))
            .ShouldKill(ActionId.ThermionicBurst, CasterDps);

    [Test]
    public void TwoMeteorStreamsOnOneSpotKillBoth()
        => Negative<UcobP2NaelScenario>(MeleeDpsA)
            .Overrides<UcobP2NaelStateOverrides>(o => o.MeteorOrder = [MainTank, MeleeDpsA, RegenHealer, ShieldHealer, OffTank, MeleeDpsB, PhysRangedDps, CasterDps])
            .TeleportAt(13.4f, to: new Vector2(4.6f, 11.1f))
            .ShouldKill(ActionId.MeteorStream, MeleeDpsA, MainTank);

    [Test]
    public void StandingBesideTheTankForDalamudDiveKills()
        => Negative<UcobP2NaelScenario>(MeleeDpsA)
            .TeleportAt(19.7f, to: new Vector2(0f, 10.5f))
            .ShouldKill(ActionId.DalamudDive, MeleeDpsA);

    [Test]
    public void StandingNextToAThunderstruckPlayerKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.ThunderTargets = [[MeleeDpsA, MeleeDpsB], [MeleeDpsA, MeleeDpsB], [RegenHealer, ShieldHealer], [RegenHealer, ShieldHealer]])
            .TeleportAt(44f, to: new Vector2(-5.2f, 9f))
            .ShouldKill(ActionId.ChainLightningSplash, CasterDps);

    [Test]
    public void StayingAwayForLunarDynamoKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.Quotes[0] = 0)
            .TeleportAt(43.5f, to: new Vector2(0f, -5f))
            .ShouldKill(ActionId.LunarDynamo, CasterDps);

    [Test]
    public void StayingInForIronChariotKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.Quotes[0] = 1)
            .TeleportAt(47f, to: new Vector2(0f, 6f))
            .ShouldKill(ActionId.IronChariot, CasterDps);

    [Test]
    public void AnUncleansedDoomKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.DoomTargets = [[CasterDps, MeleeDpsA], [RegenHealer, ShieldHealer, MeleeDpsB], [OffTank, MainTank, PhysRangedDps]])
            .TeleportAt(50f, to: new Vector2(-10f, -5f))
            .ShouldKill(ActionId.Deathstorm, CasterDps);

    [Test]
    public void TheBotsClearWhenTheSameTwoTakeEveryFire()
        => Negative<UcobP2NaelScenario>(RegenHealer)
            .Overrides<UcobP2NaelStateOverrides>(o => o.FireTargets = [MainTank, MainTank, OffTank, OffTank])
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearEveryQuote([Values(0, 1)] int first, [Values(0, 1)] int second, [Values(0, 1)] int third)
        => Negative<UcobP2NaelScenario>(PhysRangedDps)
            .Overrides<UcobP2NaelStateOverrides>(o =>
            {
                o.Quotes[0] = first;
                o.Quotes[1] = second;
                o.Quotes[2] = third;
            })
            .ShouldKillNoOneElse();
}
