using AnoMech.Core.Native.Implementations.Pointers;

namespace AnoMech.Core.Native.Implementations;

// The local player's recast groups, set the way the server does: its full recast-times packet.
internal static unsafe class RecastTimes
{
    // What the server sends at zone-in: every group 0 / 0.
    public static void ClearAll()
    {
        if (Plugin.ObjectTable.LocalPlayer is not { } player) return;
        var packet = new UpdateRecastTimesPacket();
        PacketDispatcherPointers.HandleUpdateRecastTimesPacket(player.EntityId, &packet);
    }
}
