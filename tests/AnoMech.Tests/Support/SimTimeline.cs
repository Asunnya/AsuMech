using System.Globalization;
using System.Numerics;
using System.Text;

namespace AnoMech.Tests;

// What the sim sent to the game client, one row per packet-shaped call on the fakes, in the
// replay timeline CSV's columns (.claude/tools/replay_timeline.py), so a run can be diffed against
// a replay. ActorControls stay raw (`ac` rows, category in `id`); the comparer names them.
internal sealed class SimTimeline
{
    public static readonly string[] Columns =
        ["t", "kind", "actor", "actor_name", "id", "name", "target", "target_name", "x", "z", "rot", "detail",
         "omen_delay", "ballista", "anim_lock", "anim_var", "knockback", "damage_type", "y", "p", "source"];

    private readonly List<Dictionary<string, string>> rows = [];

    public Func<float> Clock { get; set; } = () => 0f;

    // file:line of the scheduler entry fired last: the one that sent a row, unless the row came from a
    // per-frame tick (cast bar ends, movement) after it.
    public Func<string> Source { get; set; } = () => "";

    public IReadOnlyList<Dictionary<string, string>> Rows => rows;

    public void Add(string kind, uint actor = 0, string actorName = "", string id = "", string name = "",
        uint target = 0, string targetName = "", Vector3? position = null, float? rotation = null, string detail = "",
        string p = "", IReadOnlyDictionary<string, string>? extra = null)
    {
        var row = new Dictionary<string, string>
        {
            ["t"] = F(Clock(), "F3"),
            ["kind"] = kind,
            ["actor"] = actor == 0 ? "" : actor.ToString("X8"),
            ["actor_name"] = actorName,
            ["id"] = id,
            ["name"] = name,
            ["target"] = target == 0 ? "" : target.ToString("X8"),
            ["target_name"] = targetName,
            ["detail"] = detail,
            ["p"] = p,
            ["source"] = Source(),
        };
        if (position is { } pos)
        {
            row["x"] = F(pos.X);
            row["y"] = F(pos.Y);
            row["z"] = F(pos.Z);
        }
        if (rotation is { } rot) row["rot"] = F(rot);
        if (extra != null)
            foreach (var (key, value) in extra)
                row[key] = value;
        rows.Add(row);
    }

    // Hex, trailing zeros dropped, like the replay's `p`.
    public static string Params(params uint[] values)
    {
        var count = values.Length;
        while (count > 0 && values[count - 1] == 0) count--;
        return string.Join(",", values.Take(count).Select(v => $"0x{v:X}"));
    }

    public static string F(float value, string format = "F2") => value.ToString(format, CultureInfo.InvariantCulture);

    public void Write(string path)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Join(",", Columns));
        foreach (var row in rows)
            text.AppendLine(string.Join(",", Columns.Select(c => Quote(row.GetValueOrDefault(c, "")))));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, text.ToString());
    }

    private static string Quote(string value)
        => value.IndexOfAny([',', '"', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
