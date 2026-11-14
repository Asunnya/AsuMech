using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace AsuMech.Core.Game.Trace;

// What the log knows of a combatant when a line is written; positions are the game's (Y up).
public readonly record struct ActCombatant(uint Hp, uint MaxHp, uint Mp, uint MaxMp, Vector3 Position, float Heading);

// Writes a network log the way FFXIV_ACT_Plugin (lines 00-39) and OverlayPlugin (257-274) do,
// field for field, so whatever reads an ACT log reads a recording's too. Like ACT, positions go
// out as (X, game Z, height).
public sealed class ActLogWriter
{
    public const uint NoTarget = 0xE0000000;

    private readonly TextWriter output;
    private readonly Func<DateTimeOffset> clock;
    private readonly Func<string, string?>? rewrite;
    private readonly ActLogSeal seal = new();

    public int Lines { get; private set; }

    public ActLogWriter(TextWriter output, Func<DateTimeOffset> clock, Func<string, string?>? rewrite = null)
    {
        this.output = output;
        this.clock = clock;
        this.rewrite = rewrite;
    }

    public void Flush() => output.Flush();

    public void Version(string text) => Write(253, text);

    public void Zone(uint territory, string name) => Write(1, I($"{territory:X2}|{name}"));

    public void PrimaryPlayer(uint id, string name) => Write(2, I($"{id:X4}|{name}"));

    public void AddCombatant(uint id, uint owner, string name, int job, int level, uint worldId, string worldName, uint bnpcNameId, uint bnpcId, ActCombatant c)
        => Write(3, CombatantBody(id, owner, name, job, level, worldId, worldName, bnpcNameId, bnpcId, c));

    public void RemoveCombatant(uint id, uint owner, string name, int job, int level, uint worldId, string worldName, uint bnpcNameId, uint bnpcId, ActCombatant c)
        => Write(4, CombatantBody(id, owner, name, job, level, worldId, worldName, bnpcNameId, bnpcId, c));

    private static string CombatantBody(uint id, uint owner, string name, int job, int level, uint worldId, string worldName, uint bnpcNameId, uint bnpcId, ActCombatant c)
        => I($"{id:X4}|{name}|{job:X2}|{level:X1}|{owner:X4}|{worldId:X2}|{worldName}|{bnpcNameId}|{bnpcId}|") + Props(c.Hp, c.MaxHp, c.Mp, c.MaxMp, null, c);

    public void Chat(uint type, string sender, string text) => Write(0, I($"{type:X4}|{sender}|{text}"));

    public void StartsCasting(uint source, string sourceName, uint target, string targetName, uint actionId, string actionName, float duration, ActCombatant? caster)
    {
        var p = caster is { } c ? Act(c.Position) : (Vector3?)null;
        Write(20, I($"{source:X4}|{sourceName}|{actionId:X2}|{actionName}|{target:X4}|{targetName}|{duration:0.000}|{p?.X:0.00}|{p?.Y:0.00}|{p?.Z:0.00}|{caster?.Heading:0.00}"));
    }

    // One line per target, or one with no target; `effects` is that target's eight raw entries.
    public void Ability(bool aoe, uint source, string sourceName, uint skillId, string skillName, uint target, string targetName,
        ActCombatant? caster, ActCombatant? victim, ReadOnlySpan<ulong> effects, uint sequence, int targetIndex, int totalTargets,
        uint owner, string ownerName, byte displayType, uint actionId, uint animationId, float animationLock, ushort rotation)
    {
        var text = new StringBuilder();
        text.Append(I($"{source:X2}|{sourceName}|{skillId:X2}|{skillName}|{target:X2}|{targetName}|"));
        for (var i = 0; i < 8; i++)
        {
            var effect = i < effects.Length ? effects[i] : 0UL;
            text.Append(I($"{effect & 0xFFFFFFFFu:X1}|{effect >> 32:X1}|"));
        }
        text.Append(victim is { } v ? Props(v.Hp, v.MaxHp, v.Mp, v.MaxMp, null, v) : Props(null, null, null, null, null, null));
        text.Append('|');
        text.Append(caster is { } c ? Props(c.Hp, c.MaxHp, c.Mp, c.MaxMp, null, c) : Props(null, null, null, null, null, null));
        text.Append(I($"|{sequence:X8}|{targetIndex}|{totalTargets}|{owner:X2}|{ownerName}|{displayType:X2}|{actionId:X2}|{animationId:X2}|{animationLock:0.000}|{rotation:X4}"));
        Write(aoe ? 22 : 21, text.ToString());
    }

