using System.Collections.Generic;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

namespace AnoMech.Windows.Learn;

// Job icons by the three-letter abbreviation toolbox plans use. The icon id is 062100 + ClassJob row.
internal static class JobIcons
{
    private const uint IconBase = 062100;

    private static readonly Dictionary<string, uint> Rows = new()
    {
        ["PLD"] = 19, ["MNK"] = 20, ["WAR"] = 21, ["DRG"] = 22, ["BRD"] = 23, ["WHM"] = 24, ["BLM"] = 25,
        ["SMN"] = 27, ["SCH"] = 28, ["NIN"] = 30, ["MCH"] = 31, ["DRK"] = 32, ["AST"] = 33, ["SAM"] = 34,
        ["RDM"] = 35, ["BLU"] = 36, ["GNB"] = 37, ["DNC"] = 38, ["RPR"] = 39, ["SGE"] = 40, ["VPR"] = 41, ["PCT"] = 42,
    };

    // Null until the texture is loaded or when the job is unknown; callers draw a fallback then.
    public static IDalamudTextureWrap? Get(string? job)
    {
        if (job is null || !Rows.TryGetValue(job.ToUpperInvariant(), out var row)) return null;
        var texture = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(IconBase + row));
        return texture.TryGetWrap(out var wrap, out _) ? wrap : null;
    }
}
