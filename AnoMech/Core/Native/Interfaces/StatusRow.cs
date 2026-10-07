namespace AnoMech.Core.Native.Interfaces;

public sealed record StatusRow(ushort Id, bool LockMovement, bool LockActions, bool LockControl);
