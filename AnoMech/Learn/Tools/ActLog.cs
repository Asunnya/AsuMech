using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace AnoMech.Learn.Tools;

// One pull of an ACT/IINACT network log, as the scenario-skeleton reads it: the enemy-side events
// between combat start and end, times in seconds from the pull's start. Positions are the log's
// (X, Y, Z) with Y up, as the game's; a scenario's local frame is the caller's business. Players
// are "player 1", "player 2", ... in order of appearance: their names never leave the log.
public sealed record ActPull(int Index, uint Territory, DateTime Start, DateTime End, bool Clear, List<ActEvent> Events,
    IReadOnlyDictionary<uint, byte>? PlayerJobs = null)
{
    // Each player's ClassJob, from the zone's combatant lines, which come before the pull.
    public IReadOnlyDictionary<uint, byte> Jobs => PlayerJobs ?? new Dictionary<uint, byte>();

    public float Duration => (float)(End - Start).TotalSeconds;

    // The first enemy cast, which the skeleton's clock starts on.
    public float? FirstCastAt => Events.OfType<ActCast>().FirstOrDefault(c => c.Caster.IsEnemy)?.T;
}

public sealed record ActActor(uint Id, string Name)
{
    public bool IsEnemy => Id >> 28 == 4;
    public bool IsPlayer => Id >> 28 == 1;
}

public abstract record ActEvent(float T);

public sealed record ActSpawn(float T, ActActor Actor, uint NameId, uint BaseId, byte Level, Vector3 Position, float Rotation) : ActEvent(T);
public sealed record ActDespawn(float T, ActActor Actor) : ActEvent(T);
public sealed record ActCast(float T, ActActor Caster, uint ActionId, string Name, ActActor? Target, float CastTime, Vector3 Position, float Rotation) : ActEvent(T);
// One target of one action instance; Sequence ties the instances' targets together.
public sealed record ActAbility(float T, ActActor Caster, uint ActionId, string Name, ActActor? Target, Vector3 CasterPosition, float CasterRotation, Vector3? TargetPosition, uint Sequence, int TargetIndex, int TargetCount, int? Damage) : ActEvent(T);
public sealed record ActStatus(float T, bool Gained, ushort StatusId, string Name, float Duration, ActActor Source, ActActor Target, int Stacks) : ActEvent(T);
public sealed record ActMarker(float T, ActActor Target, uint MarkerId) : ActEvent(T);
public sealed record ActTether(float T, ActActor Source, ActActor Target, ushort TetherId) : ActEvent(T);
public sealed record ActDirector(float T, uint Instance, uint Command, uint[] Params) : ActEvent(T);
public sealed record ActMapEffect(float T, uint Instance, uint Flags, byte Index) : ActEvent(T);
// Where a player was: from the combatant-memory lines (261, every move) when the log has them,
// else from their own abilities and the hits they took (21/22, a few a second at best).
public sealed record ActPose(float T, ActActor Actor, Vector3 Position, float Rotation) : ActEvent(T);

public static class ActLog
{
    // Combat runs from the game-combat flag going up to it going down (260 lines); a log without
    // them falls back to the duty director's commence and wipe/clear commands.
    private const uint DirectorCommence = 0x40000001;
    private const uint DirectorClear = 0x40000003;
    private const uint DirectorWipe = 0x40000005;
    private const uint DirectorWipe2 = 0x40000006;

    public static List<ActPull> Pulls(string path, uint territory) => Pulls(File.ReadLines(path), territory);

