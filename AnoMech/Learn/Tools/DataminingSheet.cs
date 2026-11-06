using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AnoMech.Learn.Tools;

// One xivapi/ffxiv-datamining CSV (csv/en/<Sheet>.csv): the first record names the columns, every
// record after is a row keyed by its first field. Quoted fields may hold commas, quotes and line
// breaks. Rows are split as they are asked for, since Action.csv is large and a diff reads a few hundred.
public sealed class DataminingSheet
{
    public sealed record Row(DataminingSheet Sheet, string[] Fields)
    {
        public string Text(string column) => Fields[Sheet.Column(column)];
        public bool Has(string column) => Sheet.columns.ContainsKey(column) && Sheet.Column(column) < Fields.Length;
    }

    private readonly string name;
    private readonly string text;
    private readonly Dictionary<string, int> columns = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, int> rows = [];
    private readonly Dictionary<uint, Row> parsed = [];

    private DataminingSheet(string name, string text)
    {
        this.name = name;
        this.text = text;
    }

    public string Name => name;
    public IEnumerable<uint> Ids => rows.Keys;
    public IReadOnlyCollection<string> Columns => columns.Keys;

    public static DataminingSheet Parse(string name, string text)
    {
        var sheet = new DataminingSheet(name, text);
        var pos = 0;
        var header = ReadRecord(text, ref pos);
        for (var i = 0; i < header.Length; i++) sheet.columns.TryAdd(header[i], i);
        while (pos < text.Length)
        {
            var start = pos;
            var end = pos;
            while (end < text.Length && text[end] is not (',' or '\r' or '\n')) end++;
            if (uint.TryParse(text.AsSpan(start, end - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) sheet.rows.TryAdd(id, start);
            ReadRecord(text, ref pos);
        }
        return sheet;
    }

    public Row? Get(uint id)
    {
        if (parsed.TryGetValue(id, out var row)) return row;
        if (!rows.TryGetValue(id, out var start)) return null;
        var pos = start;
        row = new Row(this, ReadRecord(text, ref pos));
        parsed[id] = row;
        return row;
    }

    public int Column(string column)
        => columns.TryGetValue(column, out var index) ? index : throw new KeyNotFoundException($"{name}.csv has no column '{column}'.");

    private static string[] ReadRecord(string text, ref int pos)
    {
        var fields = new List<string>();
        while (true)
        {
            if (pos < text.Length && text[pos] == '"')
            {
                var field = new StringBuilder();
                for (pos++; pos < text.Length; pos++)
                {
                    if (text[pos] != '"') field.Append(text[pos]);
                    else if (pos + 1 < text.Length && text[pos + 1] == '"') field.Append(text[++pos]);
                    else break;
                }
                pos++;
                fields.Add(field.ToString());
            }
            else
            {
                var start = pos;
                while (pos < text.Length && text[pos] is not (',' or '\r' or '\n')) pos++;
                fields.Add(text[start..pos]);
            }
            if (pos < text.Length && text[pos] == ',') { pos++; continue; }
            if (pos < text.Length && text[pos] == '\r') pos++;
            if (pos < text.Length && text[pos] == '\n') pos++;
            return fields.ToArray();
        }
    }

    // The sheet at one commit of xivapi/ffxiv-datamining, cached under `cacheDir/<commit>/`.
    public static async Task<DataminingSheet> FetchAsync(string commit, string sheet, string cacheDir, HttpClient? http = null)
    {
        var dir = Path.Combine(cacheDir, commit);
        var path = Path.Combine(dir, $"{sheet}.csv");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(dir);
            using var client = http ?? new HttpClient();
            // The English sheets moved under csv/en/ in 2025; older commits keep them in csv/.
            string text;
            try { text = await client.GetStringAsync($"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/{commit}/csv/en/{sheet}.csv").ConfigureAwait(false); }
            catch (HttpRequestException) { text = await client.GetStringAsync($"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/{commit}/csv/{sheet}.csv").ConfigureAwait(false); }
            await File.WriteAllTextAsync(path + ".part", text).ConfigureAwait(false);
            File.Move(path + ".part", path, overwrite: true);
        }
        return Parse(sheet, await File.ReadAllTextAsync(path).ConfigureAwait(false));
    }
}
