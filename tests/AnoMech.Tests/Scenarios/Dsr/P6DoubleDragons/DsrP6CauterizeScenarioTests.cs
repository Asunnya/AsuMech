using System.Linq;
using System.Numerics;
using AnoMech.Scenarios.Dsr.P6DoubleDragons;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Second half, pinned like the first. Fire on H1, H2 and the caster for the second Wyrmsbreath; north
// wing with the head up and Hot Wing, so the party waits east in the middle strip; Nidhogg dives the
// west half of the north wall. The Vow started on the ranged, so M1 takes the third pass.
public class DsrP6CauterizeScenarioTests
{
    private static void Pinned(DsrP6DoubleDragonsStateOverrides o) => DsrP6DoubleDragonsScenarioTests.Pinned(o);

    [Test]
    public void NobodyTakingTheThirdPassWipes()
        => Negative<DsrP6CauterizeScenario>(MeleeDpsA)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .At(99.5f, p =>
            {
                Assert.That(p.Member(OffTank)!.HasStatus(StatusId.MortalVow));
                Assert.That(p.Member(MainTank)!.HasStatus(StatusId.MortalAtonement));
                Assert.That(p.Member(PhysRangedDps)!.HasStatus(StatusId.MortalAtonement));
            })
            .TeleportAt(125f, to: new Vector2(10f, 10f))
            .ShouldKill(ActionId.MortalVowPass, AllBut());

    [Test]
    public void StandingUnderTheSecondWingKills()
        => Negative<DsrP6CauterizeScenario>(CasterDps)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .TeleportAt(121f, to: new Vector2(19f, -2f))
            .ShouldKill(ActionId.HallowedWingsNorth, CasterDps);

    [Test]
    public void StandingFarWhenTheHeadIsUpKills()
        => Negative<DsrP6CauterizeScenario>(MeleeDpsB)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .TeleportAt(121.5f, to: new Vector2(-10f, 2f))
            .ShouldKill(ActionId.HallowedPlume, MeleeDpsB, MainTank);

    [Test]
    public void StaticSpotsTakeExactlyOneBreathEach()
        => Negative<DsrP6CauterizeScenario>(MeleeDpsB)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .At(143f, p =>
            {
                var roles = new[] { RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps };
                Assert.That(roles.Count(r => p.Member(r)!.HasStatus(StatusId.Boiling)), Is.EqualTo(3));
                Assert.That(roles.Count(r => p.Member(r)!.HasStatus(StatusId.Freezing)), Is.EqualTo(3));
            })
            .TeleportAt(160f, to: new Vector2(0f, -5f))
            .ShouldKill(ActionId.TouchdownHit, MeleeDpsB);

    [Test]
    public void SharingAnIceBreathKillsBoth()
        => Negative<DsrP6CauterizeScenario>(MeleeDpsB)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .TeleportAt(140f, to: new Vector2(2.6f, -9.3f))
            .ShouldKill(ActionId.IceBreath, MeleeDpsB, MeleeDpsA);

    [Test]
    public void DeepFreezeInHraesvelgrsDiveKills()
        => Negative<DsrP6CauterizeScenario>(MeleeDpsA)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .TeleportAt(152f, to: new Vector2(6.5f, -8f))
            .ShouldKill(ActionId.CauterizeHraesvelgr, MeleeDpsA);

    [Test]
    public void StandingInFrontOfTheTankKills()
        => Negative<DsrP6CauterizeScenario>(ShieldHealer)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .TeleportAt(152f, to: new Vector2(9f, -21.5f))
            .ShouldKill(ActionId.CauterizeHraesvelgr, ShieldHealer);

    [Test]
    public void MovingWithPyreticKills()
        => Negative<DsrP6CauterizeScenario>(ShieldHealer)
            .Overrides<DsrP6DoubleDragonsStateOverrides>(Pinned)
            .TeleportAt(153.6f, to: new Vector2(9f, -8f))
            .ShouldKill(ActionId.CauterizeHraesvelgr, ShieldHealer);
}
