using System;
using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Core.Game.Party;

// The party's faked limit break gauge. Solo in the inn the real gauge is empty, so Set sends the
// local player the gauge's ActorControlSelf, for display only: the real values are saved once and
// sent back when the party despawns. The client gates presses on it; the request packet is eaten
// by the firewall.
public sealed class LimitBreakGauge
{
    private const ushort UnitsPerBar = 10000;
    private const byte Bars = 3;

    private readonly Func<ActorControl?> player;
    private ushort units;
    private LimitBreakBars? saved;

    internal LimitBreakGauge(Func<ActorControl?> player) => this.player = player;

    public int FilledBars => units / UnitsPerBar;

    // The local player's own limit break as it resolves, and where it was aimed: a scenario reacts
    // to what it did (where a caster LB3 came down, which add a melee LB3 hit).
    public event Action<uint, LimitBreakAim>? Landed;

    internal void Land(uint actionId, LimitBreakAim aim) => Landed?.Invoke(actionId, aim);

    public void Set(float bars)
    {
        units = (ushort)(Math.Clamp(bars, 0f, Bars) * UnitsPerBar);
        saved ??= Natives.LimitBreak.Read();
        player()?.SetLimitBreakGauge(Bars, units, UnitsPerBar);
    }

    internal void Spend()
    {
        units = 0;
        player()?.SetLimitBreakGauge(Bars, units, UnitsPerBar);
    }

    internal void Restore()
    {
        units = 0;
        if (saved is not { } s) return;
        saved = null;
        player()?.SetLimitBreakGauge(s.BarCount, s.CurrentUnits, s.BarUnits);
    }
}
