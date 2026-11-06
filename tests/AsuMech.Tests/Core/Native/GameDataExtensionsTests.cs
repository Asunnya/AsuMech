using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Tests;

public class GameDataExtensionsTests
{
    [TestCase(1122u, true, TestName = "TOP is ultimate")]
    [TestCase(733u, true, TestName = "UCOB is ultimate")]
    [TestCase(1232u, true, TestName = "M4S is savage")]
    [TestCase(1263u, true, TestName = "M8S is savage")]
    [TestCase(1327u, true, TestName = "M12S is savage")]
    [TestCase(1362u, true, TestName = "an extreme is")]
    [TestCase(1372u, false, TestName = "an unreal is not")]
    [TestCase(1231u, false, TestName = "a normal raid is not")]
    [TestCase(179u, false, TestName = "an inn is not")]
    [TestCase(132u, false, TestName = "a city is not")]
    public void RecordableDutyFollowsTheContentFinderCondition(uint territory, bool expected)
    {
        FakeGame.Install();
        Assert.That(Natives.Data.IsRecordableDuty(territory), Is.EqualTo(expected));
    }
}
