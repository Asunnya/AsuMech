namespace AnoMech.Core.SimObjects;

// Status ids past the Status sheet, for state the game has no status for (e.g. "already took a
// Mistral Song"). They live only on the SimCharacter: never written to the native StatusManager,
// never sent to peers. Scenarios name theirs as First + n in their own constants.
public static class SimOnlyStatus
{
    public const ushort First = 0xF000;

    public static bool Is(ushort statusId) => statusId >= First;
}
