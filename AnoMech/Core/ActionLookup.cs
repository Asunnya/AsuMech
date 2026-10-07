using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core;

// Resolves an Action-sheet row id to its display name for user-facing messages.
internal static class ActionLookup
{
    // Action name for `actionId`, or the raw id as a fallback when the row is
    // missing or unnamed — death messages must never go blank.
    public static string Name(uint actionId)
        => Natives.Data.Action(actionId) is { Name: { Length: > 0 } name } ? name : actionId.ToString();

    // The sheet name as-is, blank for an unnamed row (enemy auto-attacks): flytext labels mirror
    // what the game prints.
    public static string FlyTextName(uint actionId) => Natives.Data.Action(actionId)?.Name ?? "";
}
