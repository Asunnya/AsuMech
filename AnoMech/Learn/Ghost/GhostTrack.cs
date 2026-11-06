using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Trace;
using AnoMech.Core.UserActions;
using AnoMech.Learn.Mitigation;
using AnoMech.Learn.Tools;

namespace AnoMech.Learn.Ghost;

public readonly record struct GhostSample(float T, Vector2 Position, float Rotation);

// One player of a real pull: a label ("player 3"), the seat their job puts them in, and where they
// were, in the pull's seconds and the scenario's local frame.
public sealed record GhostLane(string Label, PartyRole? Role, byte Job, IReadOnlyList<GhostSample> Samples);

// A real pull's party, to walk beside the sim: from a PullRecorder CSV (its `pos` rows, ten a
// second) or an ACT log (its combatant-memory lines, else the hits). Offset maps the sim's clock
// onto the pull's: aligned on the first enemy cast the scenario's timeline also has.
public sealed class GhostTrack
{
    // A lane is held at its last sample this long before it fades: the ACT log's hits come seconds apart.
    public const float HoldSeconds = 3f;

    public IReadOnlyList<GhostLane> Lanes { get; }
    public IReadOnlyList<(float T, uint ActionId)> EnemyCasts { get; }
    public float Offset { get; private set; }
    public bool Aligned { get; private set; }
    public string? AlignedOn { get; private set; }

    public GhostTrack(IReadOnlyList<GhostLane> lanes, IReadOnlyList<(float T, uint ActionId)> enemyCasts)
    {
        Lanes = lanes;
        EnemyCasts = enemyCasts;
    }

    // The sim's clock on the pull's: the scenario's first cast that the pull also shows lines up.
    public bool Align(CastTimeline timeline)
    {
        foreach (var (t, id) in EnemyCasts)
        {
            if (timeline.Casts.FirstOrDefault(c => c.ActionId == id) is not { } cast) continue;
            Offset = t - cast.Start;
            Aligned = true;
            AlignedOn = cast.Name;
            return true;
        }
        Aligned = false;
        return false;
    }

    public void AlignAt(float offset)
    {
        Offset = offset;
        Aligned = true;
    }

    // Where the lane was at this sim time: interpolated between samples, held at the edges for
    // HoldSeconds, null outside.
    public GhostSample? At(GhostLane lane, float simTime)
    {
        var samples = lane.Samples;
        if (samples.Count == 0) return null;
        var t = simTime + Offset;
        if (t < samples[0].T - HoldSeconds || t > samples[^1].T + HoldSeconds) return null;
        if (t <= samples[0].T) return samples[0] with { T = simTime };
        if (t >= samples[^1].T) return samples[^1] with { T = simTime };
        int lo = 0, hi = samples.Count - 1;
        while (hi - lo > 1)
        {
            var mid = (lo + hi) / 2;
            if (samples[mid].T <= t) lo = mid; else hi = mid;
        }
        var a = samples[lo];
        var b = samples[hi];
        var f = b.T > a.T ? (t - a.T) / (b.T - a.T) : 0f;
        // A recording writes a pose only when it changed: a long gap is standing still, not a slow walk.
        if (b.T - a.T > HoldSeconds * 2f) return a with { T = simTime };
        return new GhostSample(simTime, Vector2.Lerp(a.Position, b.Position, f), f < 0.5f ? a.Rotation : b.Rotation);
    }

    public GhostLane? LaneFor(PartyRole role) => Lanes.FirstOrDefault(l => l.Role == role);

    // ── Sources ──────────────────────────────────────────────────────────────

    public static GhostTrack FromRecording(IReadOnlyList<Dictionary<string, string>> rows, Vector3 origin)
    {
        var jobs = new Dictionary<string, byte>();
        var samples = new Dictionary<string, List<GhostSample>>();
        var labels = new Dictionary<string, string>();
        var casts = new List<(float, uint)>();
        foreach (var row in rows)
        {
            var actor = row.GetValueOrDefault("actor", "");
            var kind = row.GetValueOrDefault("kind");
            if (kind == "cast" && CastRow.From(row) is { IsEnemy: true } cast) casts.Add((cast.T, cast.ActionId));
            if (!actor.StartsWith('1') || actor.Length != 8) continue;
            labels.TryAdd(actor, row.GetValueOrDefault("actor_name", actor));
            if (kind is "member" or "spawn_player" && byte.TryParse(row.GetValueOrDefault("id"), out var job)) jobs[actor] = job;
            if (kind == "pos" && SimTimeline.Float(row, "x") is { } x && SimTimeline.Float(row, "z") is { } z)
            {
                if (!samples.TryGetValue(actor, out var list)) samples[actor] = list = [];
                list.Add(new GhostSample(SimTimeline.Time(row), new Vector2(x - origin.X, z - origin.Z), SimTimeline.Float(row, "rot") ?? 0f));
            }
        }
        return Build(samples.Keys.Select(a => (labels[a], jobs.GetValueOrDefault(a), (IReadOnlyList<GhostSample>)samples[a].OrderBy(s => s.T).ToList())).ToList(), casts);
    }

