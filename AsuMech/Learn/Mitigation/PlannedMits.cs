using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;

namespace AsuMech.Learn.Mitigation;

// One mitigation a bot applies for the plan: at `Time`, the bot in `Role` grants `Status` to the
// action's target kind (the casting enemy, the whole party, itself, or the plan's ally).
public sealed record PlannedMit(float Time, PartyRole Role, string Action, MitStatus Status, MitTarget Target, string? Ally, uint AnchorActionId);

// The plan's other columns, played by the bots: each expectation in a column a bot fills becomes
// a status applied just before the cast resolves, with the action's catalog duration, so the
// player's damage checks see the party mitigated as the plan says. The player's own column is
// theirs to press.
public static class PlannedMits
{
    public const float LeadSeconds = 1f;

    public static List<PlannedMit> Schedule(MitScenario scenario, CastTimeline timeline, IReadOnlyDictionary<PartyRole, string> jobs, PartyRole? player)
    {
        var result = new List<PlannedMit>();
        foreach (var (role, job) in jobs)
        {
            if (role == player) continue;
            var column = MitPlanResolver.ColumnFor(job, role.ToString());
            if (column is null) continue;
            foreach (var expectation in MitPlanResolver.Resolve(scenario, timeline, column, job))
            {
                var action = expectation.AcceptIds.Select(MitCatalog.Find).OfType<MitAction>().FirstOrDefault(a => MitStatuses.ForAction(a.Name) != null);
                if (action is null) continue;
                var status = MitStatuses.ForAction(action.Name)!;
                var anchor = scenario.Mechanics.FirstOrDefault(m => m.Id == expectation.MechanicId)?.Anchor;
                var cast = anchor is null ? null : timeline.Find(anchor.Cast, anchor.Occurrence);
                var time = Math.Max(expectation.Opens, expectation.Closes - LeadSeconds);
                result.Add(new PlannedMit(time, role, action.Name, status, action.Target, expectation.Target, cast?.ActionId ?? 0));
            }
        }
        return result.OrderBy(m => m.Time).ToList();
    }
}
