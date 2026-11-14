using System.Buffers.Binary;
using System.Numerics;
using AsuMech.Core.Game.Trace;

namespace AsuMech.Tests.Core.Game.Trace;

public class UnclaimedPacketsTests
{
    private const uint Player = 0x100B0290;
    private const uint Carbuncle = 0x40003F6B;

    private static byte[] EffectResult(uint sequence, uint target)
    {
        var p = new byte[UnclaimedPackets.EffectResultSize];
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x04), sequence);
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x08), target);
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x0C), 17397);
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x10), 19343);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x14), 10000);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x16), 0x2700);
        p[0x19] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x1E), 0x30);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x20), 0x29CE);
        BinaryPrimitives.WriteSingleLittleEndian(p.AsSpan(0x24), 1800f);
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x28), Player);
        return p;
    }

    private static byte[] StatusList()
    {
        var p = new byte[UnclaimedPackets.StatusListSize];
        p[1] = 0x46;
        p[2] = 0x46;
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x04), 14594);
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x08), 16288);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x0C), 10000);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x0E), 10000);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x14), 0x30);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(0x16), 0x29CE);
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x18), 0x455A0E30);
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(0x1C), 0x10094563);
        return p;
    }

    private static string Body(StringWriter text)
    {
        var fields = text.ToString().TrimEnd('\n').Split('|');
        return string.Join("|", fields[2..^1]);
    }

    [Test]
    public void AnOpcodeIsTrustedAfterThreeMatchingPackets()
    {
        var packets = new UnclaimedPackets();
        var seen = new HashSet<uint> { 0x8B6, 0x8B7, 0x8B8 };
        Assert.That(packets.Classify(0x320, EffectResult(0x8B6, Player), Player, seen.Contains, () => (0u, (byte)0)), Is.EqualTo(UnclaimedPackets.Shape.Unknown));
        Assert.That(packets.Classify(0x320, EffectResult(0x8B7, Player), Player, seen.Contains, () => (0u, (byte)0)), Is.EqualTo(UnclaimedPackets.Shape.Unknown));
        Assert.That(packets.Classify(0x320, EffectResult(0x8B8, Player), Player, seen.Contains, () => (0u, (byte)0)), Is.EqualTo(UnclaimedPackets.Shape.EffectResult));
        Assert.That(packets.Classify(0x320, new byte[8], Player, seen.Contains, () => (0u, (byte)0)), Is.EqualTo(UnclaimedPackets.Shape.EffectResult), "learned");
    }

    [Test]
    public void APacketThatReadsAsNothingDoesNotUndoTheVotes()
    {
        var packets = new UnclaimedPackets();
        var seen = new HashSet<uint> { 1, 2, 3 };
        packets.Classify(0x320, EffectResult(1, Player), Player, seen.Contains, () => (0u, (byte)0));
        packets.Classify(0x320, EffectResult(99, Player), Player, seen.Contains, () => (0u, (byte)0));
        packets.Classify(0x320, EffectResult(2, Player), Player, seen.Contains, () => (0u, (byte)0));
        Assert.That(packets.Classify(0x320, EffectResult(3, Player), Player, seen.Contains, () => (0u, (byte)0)), Is.EqualTo(UnclaimedPackets.Shape.EffectResult));
    }

    [Test]
    public void AnOpcodeThatNeverMatchesIsGivenUpOn()
    {
        var packets = new UnclaimedPackets();
        for (var i = 0; i < 64; i++) packets.Classify(0x100, new byte[0x40], Player, _ => false, () => (0u, (byte)0));
        Assert.That(packets.WorthTrying(0x100), Is.False);
        Assert.That(packets.WorthTrying(0x101), Is.True);
    }

    [Test]
    public void TheTargetIsOnlyLookedUpForAPacketLongEnoughForAStatusList()
    {
        var packets = new UnclaimedPackets();
        var lookups = 0;
        packets.Classify(0x320, EffectResult(1, Player), Player, _ => true, () => { lookups++; return (0u, (byte)0); });
        Assert.That(lookups, Is.Zero);
        packets.Classify(0x321, StatusList(), Carbuncle, _ => false, () => { lookups++; return (16288u, (byte)0); });
        Assert.That(lookups, Is.EqualTo(1));
    }

    [Test]
    public void AnEffectResultForSomeoneElseOrAnUnknownSequenceIsNotOne()
    {
        Assert.That(UnclaimedPackets.Guess(EffectResult(5, Player), 0x10000001, _ => true, 0, 0), Is.EqualTo(UnclaimedPackets.Shape.Unknown));
        Assert.That(UnclaimedPackets.Guess(EffectResult(5, Player), Player, _ => false, 0, 0), Is.EqualTo(UnclaimedPackets.Shape.Unknown));
        Assert.That(UnclaimedPackets.Guess(EffectResult(5, Player).AsSpan(0, 0x40), Player, _ => true, 0, 0), Is.EqualTo(UnclaimedPackets.Shape.Unknown), "too short");
    }

    [Test]
    public void AStatusListIsKnownByJobAndMaxHp()
    {
        Assert.That(UnclaimedPackets.Guess(StatusList(), Carbuncle, _ => false, 16288, 0), Is.EqualTo(UnclaimedPackets.Shape.StatusList));
        Assert.That(UnclaimedPackets.Guess(StatusList(), Carbuncle, _ => false, 16000, 0), Is.EqualTo(UnclaimedPackets.Shape.Unknown));
        Assert.That(UnclaimedPackets.Guess(StatusList(), Carbuncle, _ => false, 16288, 27), Is.EqualTo(UnclaimedPackets.Shape.Unknown));
    }

    [Test]
    public void ADecodedEffectResultWritesTheRealLine()
    {
        var r = UnclaimedPackets.ReadEffectResult(EffectResult(0x8B6, Player));
        var text = new StringWriter { NewLine = "\n" };
        new NetworkLogWriter(text, () => DateTimeOffset.UnixEpoch).EffectResult(Player, "Creatina Whey", r.Sequence, r.Hp, r.MaxHp, r.Mp, r.Shield,
            new CombatantState(0, 0, 0, 0, new Vector3(96.57f, 0f, 114.79f), -2.29f), r.Groups);
        Assert.That(Body(text), Is.EqualTo("100B0290|Creatina Whey|000008B6|17397|19343|10000|10000|0||96.57|114.79|0.00|-2.29|2700|0|0|01|30|29CE|44E10000|100B0290"));
    }

    [Test]
    public void ADecodedStatusListWritesTheRealLine()
    {
        var r = UnclaimedPackets.ReadStatusList(StatusList());
        var text = new StringWriter { NewLine = "\n" };
        new NetworkLogWriter(text, () => DateTimeOffset.UnixEpoch).StatusList(Carbuncle, "", r.JobLevels, r.Hp, r.MaxHp, r.Mp, r.MaxMp, r.Shield, null, r.Entries);
        Assert.That(Body(text), Is.EqualTo("40003F6B||00464600|14594|16288|10000|10000|0||||||0|0|0|29CE0030|455A0E30|10094563"));
    }
}
