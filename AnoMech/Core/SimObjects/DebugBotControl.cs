namespace AnoMech.Core.SimObjects;

// Debug-only: when true, the orders a strat gives the local SimPlayer (MoveTo, Follow, Intercept,
// TurnTo) drive the real local character as movement input, the way they walk a bot; otherwise
// PlayerMotion drops them. The seam MultiplayerManager's debug-bot-controlled mode flips through.
// A plain static flag is enough since there is ever only one local SimPlayer.
internal static class DebugBotControl
{
    public static bool Enabled { get; set; }
}
