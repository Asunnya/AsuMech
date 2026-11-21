using AsuMech.Core.Native.Implementations.Interop;
using NUnit.Framework;

namespace AsuMech.Tests;

public class FirewallOpcodesTests
{
    [Test]
    public void APublishedFileParses()
    {
        var opcodes = FirewallOpcodes.Parse("ZoneUp=872\r\nZoneDown=169,146\r\n", out var problem);
        Assert.That(problem, Is.Null);
        Assert.That(opcodes!.ZoneUp, Is.EqualTo((ushort)872));
        Assert.That(opcodes.ZoneDown, Is.EqualTo(new uint[] { 169, 146 }));
    }

    [TestCase("ZoneDown=169,146", "no ZoneUp")]
    [TestCase("ZoneUp=872", "no ZoneDown")]
    [TestCase("ZoneUp=0\nZoneDown=169", "ZoneUp is not")]
    [TestCase("ZoneUp=70000\nZoneDown=169", "ZoneUp is not")]
    [TestCase("ZoneUp=872\nZoneDown=169,-1", "not an opcode")]
    [TestCase("ZoneUp=872\nZoneDown=169,0", "not an opcode")]
    [TestCase("ZoneUp=872\nZoneDown=169,abc", "not an opcode")]
    [TestCase("ZoneUp=872\nZoneDown=169,872", "also listed")]
    [TestCase("ZoneUp=872\nZoneDown=1,2,3,4,5,6,7,8,9", "more than")]
    [TestCase("<html>404</html>", "no ZoneUp")]
    public void AnythingOutOfShapeIsRefused(string text, string why)
    {
        Assert.That(FirewallOpcodes.Parse(text, out var problem), Is.Null);
        Assert.That(problem, Does.Contain(why));
    }

    [Test]
    public void ARepeatedZoneDownCountsOnce()
        => Assert.That(FirewallOpcodes.Parse("ZoneUp=872\nZoneDown=169,169,146", out _)!.ZoneDown, Is.EqualTo(new uint[] { 169, 146 }));

    [Test]
    public void CurrentOpcodesThatMatchTheBinaryAllowAStay()
        => Assert.That(FirewallOpcodes.Problem("v2_1", "v2_1", 872, 2, 872), Is.Null);

    [Test]
    public void AListFromAnotherPatchRefusesAStay()
        => Assert.That(FirewallOpcodes.Problem("v2_1", "v1_1", 872, 2, 872), Does.Contain("not available yet"));

    [Test]
    public void AListWithoutTheHeartbeatRefusesAStay()
        => Assert.That(FirewallOpcodes.Problem("v2_1", "v2_1", 0, 2, 872), Does.Contain("not available yet"));

    [Test]
    public void AHeartbeatTheBinaryDisagreesWithRefusesAStay()
        => Assert.That(FirewallOpcodes.Problem("v2_1", "v2_1", 872, 2, 999), Does.Contain("does not match"));

    private static readonly string[] Published = ["2026.10.06.0000.0000.txt", "2026.11.03.0000.0000.txt", "2026.10.16.0000.0000.txt", "README.md"];

    [Test]
    public void APatchWithoutItsOwnFileTakesTheNewestPublishedOne()
        => Assert.That(FirewallOpcodes.LatestPublishedFor(Published, "2026.11.20.0000.0000"), Is.EqualTo("2026.11.03.0000.0000"));

    [Test]
    public void APublishedFileNewerThanTheGameIsNeverTaken()
        => Assert.That(FirewallOpcodes.LatestPublishedFor(Published, "2026.10.20.0000.0000"), Is.EqualTo("2026.10.16.0000.0000"));

    [Test]
    public void AGameOlderThanEveryFileHasNone()
        => Assert.That(FirewallOpcodes.LatestPublishedFor(Published, "2026.01.01.0000.0000"), Is.Null);

    [Test]
    public void TheGamesOwnFileIsNeverTheFallback()
        => Assert.That(FirewallOpcodes.LatestPublishedFor(Published, "2026.11.03.0000.0000"), Is.EqualTo("2026.10.16.0000.0000"));

    [Test]
    public void ARefusedOwnFileKeepsAListAlreadyFetchedForThisPatch()
        => Assert.That(FirewallOpcodes.RefusedOwnFileForgets("v2_1", "v2_1", ""), Is.False);

    [TestCase("v2_1", "2026.10.16.0000.0000")]
    [TestCase("v1_1", "")]
    [TestCase("", "")]
    public void ARefusedOwnFileForgetsAFallbackOrStaleList(string stored, string from)
        => Assert.That(FirewallOpcodes.RefusedOwnFileForgets("v2_1", stored, from), Is.True);
}
