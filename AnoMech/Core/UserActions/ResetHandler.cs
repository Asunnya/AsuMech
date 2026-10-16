using System;
using AnoMech.Core.Native.Implementations;

namespace AnoMech.Core.UserActions;

// What the server resets for a fresh pull: every recast cleared, as it does at zone-in, and the
// job gauge emptied, then seeded from JobActions.StartingResources.
internal sealed class ResetHandler : IUserActionHandler
{
    private readonly Random rng = new();

    public bool RunsWhenDisabled => true;

    public void OnScenarioStart()
    {
        RecastTimes.ClearAll();
        ClearGauge();
        if (Plugin.Game?.Player is { } player) JobActions.ApplyStartingResources(player, rng);
    }

    // Its own scope: the seed opens gauge edits of its own.
    private static void ClearGauge()
    {
        using var edit = JobGaugeEdit.Begin();
        edit.Clear();
    }
}