    public static GhostTrack FromActPull(ActPull pull, Vector3 origin)
    {
        var lanes = pull.Events.OfType<ActPose>().GroupBy(p => p.Actor.Id)
            .Select(g => (g.First().Actor.Name, pull.Jobs.GetValueOrDefault(g.Key),
                (IReadOnlyList<GhostSample>)g.OrderBy(p => p.T).Select(p => new GhostSample(p.T, new Vector2(p.Position.X - origin.X, p.Position.Z - origin.Z), p.Rotation)).ToList()))
            .ToList();
        var casts = pull.Events.OfType<ActCast>().Where(c => c.Caster.IsEnemy).Select(c => (c.T, c.ActionId)).ToList();
        return Build(lanes, casts);
    }

    private static GhostTrack Build(List<(string Label, byte Job, IReadOnlyList<GhostSample> Samples)> players, List<(float, uint)> casts)
    {
        var roles = GhostRoles.Assign(players.Select(p => (p.Label, p.Job)).ToList());
        var lanes = players.Select(p => new GhostLane(p.Label, roles.GetValueOrDefault(p.Label), p.Job, p.Samples))
            .OrderBy(l => l.Role ?? (PartyRole)99).ThenBy(l => l.Label, StringComparer.Ordinal).ToList();
        return new GhostTrack(lanes, casts.OrderBy(c => c.Item1).ToList());
    }

    // A recording (.csv / .csv.gz) or an ACT log (.log), one pull of it.
    public static GhostTrack Load(string path, uint territory, int pull, Vector3 origin)
    {
        if (path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            var pulls = ActLog.Pulls(path, territory);
            if (pulls.Count == 0) throw new InvalidDataException($"no pull in territory {territory} in {Path.GetFileName(path)}");
            if (pull < 1 || pull > pulls.Count) throw new InvalidDataException($"pull {pull}: the log has {pulls.Count} in this territory");
            return FromActPull(pulls[pull - 1], origin);
        }
        var rows = Recording.Load(path);
        if (Recording.Territory(rows) is { } recorded && recorded != territory)
            throw new InvalidDataException($"the recording is of territory {recorded}, not {territory}");
        var recordedPulls = Recording.Pulls(rows);
        if (pull < 1 || pull > recordedPulls.Count) throw new InvalidDataException($"pull {pull}: the recording has {recordedPulls.Count}");
        return FromRecording(recordedPulls[pull - 1], origin);
    }
}

// A real party's seats from their jobs: tanks in the order met, the regen and shield healer, the
// melee in order, then phys ranged and caster; a second of one kind takes a free seat of its group.
public static class GhostRoles
{
    private static readonly HashSet<JobId> Tanks = [JobId.Paladin, JobId.Warrior, JobId.DarkKnight, JobId.Gunbreaker, JobId.Gladiator, JobId.Marauder];
    private static readonly HashSet<JobId> Regen = [JobId.WhiteMage, JobId.Astrologian, JobId.Conjurer];
    private static readonly HashSet<JobId> Shield = [JobId.Scholar, JobId.Sage];
    private static readonly HashSet<JobId> Melee = [JobId.Monk, JobId.Dragoon, JobId.Ninja, JobId.Samurai, JobId.Reaper, JobId.Viper, JobId.Pugilist, JobId.Lancer, JobId.Rogue];
    private static readonly HashSet<JobId> PhysRanged = [JobId.Bard, JobId.Machinist, JobId.Dancer, JobId.Archer];

    public static Dictionary<string, PartyRole> Assign(IReadOnlyList<(string Label, byte Job)> players)
    {
        var result = new Dictionary<string, PartyRole>();
        var free = new List<PartyRole>(Enum.GetValues<PartyRole>());
        bool Take(string label, params PartyRole[] seats)
        {
            foreach (var seat in seats)
                if (free.Remove(seat)) { result[label] = seat; return true; }
            return false;
        }
        foreach (var (label, raw) in players.Where(p => p.Job != 0))
        {
            var job = (JobId)raw;
            _ = Tanks.Contains(job) ? Take(label, PartyRole.MainTank, PartyRole.OffTank)
                : Regen.Contains(job) ? Take(label, PartyRole.RegenHealer, PartyRole.ShieldHealer)
                : Shield.Contains(job) ? Take(label, PartyRole.ShieldHealer, PartyRole.RegenHealer)
                : Melee.Contains(job) ? Take(label, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps)
                : PhysRanged.Contains(job) ? Take(label, PartyRole.PhysRangedDps, PartyRole.MeleeDpsB, PartyRole.MeleeDpsA, PartyRole.CasterDps)
                : Take(label, PartyRole.CasterDps, PartyRole.PhysRangedDps, PartyRole.MeleeDpsB, PartyRole.MeleeDpsA);
        }
        return result;
    }

    public static string Short(PartyRole role) => role switch
    {
        PartyRole.MainTank => "MT", PartyRole.OffTank => "OT", PartyRole.RegenHealer => "H1", PartyRole.ShieldHealer => "H2",
        PartyRole.MeleeDpsA => "M1", PartyRole.MeleeDpsB => "M2", PartyRole.PhysRangedDps => "R1", PartyRole.CasterDps => "R2", _ => role.ToString(),
    };
}