    public static List<ActPull> Pulls(IEnumerable<string> lines, uint territory)
    {
        var pulls = new List<ActPull>();
        uint zone = 0;
        // Declared ahead of Close, which reads them.
        Dictionary<string, string> players = new(StringComparer.Ordinal);
        Dictionary<uint, byte> jobs = [];
        var inCombat = false;
        var sawCombatFlag = false;
        DateTime? start = null;
        List<string[]>? pending = null;
        var clear = false;

        void Close(DateTime end)
        {
            if (start is not { } s || pending is null) return;
            var events = pending.Select(p => Parse(p, s)).OfType<ActEvent>().Concat(pending.SelectMany(p => Poses(p, s, players))).OrderBy(e => e.T).ToList();
            pulls.Add(new ActPull(pulls.Count + 1, zone, s, end, clear, events, new Dictionary<uint, byte>(jobs)));
            start = null;
            pending = null;
            clear = false;
        }

        var lastTime = DateTime.MinValue;
        foreach (var raw in lines)
        {
            var parts = raw.Split('|');
            if (parts.Length < 3) continue;
            var op = parts[0];
            Anonymize(parts, players);
            if (op == "01")
            {
                if (start is { } s) Close(Time(parts[1]));
                zone = Hex(parts[2]);
                continue;
            }
            if (zone != territory) continue;
            var time = Time(parts[1]);
            lastTime = time;
            NoteJob(parts, jobs);
            switch (op)
            {
                case "260":
                {
                    sawCombatFlag = true;
                    var now = parts.Length > 3 && parts[3] == "1";
                    if (now && !inCombat) { start = time; pending = []; clear = false; }
                    else if (!now && inCombat) Close(time);
                    inCombat = now;
                    continue;
                }
                case "33" when !sawCombatFlag:
                {
                    var command = Hex(parts[3]);
                    if (command == DirectorCommence && start is null) { start = time; pending = []; clear = false; }
                    else if (command is DirectorWipe or DirectorWipe2 && start is { }) { pending?.Add(parts); Close(time); continue; }
                    break;
                }
            }
            if (op == "33" && start is { } && Hex(parts[3]) == DirectorClear) clear = true;
            if (start is { } && pending is { }) pending.Add(parts);
        }
        // A log that ends mid-pull (the capture stopped) keeps what it has.
        if (start is { }) Close(lastTime);
        return pulls;
    }

    // One line, in the pull's own seconds; null for a kind the skeleton doesn't read.
    private static ActEvent? Parse(string[] p, DateTime start)
    {
        var t = (float)(Time(p[1]) - start).TotalSeconds;
        try
        {
            switch (p[0])
            {
                case "03" when p.Length > 20 && Actor(p, 2) is { IsEnemy: true } spawned:
                    return new ActSpawn(t, spawned, Dec(p[9]), Dec(p[10]), (byte)Hex(p[5]), Pos(p, 17), F(p[20]));
                case "04" when p.Length > 3 && Actor(p, 2) is { IsEnemy: true } gone:
                    return new ActDespawn(t, gone);
                case "20" when p.Length > 12 && Actor(p, 2) is { IsEnemy: true } caster:
                    return new ActCast(t, caster, Hex(p[4]), p[5], Actor(p, 6), F(p[8]), Pos(p, 9), F(p[12]));
                case "21" or "22" when p.Length > 46 && Actor(p, 2) is { IsEnemy: true } source:
                {
                    var target = Actor(p, 6);
                    Vector3? targetPos = target is null || p[30].Length == 0 ? null : Pos(p, 30);
                    return new ActAbility(t, source, Hex(p[4]), p[5], target, Pos(p, 40), F(p[43]), targetPos, Hex(p[44]), (int)Hex(p[45]), (int)Hex(p[46]), Damage(p[8], p[9]));
                }
                case "26" or "30" when p.Length > 9 && Actor(p, 5) is { } from && Actor(p, 7) is { } to && (from.IsEnemy || to.IsEnemy || from.Id == 0xE0000000):
                    return new ActStatus(t, p[0] == "26", (ushort)Hex(p[2]), p[3], F(p[4]), from, to, (int)Hex(p[9]));
                case "27" when p.Length > 6 && Actor(p, 2) is { } marked:
                    return new ActMarker(t, marked, Hex(p[6]));
                case "35" when p.Length > 8 && Actor(p, 2) is { } a && Actor(p, 4) is { } b:
                    return new ActTether(t, a, b, (ushort)Hex(p[8]));
                case "33" when p.Length > 7:
                    return new ActDirector(t, Hex(p[2]), Hex(p[3]), [Hex(p[4]), Hex(p[5]), Hex(p[6]), Hex(p[7])]);
                case "257" when p.Length > 4:
                    return new ActMapEffect(t, Hex(p[2]), Hex(p[3]), (byte)Hex(p[4]));
                default:
                    return null;
            }
        }
        catch (Exception)
        {
            // A line that doesn't fit its own shape is skipped rather than guessed at.
            return null;
        }
    }

    // The line kinds that carry (id, name) pairs; others put a key or a number after an id.
    private static readonly HashSet<string> NamedPairs = ["03", "04", "20", "21", "22", "23", "24", "25", "26", "27", "30", "34", "35", "36", "37", "38", "39"];

