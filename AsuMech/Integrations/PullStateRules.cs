namespace AsuMech.Integrations;

// Frame by frame, what a stay shows other plugins. A restart ends one pull and begins the next on
// the same frame, but Splatoon resets its scripts on InCombat's falling edge, polled once a frame:
// the flag reads false for a few frames between two runs, or the restart is invisible to it.
internal sealed class PullStateRules
{
    public const int GapFrames = 3;

    private object? lastRun;
    private int gapLeft;

    public readonly record struct Frame(bool InDuty, bool InCombat);

    // run: the current run, compared by identity; running: it is still being fought.
    public Frame? Tick(bool enabled, bool inZone, object? run, bool running)
    {
        if (!enabled || !inZone)
        {
            lastRun = null;
            gapLeft = 0;
            return null;
        }
        if (running && run != null && !ReferenceEquals(run, lastRun))
        {
            if (lastRun != null) gapLeft = GapFrames;
            lastRun = run;
        }
        if (gapLeft > 0)
        {
            gapLeft--;
            return new Frame(InDuty: true, InCombat: false);
        }
        return new Frame(InDuty: true, InCombat: running);
    }
}
