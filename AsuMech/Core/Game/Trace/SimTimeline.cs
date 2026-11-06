using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace AsuMech.Core.Game.Trace;

// One row per packet-shaped event, in fixed columns, so a headless run (the fakes), a real pull
// (PullRecorder) and a replay can be diffed as text. ActorControls stay raw (`ac` rows, category
// in `id`); the comparer names them.
//
// Without a sink rows stay in memory (Rows) until Write; with one, each row goes to it as it is
// added and Rows stays empty, so a recording of any length costs no memory.
public sealed class SimTimeline
{
    public static readonly string[] Columns =
        ["t", "kind", "actor", "actor_name", "id", "name", "target", "target_name", "x", "z", "rot", "detail",
         "omen_delay", "ballista", "anim_lock", "anim_var", "knockback", "damage_type", "y", "p", "source"];

    private readonly List<Dictionary<string, string>> rows = [];
    private readonly TextWriter? sink;
    private bool headerWritten;

    public SimTimeline(TextWriter? sink = null) => this.sink = sink;

    public Func<float> Clock { get; set; } = () => 0f;

    // file:line of the scheduler entry fired last: the one that sent a row, unless the row came from a
    // per-frame tick (cast bar ends, movement) after it.
    public Func<string> Source { get; set; } = () => "";

    public IReadOnlyList<Dictionary<string, string>> Rows => rows;

    public int Count { get; private set; }

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
        Count++;
        if (sink == null) rows.Add(row);
        else WriteRow(sink, row);
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
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, ToCsv());
    }

    public string ToCsv() => ToCsv(rows);

    public static string ToCsv(IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        var text = new StringWriter();
        text.NewLine = "\n";
        text.WriteLine(Header);
        foreach (var row in rows) text.WriteLine(Line(row));
        return text.ToString();
    }

    public void Flush() => sink?.Flush();

    private void WriteRow(TextWriter writer, Dictionary<string, string> row)
    {
        if (!headerWritten)
        {
            writer.WriteLine(Header);
            headerWritten = true;
        }
        writer.WriteLine(Line(row));
    }

    private static string Header => string.Join(",", Columns);

    private static string Line(IReadOnlyDictionary<string, string> row)
        => string.Join(",", Columns.Select(c => Quote(row.GetValueOrDefault(c, ""))));

    private static string Quote(string value)
        => value.IndexOfAny([',', '"', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    // The rows of a CSV this class wrote, keyed by its own header, so a file from another column
    // order or an older build still reads. Empty cells are kept, so every row has every column.
    public static List<Dictionary<string, string>> Read(string csv)
    {
        var records = ParseCsv(csv);
        var result = new List<Dictionary<string, string>>();
        if (records.Count == 0) return result;
        var header = records[0];
        foreach (var cells in records.Skip(1))
        {
            if (cells.Count == 1 && cells[0].Length == 0) continue;
            var row = new Dictionary<string, string>();
            for (var i = 0; i < header.Count; i++)
                row[header[i]] = i < cells.Count ? cells[i] : "";
            result.Add(row);
        }
        return result;
    }

    public static float Time(IReadOnlyDictionary<string, string> row)
        => float.Parse(row["t"], NumberStyles.Float, CultureInfo.InvariantCulture);

    public static float? Float(IReadOnlyDictionary<string, string> row, string column)
        => row.TryGetValue(column, out var text) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    // RFC 4180: a quoted cell may hold commas, doubled quotes and line breaks.
    private static List<List<string>> ParseCsv(string text)
    {
        var records = new List<List<string>>();
        var cells = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = false;
                }
                else cell.Append(c);
                continue;
            }
            switch (c)
            {
                case '"': quoted = true; break;
                case ',': cells.Add(cell.ToString()); cell.Clear(); break;
                case '\r': break;
                case '\n':
                    cells.Add(cell.ToString());
                    cell.Clear();
                    records.Add(cells);
                    cells = [];
                    break;
                default: cell.Append(c); break;
            }
        }
        if (cell.Length > 0 || cells.Count > 0)
        {
            cells.Add(cell.ToString());
            records.Add(cells);
        }
        return records;
    }
}