    // Every (id, name) pair of a player on the line gets the player's number instead of the name;
    // a combatant-memory line's Name key does too.
    private static void Anonymize(string[] parts, Dictionary<string, string> players)
    {
        string Label(string id)
        {
            if (!players.TryGetValue(id, out var label)) label = players[id] = $"player {players.Count + 1}";
            return label;
        }
        if (parts[0] == "261")
        {
            if (parts.Length > 3 && IsPlayerId(parts[3]))
                for (var i = 4; i + 1 < parts.Length; i += 2)
                    if (parts[i] == "Name") parts[i + 1] = Label(parts[3]);
            return;
        }
        if (!NamedPairs.Contains(parts[0])) return;
        for (var i = 2; i + 1 < parts.Length; i++)
        {
            if (!IsPlayerId(parts[i]) || parts[i + 1].Length == 0) continue;
            parts[i + 1] = Label(parts[i]);
        }
    }

    private static bool IsPlayerId(string text)
        => text.Length == 8 && text.StartsWith("10", StringComparison.Ordinal) && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);

    // A player's ClassJob: hex on an AddCombatant line, decimal under a combatant-memory Job key.
    private static void NoteJob(string[] p, Dictionary<uint, byte> jobs)
    {
        if (p[0] == "03" && p.Length > 4 && IsPlayerId(p[2])) jobs[Hex(p[2])] = (byte)Hex(p[4]);
        else if (p[0] == "261" && p.Length > 3 && IsPlayerId(p[3]))
            for (var i = 4; i + 1 < p.Length; i += 2)
                if (p[i] == "Job" && byte.TryParse(p[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var job) && job != 0) jobs[Hex(p[3])] = job;
    }

    private static IEnumerable<ActPose> Poses(string[] p, DateTime start, Dictionary<string, string> players)
    {
        var t = (float)(Time(p[1]) - start).TotalSeconds;
        switch (p[0])
        {
            case "261" when p.Length > 3 && IsPlayerId(p[3]):
            {
                float? x = null, z = null, y = null, heading = null;
                for (var i = 4; i + 1 < p.Length; i += 2)
                    switch (p[i])
                    {
                        case "PosX": x = F(p[i + 1]); break;
                        case "PosY": z = F(p[i + 1]); break;
                        case "PosZ": y = F(p[i + 1]); break;
                        case "Heading": heading = F(p[i + 1]); break;
                    }
                if (x is { } px && z is { } pz)
                    yield return new ActPose(t, new ActActor(Hex(p[3]), players.GetValueOrDefault(p[3], "")), new Vector3(px, y ?? 0f, pz), heading ?? 0f);
                break;
            }
            case "21" or "22" when p.Length > 43:
            {
                if (Actor(p, 2) is { IsPlayer: true } source && p[40].Length > 0) yield return new ActPose(t, source, Pos(p, 40), F(p[43]));
                if (Actor(p, 6) is { IsPlayer: true } target && p[30].Length > 0 && target.Id != Actor(p, 2)?.Id) yield return new ActPose(t, target, Pos(p, 30), F(p[33]));
                break;
            }
        }
    }

    private static ActActor? Actor(string[] p, int idIndex)
    {
        if (idIndex + 1 >= p.Length || p[idIndex].Length != 8) return null;
        var id = Hex(p[idIndex]);
        return id is 0 or 0xE0000000 ? null : new ActActor(id, p[idIndex + 1]);
    }

    // The damage field pair of a 21 line: a 0x03 effect's value, with the high byte folded in when flagged.
    private static int? Damage(string flagText, string valueText)
    {
        var flag = Hex(flagText);
        if ((flag & 0xFF) != 0x03) return null;
        var v = Hex(valueText);
        var b0 = (v >> 24) & 0xFF;
        var b1 = (v >> 16) & 0xFF;
        var b2 = (v >> 8) & 0xFF;
        var b3 = v & 0xFF;
        return (int)((b2 & 0x40) != 0 ? (b3 << 16) | (b0 << 8) | b1 : (b0 << 8) | b1);
    }

    private static Vector3 Pos(string[] p, int x) => new(F(p[x]), F(p[x + 2]), F(p[x + 1]));

    private static DateTime Time(string text) => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    private static uint Hex(string text) => text.Length == 0 ? 0 : uint.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    private static uint Dec(string text) => uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
    private static float F(string text) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;
}
