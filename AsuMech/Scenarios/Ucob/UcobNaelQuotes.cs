using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Ucob;

public enum NaelMove { LunarDynamo, IronChariot, ThermionicBeam, RavenDive, MeteorStream, DalamudDive }

public sealed record NaelQuote(string Text, NaelMove First, NaelMove Second);

public static class UcobNaelQuotes
{
    // How long retail keeps the balloon up.
    public const float BalloonSeconds = 4f;

    // Over Nael's head as retail shows it, and in chat.
    public static void Say(SimWorld world, SimEnemy? nael, string text)
    {
        world.Announce($"Nael deus Darnus: {text}");
        nael?.Yell(text, BalloonSeconds);
    }
}
