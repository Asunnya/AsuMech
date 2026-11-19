using System.Numerics;
using AsuMech.Scenarios.Fru;

namespace AsuMech.Tests;

public class OracleJumpTests
{
    [Test]
    public void StaysPutForANearTarget()
        => Assert.That(OracleJump.Landing(new Vector3(0, 0, 5), new Vector3(-6.08f, 0, 5.88f)), Is.EqualTo(new Vector3(0, 0, 5)));

    [Test]
    public void LandsShortOfAFarTarget()
    {
        var landing = OracleJump.Landing(new Vector3(0, 0, 5), new Vector3(-12, 0, -5));
        Assert.That(Vector3.Distance(landing, new Vector3(-12, 0, -5)), Is.EqualTo(4.5f).Within(0.01f));
    }
}
