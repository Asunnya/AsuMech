using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsuMech.Learn.Tools;

public enum PinStatus { Ok, Shifts, NoRoundTrip, NotPinnable }

public sealed record PinRow(string Roll, PinStatus Status, string Detail);

// What pinning each roll of a scenario does to the seed's other rolls (overrides-check).
public sealed record PinReport(string Scenario, int Seed, List<PinRow> Rows, string? Error)
{
    public bool Clean => Error is null && Rows.All(r => r.Status is PinStatus.Ok or PinStatus.NotPinnable);

    public string Summary()
    {
        if (Error != null) return Error;
        var ok = Rows.Count(r => r.Status == PinStatus.Ok);
        var shifts = Rows.Where(r => r.Status == PinStatus.Shifts).Select(r => r.Roll).ToList();
        var noRoundTrip = Rows.Where(r => r.Status == PinStatus.NoRoundTrip).Select(r => r.Roll).ToList();
        var unpinnable = Rows.Where(r => r.Status == PinStatus.NotPinnable).Select(r => r.Roll).ToList();
        var sb = new StringBuilder($"{ok} roll(s) pin cleanly");
        if (shifts.Count > 0) sb.Append($"; pinning shifts other rolls: {string.Join(", ", shifts)} (the State short-circuits the draw)");
        if (noRoundTrip.Count > 0) sb.Append($"; pin does not read back: {string.Join(", ", noRoundTrip)}");
        if (unpinnable.Count > 0) sb.Append($"; not pinnable by name: {string.Join(", ", unpinnable)}");
        return sb.ToString();
    }

    public string Table()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Scenario} seed {Seed}: {Summary()}");
        foreach (var r in Rows) sb.AppendLine($"  {r.Status,-12} {r.Roll,-28} {r.Detail}");
        return sb.ToString();
    }
}
