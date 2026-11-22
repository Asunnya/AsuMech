using AsuMech.Core.Native.Interfaces;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Ucob;

public enum NaelMove { LunarDynamo, IronChariot, ThermionicBeam, RavenDive, MeteorStream, DalamudDive }

// Text is the English line, for when the client has no NpcYell row to read.
public sealed record NaelQuote(uint YellRow, string Text, NaelMove First, NaelMove Second);

public static class UcobNaelQuotes
{
    // How long retail keeps the balloon up.
    public const float BalloonSeconds = 4f;

    // Over Nael's head and in the NPC announcement channel, in the client's language, as retail shows it.
    public static void Say(SimWorld world, SimEnemy? nael, uint yellRow, string englishText)
    {
        var text = Natives.Data.NpcYell(yellRow) ?? englishText;
        var speaker = Natives.Data.BNpcName(UcobConstants.BNpcNameId.NaelDeusDarnus) ?? "Nael deus Darnus";
        world.NpcSay(speaker, text.Replace('\n', ' '));
        nael?.Yell(text, BalloonSeconds);
    }
}
