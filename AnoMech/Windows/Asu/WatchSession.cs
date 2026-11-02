using System;

namespace AnoMech.Windows.Asu;

// The state Watch borrows from the player and must give back.
internal interface IWatchTarget
{
    bool BotControl { get; set; }
    float Speed { get; set; }
    bool Counting { get; set; }
}

internal sealed class GameWatchTarget : IWatchTarget
{
    public bool BotControl
    {
        get => Core.SimObjects.DebugBotControl.Enabled;
        set => Core.SimObjects.DebugBotControl.Enabled = value;
    }

    public float Speed
    {
        get => Plugin.Game.EventTimeScale;
        set => Plugin.Game.EventTimeScale = value;
    }

    public bool Counting
    {
        get => Plugin.Practice.Counting;
        set => Plugin.Practice.Counting = value;
    }
}

// A run the strat's AI plays in the player's seat, slowed down, kept out of the streak. Every way
// out restores what it changed: the run ending, a stop, the window or plugin going away.
internal sealed class WatchSession : IDisposable
{
    public const float WatchSpeed = 0.5f;
    public const float MaxSecondsToStart = 60f;

    private readonly IWatchTarget target;
    private readonly bool botControl;
    private readonly float speed;
    private readonly bool counting;
    private bool sawLive;
    private float waited;

    public bool Ended { get; private set; }

    public WatchSession(IWatchTarget target)
    {
        this.target = target;
        botControl = target.BotControl;
        speed = target.Speed;
        counting = target.Counting;
        target.BotControl = true;
        target.Speed = WatchSpeed;
        target.Counting = false;
    }

    // Once per frame. A run that went live and stopped, or never started, ends the watch.
    public void Tick(bool runLive, float deltaSeconds)
    {
        if (Ended) return;
        if (runLive)
        {
            sawLive = true;
            return;
        }
        waited += deltaSeconds;
        if (sawLive || waited > MaxSecondsToStart) Dispose();
    }

    public void Dispose()
    {
        if (Ended) return;
        Ended = true;
        target.BotControl = botControl;
        target.Speed = speed;
        target.Counting = counting;
    }
}
