using System.Linq;
using System.Numerics;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Fru.P4Materialization;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

public class FruP4MaterializationScenarioTests
{
    [Test]
    public void TheUsurperComesBackSouthAndTheFragmentAppearsNorthAfterMaterialization()
        => Negative<FruP4MaterializationScenario>(MainTank)
            .At(1f, p =>
            {
                var usurper = Enemy(p, BNpcBaseId.UsurperP4)!;
                Assert.That(Vector3.Distance(usurper.Position, new Vector3(0, 0, 10)), Is.LessThan(0.01f));
                Assert.That(usurper.Targetable, Is.False);
                Assert.That(Enemy(p, BNpcBaseId.FragmentOfFate), Is.Null);
            })
            .At(5f, p => Assert.That(Enemy(p, BNpcBaseId.UsurperP4)!.Targetable))
            .At(16.5f, p => Assert.That(Vector3.Distance(Enemy(p, BNpcBaseId.FragmentOfFate)!.Position, new Vector3(0, 0, -16)), Is.LessThan(0.01f)))
            .ShouldKillNoOneElse();

    private static SimEnemy? Enemy(ScenarioProbe p, uint bnpcBaseId)
        => p.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.BNpcBaseId == bnpcBaseId);
}