    public void Cancel(uint id, string name, uint actionId, string actionName, bool cancelled, bool interrupted)
        => Write(23, I($"{id:X2}|{name}|{actionId:X2}|{actionName}|{(cancelled ? "Cancelled" : "")}{(interrupted ? "Interrupted" : "")}"));

    public void DoT(uint target, string targetName, bool heal, uint buffId, uint amount, ActCombatant? victim, uint source, string sourceName, uint damageType, ActCombatant? caster)
    {
        var t = victim is { } v ? Props(v.Hp, v.MaxHp, v.Mp, v.MaxMp, null, v) : Props(null, null, null, null, null, null);
        var s = caster is { } c ? Props(c.Hp, c.MaxHp, c.Mp, c.MaxMp, null, c) : Props(null, null, null, null, null, null);
        Write(24, I($"{target:X4}|{targetName}|{(heal ? "HoT" : "DoT")}|{buffId:X1}|{amount:X1}|{t}|{source:X4}|{sourceName}|{damageType:X1}|{s}"));
    }

    public void Death(uint target, string targetName, uint source, string sourceName)
        => Write(25, I($"{target:X2}|{targetName}|{source:X2}|{sourceName}"));

    // A permanent status is 9999 s, as ACT has it.
    public void StatusAdd(ushort statusId, string name, float duration, uint source, string sourceName, uint target, string targetName, ushort extra, uint? targetMaxHp, uint? sourceMaxHp)
        => Write(26, StatusBody(statusId, name, duration is <= 0f or > 9999f ? 9999f : duration, source, sourceName, target, targetName, extra, targetMaxHp, sourceMaxHp));

    public void StatusRemove(ushort statusId, string name, uint source, string sourceName, uint target, string targetName, ushort extra, uint? targetMaxHp, uint? sourceMaxHp)
        => Write(30, StatusBody(statusId, name, 0f, source, sourceName, target, targetName, extra, targetMaxHp, sourceMaxHp));

    private static string StatusBody(ushort statusId, string name, float duration, uint source, string sourceName, uint target, string targetName, ushort extra, uint? targetMaxHp, uint? sourceMaxHp)
        => I($"{statusId:X2}|{name}|{duration:0.00}|{source:X4}|{sourceName}|{target:X4}|{targetName}|{extra:X2}|{targetMaxHp}|{sourceMaxHp}");

    public void TargetIcon(uint id, string name, uint headerUnknown, uint padding, uint p1, uint p2, uint p3, uint p4)
        => Write(27, I($"{id:X2}|{name}|{headerUnknown:X4}|{padding:X4}|{p1:X4}|{p2:X4}|{p3:X4}|{p4:X4}"));

    public void Director(params uint[] parameters)
        => Write(33, string.Join("|", parameters.Select(p => p.ToString("X2", CultureInfo.InvariantCulture))));

    public void NameToggle(uint id, string name, byte toggle) => Write(34, I($"{id:X2}|{name}|{id:X2}|{name}|{toggle:X2}"));

    public void Tether(uint source, string sourceName, uint target, string targetName, uint padding, uint p1, uint p2, uint p3, uint p4, uint padding1)
        => Write(35, I($"{source:X2}|{sourceName}|{target:X2}|{targetName}|{padding:X4}|{p1:X4}|{p2:X4}|{p3:X4}|{p4:X4}|{padding1:X4}"));

    public void LimitBreak(byte bars, uint value) => Write(36, I($"{value:X4}|{bars}"));

    public void EffectResultBasic(uint id, string name, uint sequence, uint hp, ActCombatant? c)
        => Write(37, I($"{id:X4}|{name}|{sequence:X8}|") + Props(hp, null, null, null, null, c));

    public void UpdateHp(uint id, string name, ActCombatant c) => Write(39, I($"{id:X2}|{name}|") + Props(c.Hp, c.MaxHp, c.Mp, c.MaxMp, null, c));

    public void MapEffect(uint instance, uint flags, byte index)
        => Write(257, I($"{(instance == 0 ? "" : instance.ToString("X8", CultureInfo.InvariantCulture))}|{flags:X8}|{index:X2}||"));

    public void InCombat(bool inCombat) => Write(260, inCombat ? "1|1|1|1" : "0|0|1|1");

