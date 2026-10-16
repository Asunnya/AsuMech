namespace AnoMech.Core.Native.Interfaces;

public sealed record StatusRow(ushort Id, string Name, bool LockMovement, bool LockActions, bool LockControl, byte MaxStacks);
