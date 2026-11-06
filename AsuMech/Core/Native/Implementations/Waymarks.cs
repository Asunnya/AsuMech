using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Native.Implementations.Pointers;
using AsuMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AsuMech.Core.Native.Implementations;

internal sealed unsafe class Waymarks : IWaymarks
{
    public bool Place(IReadOnlyList<(WaymarkSlot Slot, Vector3 Position)> markers)
    {
        var packet = new FieldMarkerPresetPacket();
        foreach (var (slot, position) in markers)
        {
            var idx = (int)slot;
            if (idx < 0 || idx >= FieldMarkerPresetPacket.Capacity) continue;
            packet.ActiveMask |= (byte)(1 << idx);
            packet.X[idx] = (int)MathF.Round(position.X * FieldMarkerPresetPacket.PositionScale);
            packet.Y[idx] = (int)MathF.Round(position.Y * FieldMarkerPresetPacket.PositionScale);
            packet.Z[idx] = (int)MathF.Round(position.Z * FieldMarkerPresetPacket.PositionScale);
        }
        return Receive(&packet);
    }

    public void ClearAll()
    {
        var packet = new FieldMarkerPresetPacket();
        Receive(&packet);
    }

    private static bool Receive(FieldMarkerPresetPacket* packet)
    {
        var controller = MarkingController.Instance();
        if (controller == null) return false;
        PacketDispatcherPointers.HandleFieldMarkerPresetPacket(controller, packet);
        return true;
    }
}
