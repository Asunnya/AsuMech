using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace AsuMech.Core.Game.Trace;

// Packets no recorder hook claims, told apart by what they carry rather than by opcode, which moves
// every patch. An opcode is trusted once three packets in a row read as the same shape:
// - an effect result names its own target and the sequence of an action effect just seen;
// - a status list opens with the target's job and carries its max HP.
public sealed class UnclaimedPackets
{
    public enum Shape { Unknown, EffectResult, StatusList }

    public const int EffectResultSize = 0x5C;
    public const int StatusListSize = 0x14 + StatusEntries * 12;
    public const int StatusEntries = 30;
    private const int VotesToLearn = 3;

    private readonly Dictionary<ushort, (Shape Shape, int Votes)> votes = [];

    public Shape Learned(ushort opcode)
        => votes.TryGetValue(opcode, out var v) && v.Votes >= VotesToLearn ? v.Shape : Shape.Unknown;

    public Shape Classify(ushort opcode, ReadOnlySpan<byte> payload, uint targetId, Func<uint, bool> recentSequence, uint targetMaxHp, byte targetJob)
    {
        if (Learned(opcode) is var known and not Shape.Unknown) return known;
        var guess = Guess(payload, targetId, recentSequence, targetMaxHp, targetJob);
        votes[opcode] = guess == Shape.Unknown ? (Shape.Unknown, 0)
            : votes.TryGetValue(opcode, out var v) && v.Shape == guess ? (guess, v.Votes + 1) : (guess, 1);
        return Learned(opcode);
    }

    public static Shape Guess(ReadOnlySpan<byte> payload, uint targetId, Func<uint, bool> recentSequence, uint targetMaxHp, byte targetJob)
    {
        if (payload.Length >= EffectResultSize && targetId != 0 && U32(payload, 0x08) == targetId
            && payload[0x19] <= 4 && recentSequence(U32(payload, 0x04)))
            return Shape.EffectResult;
        if (payload.Length >= StatusListSize && targetMaxHp != 0 && U32(payload, 0x08) == targetMaxHp
            && U32(payload, 0x04) <= targetMaxHp && payload[0] == targetJob)
            return Shape.StatusList;
        return Shape.Unknown;
    }

    public readonly record struct EffectResultData(uint Sequence, uint Hp, uint MaxHp, ushort Mp, byte Shield, uint[] Groups);

    // Groups as a parser writes them: (unknown, 0, unknown, count), then per status
    // (id | unknown << 16 | slot << 24, param | unknown << 16, duration bits, source).
    public static EffectResultData ReadEffectResult(ReadOnlySpan<byte> p)
    {
        var count = Math.Min(p[0x19], (byte)4);
        var groups = new uint[20];
        groups[0] = U16(p, 0x16);
        groups[2] = U16(p, 0x1A);
        groups[3] = p[0x19];
        for (var i = 0; i < count; i++)
        {
            var e = 0x1C + i * 16;
            groups[4 + i * 4] = (uint)(U16(p, e + 2) + (p[e + 1] << 16) + (p[e] << 24));
            groups[5 + i * 4] = (uint)(U16(p, e + 4) + (U16(p, e + 6) << 16));
            groups[6 + i * 4] = U32(p, e + 8);
            groups[7 + i * 4] = U32(p, e + 12);
        }
        return new EffectResultData(U32(p, 0x04), U32(p, 0x0C), U32(p, 0x10), U16(p, 0x14), p[0x18], groups);
    }

    public readonly record struct StatusListData(uint JobLevels, uint Hp, uint MaxHp, ushort Mp, ushort MaxMp, byte Shield, uint[] Entries);

    // Entries as a parser writes them: three header values, then per slot
    // (status | param << 16, duration bits, source).
    public static StatusListData ReadStatusList(ReadOnlySpan<byte> p)
    {
        var entries = new uint[3 + StatusEntries * 3];
        entries[0] = U16(p, 0x11);
        entries[1] = p[0x13];
        for (var i = 0; i < StatusEntries; i++)
        {
            var e = 0x14 + i * 12;
            var status = U16(p, e);
            if (status == 0) continue;
            entries[3 + i * 3] = (uint)(status + (U16(p, e + 2) << 16));
            entries[4 + i * 3] = U32(p, e + 4);
            entries[5 + i * 3] = U32(p, e + 8);
        }
        var jobLevels = (uint)(p[0] + (p[1] << 8) + (p[2] << 16) + (p[3] << 24));
        return new StatusListData(jobLevels, U32(p, 0x04), U32(p, 0x08), U16(p, 0x0C), U16(p, 0x0E), p[0x10], entries);
    }

    private static uint U32(ReadOnlySpan<byte> p, int at) => BinaryPrimitives.ReadUInt32LittleEndian(p[at..]);
    private static ushort U16(ReadOnlySpan<byte> p, int at) => BinaryPrimitives.ReadUInt16LittleEndian(p[at..]);
}
