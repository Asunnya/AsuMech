using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core;

// Resolves a Knockback-sheet row id directly to its Distance/Speed pair.
// Scenarios get the row id from their parser-emitted timeline (the parser
// decodes it from the corresponding ACT type-22 effect entry) and pass it
// straight to SimParty/ISimPartyMember — no intermediate action-id table.
internal static class KnockbackLookup
{
    public static bool TryGet(uint knockbackId, out float distance, out float speed)
    {
        if (Natives.Data.Knockback(knockbackId) is not { } row)
        {
            Plugin.Log.Warning($"KnockbackLookup: Knockback row {knockbackId} missing in sheet");
            distance = 0f;
            speed = 0f;
            return false;
        }
        distance = row.Distance;
        speed = row.Speed;
        return true;
    }

    // The client pushes its own player from an ActionEffect's knockback entry for every Direction
    // but 6 (away from the action's target), which the server sends as the push packet instead.
    public static bool ClientApplies(uint knockbackId) => Natives.Data.Knockback(knockbackId) is { Direction: <= 5 };
}
