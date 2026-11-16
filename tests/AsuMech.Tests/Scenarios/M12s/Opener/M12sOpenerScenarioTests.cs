using System.Numerics;
using AsuMech.Scenarios.M12s.Opener;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.Opener.M12sOpenerScenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sOpenerScenarioTests
{
    // Supports east: H1 and M1 take the first pair, H2 and M2 the second, R1 and R2 both western
    // greens of the third, the tanks both purples of the fourth.
    private static void Pinned(M12sOpenerStateOverrides o)
    {
        o.PurpleEast = true;
        o.Orbs =
        [
            [new MortalOrb(true, false), new MortalOrb(false, false)],
            [new MortalOrb(true, false), new MortalOrb(false, false)],
            [new MortalOrb(false, false), new MortalOrb(false, false)],
            [new MortalOrb(true, true), new MortalOrb(true, true)],
        ];
    }

    [Test]
    public void BaitingAnOrbWhilePoisonedKills()
        => Negative<Scenario>(MeleeDpsB)
            .Overrides<M12sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.OrbsHitAt[0] - 0.5f, to: new Vector2(-4.5f, -11.6f))
            .TeleportAt(Scenario.OrbsHitAt[1] - 0.5f, to: new Vector2(-4f, -11f))
            .ShouldKill(ActionId.MortalSlayerGreen, MeleeDpsB);

    [Test]
    public void AHealerCuttingInFrontOfThePurplesDiesWithTheTankBesideHer()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M12sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.OrbsHitAt[3] - 0.5f, to: new Vector2(3.5f, -11.6f))
            .ShouldKill(ActionId.MortalSlayerPurple, RegenHealer, MainTank);

    [Test]
    public void TanksBaitingThePurplesSideBySideBothDie()
        => Negative<Scenario>(OffTank)
            .Overrides<M12sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.OrbsHitAt[3] - 0.5f, to: new Vector2(5f, -11f))
            .ShouldKill(ActionId.MortalSlayerPurple, MainTank, OffTank);
}
