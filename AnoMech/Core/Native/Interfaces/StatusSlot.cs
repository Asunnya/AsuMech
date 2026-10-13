namespace AnoMech.Core.Native.Interfaces;

// One StatusManager slot as the server's status-list packet carries it. RemainingTime 0 never
// expires; StatusId 0 is an empty slot.
public readonly record struct StatusSlot(ushort StatusId, ushort Param, float RemainingTime, uint SourceEntityId);
