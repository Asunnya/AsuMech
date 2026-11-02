using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Learn.Mitigation;

// An expected use in this run: which actions, between which scenario seconds, on whom.
public sealed record MitExpectation(string MechanicId, string Label, MitItem Item, List<uint> AcceptIds, float Opens, float Closes, string? Target, int Index);

// Resolves a plan column into expectations for one scenario run. Healers use the column of their
// job; everyone else the column of their seat. Windows open as early as the effect would still
// cover the hit, and close at the hit.
public static class MitPlanResolver
{
    public const float DefaultDurationSeconds = 15f;
    public const float InstantSlackSeconds = 2.5f;

    public static string? ColumnFor(string job, string seat) => job.ToUpperInvariant() switch
    {
        "SCH" => "Scholar", "SGE" => "Sage", "WHM" => "White Mage", "AST" => "Astrologian",
        _ => seat switch
        {
            "MainTank" => "MT", "OffTank" => "OT", "MeleeDpsA" => "Melee 1", "MeleeDpsB" => "Melee 2",
            "PhysRangedDps" => "Phys Range", "CasterDps" => "Caster", _ => null,
        },
    };

    public static List<MitExpectation> Resolve(MitScenario scenario, CastTimeline timeline, string column, string job)
    {
        var result = new List<MitExpectation>();
        var index = 0;
        foreach (var mech in scenario.Mechanics)
        {
            if (mech.Anchor is not { } anchor || timeline.Find(anchor.Cast, anchor.Occurrence) is not { } cast) continue;
            if (!mech.Columns.TryGetValue(column, out var items)) continue;
            foreach (var item in items)
            {
                var ids = (item.Accept ?? []).Select(MitCatalog.Find).OfType<MitAction>()
                    .Where(a => a.Jobs.Contains(job, StringComparer.OrdinalIgnoreCase)).Select(a => a.Id).ToList();
                var duration = (item.Accept ?? []).Select(MitCatalog.Find).OfType<MitAction>().Select(a => a.DurationSeconds).DefaultIfEmpty(DefaultDurationSeconds).Max();
                if (duration <= 0f) duration = InstantSlackSeconds;
                var opens = cast.Start - duration;
                var closes = cast.Resolve;
                if (item.When == "afterCastEnd") { opens = cast.Resolve; closes = cast.Resolve + duration; }
                result.Add(new MitExpectation(mech.Id, mech.Label, item, ids, opens, closes, item.Target, index++));
            }
        }
        return result;
    }
}
