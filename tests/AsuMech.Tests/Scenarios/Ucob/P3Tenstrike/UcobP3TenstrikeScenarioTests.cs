using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Ucob.P3Tenstrike;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over. With
// these Hatch targets the caster takes Neurolink 1, north.
public class UcobP3TenstrikeScenarioTests
{
    private static void HealersAndCasterHatch(UcobP3TenstrikeStateOverrides o) => o.HatchTargets = [RegenHealer, ShieldHealer, CasterDps];

    private static void TanksAndHealersShakeFirst(UcobP3TenstrikeStateOverrides o) => o.FirstEarthShakers = [MainTank, OffTank, RegenHealer, ShieldHealer];

    [Test]
    public void AnOviformReachingSomeoneOutsideANeurolinkWipes()
        => Negative<UcobP3TenstrikeScenario>(CasterDps)
            .Overrides<UcobP3TenstrikeStateOverrides>(HealersAndCasterHatch)
            .TeleportAt(15f, to: new Vector2(0f, -4.5f))
            .ShouldKill(ActionId.DeepHatch, AllBut());

    [Test]
    public void StayingInTheNeurolinkForTheSecondHatchKills()
        => Negative<UcobP3TenstrikeScenario>(CasterDps)
            .Overrides<UcobP3TenstrikeStateOverrides>(o =>
            {
                HealersAndCasterHatch(o);
                o.MeteorOrder = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps, RegenHealer, ShieldHealer, OffTank, MainTank];
            })
            .FreezeAt(19.5f)
            .ShouldKill(ActionId.Hatch, CasterDps)
            .ShouldKillNoOneElse();

    [Test]
    public void StandingOnSomeoneElsesMeteorStreamKillsThem()
        => Negative<UcobP3TenstrikeScenario>(PhysRangedDps)
            .Overrides<UcobP3TenstrikeStateOverrides>(o =>
            {
                HealersAndCasterHatch(o);
                o.MeteorOrder = [PhysRangedDps, MeleeDpsA, MeleeDpsB, CasterDps, RegenHealer, ShieldHealer, OffTank, MainTank];
            })
            .TeleportAt(13.5f, to: new Vector2(-5f, -14f))
            .MoveBotAt(13.5f, MeleeDpsA, to: new Vector2(-3.5f, -14f))
            .ShouldKill(ActionId.MeteorStream, MeleeDpsA);

    [Test]
    public void EachMeteorStreamComesFromANaelGeminusOnItsTarget()
    {
        var streams = new List<(PartyRole Target, uint Caster, float Distance)>();
        PartyRole[] order = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps, RegenHealer, ShieldHealer, OffTank, MainTank];
        Negative<UcobP3TenstrikeScenario>(CasterDps)
            .Overrides<UcobP3TenstrikeStateOverrides>(o =>
            {
                HealersAndCasterHatch(o);
                o.MeteorOrder = order;
            })
            .At(0.1f, p => p.World.ActionResolved += r =>
            {
                if (r.Action.ActionId != ActionId.MeteorStream) return;
                foreach (var hit in r.Hits.OfType<ISimPartyMember>())
                    streams.Add((hit.Role, (r.Caster as SimEnemy)?.SpawnConfig.BNpcBaseId ?? 0,
                        Vector2.Distance(new(r.Caster.Position.X, r.Caster.Position.Z), new(((SimCharacter)hit).Position.X, ((SimCharacter)hit).Position.Z))));
            })
            .ShouldKillNoOneElse();
        Assert.That(streams.Select(s => s.Target), Is.EqualTo(order));
        Assert.That(streams.Select(s => s.Caster), Is.All.EqualTo(BNpcBaseId.NaelGeminus));
        // A sprinting target gets this far from where the clone landed.
        Assert.That(streams.Select(s => s.Distance), Is.All.LessThanOrEqualTo(AsuMech.Core.Game.Ai.AiManager.SprintSpeed * UcobP3TenstrikeScenario.GeminusArrives + 0.1f));
    }

    [Test]
    public void ASecondEarthShakerKills()
        => Negative<UcobP3TenstrikeScenario>(RegenHealer)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .TeleportAt(32f, to: new Vector2(0f, 4f))
            .ShouldKill(ActionId.EarthShakerCone, RegenHealer)
            .ShouldKillNoOneElse();

    // The bots read where a shaker stands and fan out around them, so any spread spot works.
    [Test]
    public void AFirstShakerAnywhereOnTheRimTakesOnlyTheirOwnCone([Values(90f, 120f, 150f, 180f, 210f, 240f, 270f)] float bearing)
    {
        var hits = CountConeHits(out var probe);
        Negative<UcobP3TenstrikeScenario>(RegenHealer)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .At(0.1f, probe)
            .TeleportAt(26.5f, to: AtBearing(bearing, 13f))
            .TeleportAt(31f, to: new Vector2(0f, -8f))
            .ShouldKillNoOneElse();
        AssertOneConeEach(hits);
    }

    [Test]
    public void ASecondShakerAnywhereOnTheRimTakesOnlyTheirOwnCone([Values(80f, 120f, 180f, 240f, 280f)] float bearing)
    {
        var hits = CountConeHits(out var probe);
        Negative<UcobP3TenstrikeScenario>(MeleeDpsA)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .At(0.1f, probe)
            .TeleportAt(31.5f, to: AtBearing(bearing, 8.5f))
            .TeleportAt(36f, to: new Vector2(0f, -8f))
            .ShouldKillNoOneElse();
        AssertOneConeEach(hits);
    }

    [Test]
    public void AShakerWhoStaysOutForTheSecondSetIsLeftClear()
        => Negative<UcobP3TenstrikeScenario>(RegenHealer)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .TeleportAt(26.5f, to: AtBearing(180f, 13f))
            .TeleportAt(31f, to: AtBearing(180f, 19f))
            .ShouldKillNoOneElse();

    // Too late for the bots to make room: south sits 40 degrees from the shakers at 140 and 220.
    [Test]
    public void SteppingBetweenTwoShakersAtTheLastMomentKillsAllThree()
        => Negative<UcobP3TenstrikeScenario>(RegenHealer)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .TeleportAt(30.2f, to: AtBearing(180f, 13f))
            .ShouldKill(ActionId.EarthShakerCone, RegenHealer, OffTank, ShieldHealer);

    private static Dictionary<(PartyRole, int), int> CountConeHits(out Action<ScenarioProbe> probe)
    {
        var hits = new Dictionary<(PartyRole, int), int>();
        probe = p => p.World.ActionResolved += r =>
        {
            if (r.Action.ActionId != ActionId.EarthShakerCone) return;
            var set = p.Time < UcobP3TenstrikeScenario.EarthShakers[1] - 1f ? 0 : 1;
            foreach (var hit in r.Hits.OfType<ISimPartyMember>())
                hits[(hit.Role, set)] = hits.GetValueOrDefault((hit.Role, set)) + 1;
        };
        return hits;
    }

    private static void AssertOneConeEach(Dictionary<(PartyRole, int), int> hits)
    {
        PartyRole[] first = [MainTank, OffTank, RegenHealer, ShieldHealer];
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var set = first.Contains(role) ? 0 : 1;
            Assert.That(hits.GetValueOrDefault((role, set)), Is.EqualTo(1), $"{role} cones in its own set");
            Assert.That(hits.GetValueOrDefault((role, 1 - set)), Is.EqualTo(0), $"{role} cones in the other set");
        }
    }

    private static Vector2 AtBearing(float degrees, float radius)
    {
        var radians = degrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(radians), -MathF.Cos(radians)) * radius;
    }

    [Test]
    public void StayingOnTheEarthShakerSpotKills()
        => Negative<UcobP3TenstrikeScenario>(RegenHealer)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .FreezeAt(30.3f)
            .ShouldKill(ActionId.EarthShaker, RegenHealer)
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearEveryHatchAndShakerPick([Values(RegenHealer, MeleeDpsA, CasterDps)] PartyRole hatch, [Values(MainTank, PhysRangedDps)] PartyRole firstShaker)
        => Negative<UcobP3TenstrikeScenario>(hatch)
            .Overrides<UcobP3TenstrikeStateOverrides>(o =>
            {
                o.HatchInclude = hatch;
                o.FirstEarthShakerInclude = firstShaker;
            })
            .ShouldKillNoOneElse();
}
