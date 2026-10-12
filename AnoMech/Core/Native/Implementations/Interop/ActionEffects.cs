namespace AnoMech.Core.Native.Implementations.Interop;

internal static class ActionEffects
{
    // The server's per-action id, unique and non-zero: an EffectResult finds its ActionEffect by it.
    private static uint globalSequence = 1;

    public static uint NextGlobalSequence() => globalSequence++;
}
