using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Learn.Mitigation;

// The status a mitigation action puts on its target, for the bots to apply a plan with. Ids
// are the Status sheet's rows of that name whose description says damage is reduced or a barrier
// stands (a test checks the names); the few names with several such rows keep the one the
// current game grants, UNVERIFIED in game for the ones marked.
public sealed record MitStatus(string Action, ushort StatusId, string StatusName, float DurationSeconds);

public static class MitStatuses
{
    public static readonly IReadOnlyList<MitStatus> All =
    [
        new("Reprisal", 1193, "Reprisal", 15f),
        new("Addle", 1203, "Addle", 15f),
        new("Feint", 1195, "Feint", 15f),
        new("Divine Veil", 1362, "Divine Veil", 30f),
        new("Passage of Arms", 1175, "Passage of Arms", 18f),
        new("Shake It Off", 1457, "Shake It Off", 30f),
        new("Heart of Light", 1839, "Heart of Light", 15f),
        new("Dark Missionary", 1894, "Dark Missionary", 15f),
        new("Intervention", 1174, "Intervention", 8f),
        new("Nascent Flash", 1858, "Nascent Glint", 8f),
        new("Oblation", 2682, "Oblation", 10f),
        new("Temperance", 1873, "Temperance", 20f),
        new("Divine Caress", 3903, "Divine Caress", 10f),
        new("Divine Benison", 1218, "Divine Benison", 15f),
        new("Aquaveil", 2708, "Aquaveil", 8f),
        new("Sacred Soil", 1944, "Sacred Soil", 15f),            // UNVERIFIED: 299 is the older row
        new("Expedient", 2711, "Desperate Measures", 20f),
        new("Fey Illumination", 317, "Fey Illumination", 20f),
        new("Adloquium", 297, "Galvanize", 30f),
        new("Succor", 297, "Galvanize", 30f),
        new("Concitation", 297, "Galvanize", 30f),
        new("Consolation", 3098, "Consolation", 30f),
        new("Collective Unconscious", 849, "Collective Unconscious", 18f),
        new("Exaltation", 2717, "Exaltation", 8f),
        new("Sun Sign", 3896, "Sun Sign", 15f),
        new("Kerachole", 2618, "Kerachole", 15f),
        new("Holos", 3003, "Holos", 20f),
        new("Panhaima", 2613, "Panhaima", 15f),
        new("Haima", 2612, "Haima", 15f),
        new("Taurochole", 2619, "Taurochole", 15f),
        new("Eukrasian Prognosis II", 2609, "Eukrasian Prognosis", 30f), // UNVERIFIED: the II variant may use its own row
        new("Troubadour", 1934, "Troubadour", 15f),
        new("Tactician", 1951, "Tactician", 15f),
        new("Shield Samba", 1826, "Shield Samba", 15f),
        new("Magick Barrier", 2707, "Magick Barrier", 10f),
    ];

    private static readonly Dictionary<string, MitStatus> ByAction = All.ToDictionary(m => m.Action, System.StringComparer.OrdinalIgnoreCase);

    public static MitStatus? ForAction(string actionName) => ByAction.GetValueOrDefault(actionName);
    public static MitStatus? ForAction(uint actionId) => MitCatalog.Find(actionId) is { } a ? ForAction(a.Name) : null;
}
