using System;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.Game;

// A Start held back while something settles (IZoneControl.StartBlockedReason's `settling`).
// Once nothing is settling the held start runs through the caller's own path, whose checks
// report anything else still in the way.
public sealed class SettleWait
{
    private Action? start;

    public string? WaitingOn { get; private set; }

    public void Hold(string settling, Action start)
    {
        this.start = start;
        WaitingOn = settling;
    }

    public void Cancel()
    {
        start = null;
        WaitingOn = null;
    }

    // True when WaitingOn changed.
    public bool Tick()
    {
        if (start is not { } pending) return false;
        if (Natives.Zone.StartBlockedReason(out var settling) != null && settling != null)
        {
            if (settling == WaitingOn) return false;
            WaitingOn = settling;
            return true;
        }
        Cancel();
        pending();
        return true;
    }
}
