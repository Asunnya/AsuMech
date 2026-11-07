using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Scenarios.Ucob.P3Fellruin;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Bahamut Prime lands west-northwest (-8.66, -5), Nael east-northeast (8.66, -5).
public class UcobP3FellruinScenarioTests
{
    private static void BahamutWestNael(UcobP3FellruinStateOverrides o)
    {
        o.BahamutSlot = 2;
        o.NaelClockwiseOfBahamut = true;
    }

    [Test]
    public void StayingAwayFromNaelForLunarDynamoKills()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                BahamutWestNael(o);
                o.Quote = 0;
            })
            .TeleportAt(17f, to: new Vector2(10f, 5f))
            .ShouldKill(ActionId.LunarDynamo, CasterDps);

    [Test]
    public void StandingNextToATankAsTempestWingGoesOffKills()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(BahamutWestNael)
            .TeleportAt(22.9f, to: new Vector2(2.84f, -8.4f))
            .ShouldKill(ActionId.TempestWingSplash, CasterDps);

    [Test]
    public void LeavingTheNeurolinkForAethericProfusionKills()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(BahamutWestNael)
            .TeleportAt(23.6f, to: new Vector2(0f, 3f))
            .ShouldKill(ActionId.AethericProfusion, CasterDps);

    [Test]
    public void SharingAMeteorStreamKillsBoth()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                BahamutWestNael(o);
                o.MeteorTargets = [CasterDps, PhysRangedDps, MainTank, OffTank];
            })
            .TeleportAt(26f, to: new Vector2(0.84f, -8.4f))
            .ShouldKill(ActionId.MeteorStream, CasterDps, MainTank);

    [Test]
    public void TheBotsClearEveryQuoteAndLayout([Values(0, 1)] int quote, [Values(0, 1, 2)] int slot, [Values] bool naelClockwise)
        => Negative<UcobP3FellruinScenario>(PhysRangedDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                o.Quote = quote;
                o.BahamutSlot = slot;
                o.NaelClockwiseOfBahamut = naelClockwise;
            })
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWithTheDiveOnATank([Values(0, 1)] int quote, [Values(MainTank, OffTank)] PartyRole diver)
        => Negative<UcobP3FellruinScenario>(PhysRangedDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                o.Quote = quote;
                o.RavenDiveTarget = diver;
            })
            .ShouldKillNoOneElse();

    [Test]
    public void NaelSaysTheQuoteOverHerHead([Values(0, 1)] int quote)
    {
        var rows = Run(o => o.Quote = quote);
        var nael = SpawnedIds(rows, BNpcBaseId.NaelDeusDarnus).Single();
        var yell = rows.Single(r => r["kind"] == "yell");
        Assert.Multiple(() =>
        {
            Assert.That(yell["actor"], Is.EqualTo(nael));
            Assert.That(yell["detail"], Does.Contain(UcobP3FellruinState.QuoteChoices[quote].Text));
        });
    }

    [Test]
    public void EachMeteorStreamIsANaelGeminusStrikingItsTarget()
    {
        PartyRole[] targets = [CasterDps, PhysRangedDps, MainTank, OffTank];
        var rows = Run(o => o.MeteorTargets = targets);
        var gemini = SpawnedIds(rows, BNpcBaseId.NaelGeminus);
        var strikes = rows.Where(r => r["kind"] == "ability" && r["id"] == ActionId.MeteorStream.ToString()).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(gemini, Has.Count.EqualTo(UcobP3FellruinState.MeteorStreams));
            Assert.That(strikes.Select(r => r["actor"]), Is.EquivalentTo(gemini));
            Assert.That(strikes.Select(r => r["detail"]), Has.All.Contains("hits=1"));
        });
    }

    private static IReadOnlyList<Dictionary<string, string>> Run(System.Action<UcobP3FellruinStateOverrides> set)
    {
        var run = ScenarioRun.Execute(typeof(UcobP3FellruinScenario), 0, 1, new ScenarioRunOptions
        {
            PlayerRole = PhysRangedDps,
            Overrides = o => set((UcobP3FellruinStateOverrides)o),
        });
        Assert.That(run.Passed, run.ToString);
        return ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows;
    }

    private static List<string> SpawnedIds(IReadOnlyList<Dictionary<string, string>> rows, uint baseId)
        => rows.Where(r => r["kind"] == "spawn" && r["id"] == baseId.ToString()).Select(r => r["actor"]).ToList();
}