    public void Combatant(string change, uint id, IEnumerable<(string Field, string Value)> fields)
        => Write(261, I($"{change}|{id:X8}") + string.Concat(fields.Select(f => $"|{f.Field}|{f.Value}")));

    public static IEnumerable<(string Field, string Value)> PoseFields(Vector3 position, float heading)
    {
        var p = Act(position);
        yield return ("PosX", F4(p.X));
        yield return ("PosY", F4(p.Y));
        yield return ("PosZ", F4(p.Z));
        yield return ("Heading", F4(heading));
    }

    public void CastExtra(uint id, uint actionId, Vector3 position, float heading)
    {
        var p = Act(position);
        Write(263, I($"{id:X8}|{actionId:X4}|{p.X:F3}|{p.Y:F3}|{p.Z:F3}|{heading:F3}"));
    }

    public void AbilityExtra(uint id, uint actionId, uint sequence, Vector3? position, float heading, uint animationTarget)
    {
        if (position is not { } pos)
        {
            Write(264, I($"{id:X8}|{actionId:X4}|{sequence:X8}|0||||{heading:F3}|{animationTarget:X8}"));
            return;
        }
        var p = Act(pos);
        Write(264, I($"{id:X8}|{actionId:X4}|{sequence:X8}|1|{p.X:F3}|{p.Y:F3}|{p.Z:F3}|{heading:F3}|{animationTarget:X8}"));
    }

    public void BattleTalk(uint actor, uint instance, uint nameId, uint textId, uint displayMs, uint p1, uint p2, uint p3, uint p4)
        => Write(267, I($"{actor:X8}|{instance:X8}|{nameId:X4}|{textId:X4}|{displayMs}|{p1:X}|{p2:X}|{p3:X}|{p4:X}"));

    public void ActorMove(uint id, float heading, uint unknown1, uint unknown2, Vector3 position)
    {
        var p = Act(position);
        Write(270, I($"{id:X8}|{heading:F4}|{unknown1:X4}|{unknown2:X4}|{p.X:F4}|{p.Y:F4}|{p.Z:F4}"));
    }

    public void ActorSetPos(uint id, float heading, uint unknown1, uint unknown2, Vector3 position)
    {
        var p = Act(position);
        Write(271, I($"{id:X8}|{heading:F4}|{unknown1:X2}|{unknown2:X2}|{p.X:F4}|{p.Y:F4}|{p.Z:F4}"));
    }

    public void ActorControlExtra(bool self, uint id, ushort category, uint p1, uint p2, uint p3, uint p4)
        => Write(self ? 274 : 273, I($"{id:X8}|{category:X4}|{p1:X}|{p2:X}|{p3:X}|{p4:X}"));

    private void Write(int type, string body)
    {
        var line = $"{type.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}|{clock().ToString("O", CultureInfo.InvariantCulture)}|{body.Replace('\0', ' ')}";
        if (rewrite != null)
        {
            if (rewrite(line) is not { } kept) return;
            line = kept;
        }
        output.WriteLine(seal.Seal(line));
        Lines++;
    }

    private static string Props(uint? hp, uint? maxHp, uint? mp, uint? maxMp, byte? shield, ActCombatant? c)
    {
        var p = c is { } known ? Act(known.Position) : (Vector3?)null;
        return I($"{hp}|{maxHp}|{mp}|{maxMp}|{shield}||{p?.X:0.00}|{p?.Y:0.00}|{p?.Z:0.00}|{c?.Heading:0.00}");
    }

    private static Vector3 Act(Vector3 game) => new(game.X, game.Z, game.Y);

    private static string F4(float value) => value.ToString("F4", CultureInfo.InvariantCulture);

    private static string I(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}

// ACT's line hash: the first 8 bytes of SHA-256 over the line, a '|' and the line's index, which
// starts over at every zone change (01) and version line (253).
public sealed class ActLogSeal
{
    private int index;

    public string Seal(string line)
    {
        if (line.StartsWith("01|", StringComparison.Ordinal) || line.StartsWith("253|", StringComparison.Ordinal)) index = 0;
        index++;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{line}|{index.ToString(CultureInfo.InvariantCulture)}"));
        return $"{line}|{Convert.ToHexStringLower(hash.AsSpan(0, 8))}";
    }

    // A line read back from a log, with its old hash replaced.
    public string Reseal(string line)
    {
        var last = line.LastIndexOf('|');
        return Seal(last > 0 ? line[..last] : line);
    }
}
