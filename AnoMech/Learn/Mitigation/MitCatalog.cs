using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Learn.Mitigation;

public enum MitTarget { Self, Party, Enemy, Ally }

// Durations are curated, UNVERIFIED in game unless noted; recast comes from the Action sheet.
public sealed record MitAction(uint Id, string Name, string[] Jobs, float DurationSeconds, MitTarget Target);

// The mitigation and support actions the plans speak of, one row per action.
public static class MitCatalog
{
    private static readonly string[] Tanks = ["PLD", "WAR", "DRK", "GNB"];
    private static readonly string[] Melee = ["MNK", "DRG", "NIN", "SAM", "RPR", "VPR"];
    private static readonly string[] PhysRanged = ["BRD", "MCH", "DNC"];
    private static readonly string[] Casters = ["BLM", "SMN", "RDM", "PCT"];
    private static readonly string[] Healers = ["WHM", "SCH", "AST", "SGE"];

    public static readonly IReadOnlyList<MitAction> All =
    [
        // Role actions
        new(7535, "Reprisal", Tanks, 15f, MitTarget.Enemy),
        new(7531, "Rampart", Tanks, 20f, MitTarget.Self),
        new(7548, "Arm's Length", [.. Tanks, .. Melee, .. PhysRanged], 6f, MitTarget.Self),
        new(7549, "Feint", Melee, 15f, MitTarget.Enemy),
        new(7560, "Addle", Casters, 15f, MitTarget.Enemy),
        new(7559, "Surecast", [.. Casters, .. Healers], 6f, MitTarget.Self),
        new(7541, "Second Wind", [.. Melee, .. PhysRanged], 0f, MitTarget.Self),
        new(7542, "Bloodbath", Melee, 20f, MitTarget.Self),
        // PLD
        new(3542, "Sheltron", ["PLD"], 8f, MitTarget.Self),
        new(25746, "Holy Sheltron", ["PLD"], 8f, MitTarget.Self),
        new(7382, "Intervention", ["PLD"], 8f, MitTarget.Ally),
        new(17, "Sentinel", ["PLD"], 15f, MitTarget.Self),
        new(36920, "Guardian", ["PLD"], 15f, MitTarget.Self),
        new(3540, "Divine Veil", ["PLD"], 30f, MitTarget.Party),
        new(7385, "Passage of Arms", ["PLD"], 18f, MitTarget.Party),
        new(30, "Hallowed Ground", ["PLD"], 10f, MitTarget.Self),
        new(22, "Bulwark", ["PLD"], 10f, MitTarget.Self),
        new(3541, "Clemency", ["PLD"], 0f, MitTarget.Ally),
        // WAR
        new(3551, "Raw Intuition", ["WAR"], 6f, MitTarget.Self),
        new(25751, "Bloodwhetting", ["WAR"], 8f, MitTarget.Self),
        new(16464, "Nascent Flash", ["WAR"], 8f, MitTarget.Ally),
        new(44, "Vengeance", ["WAR"], 15f, MitTarget.Self),
        new(36923, "Damnation", ["WAR"], 15f, MitTarget.Self),
        new(7388, "Shake It Off", ["WAR"], 30f, MitTarget.Party),
        new(40, "Thrill of Battle", ["WAR"], 10f, MitTarget.Self),
        new(3552, "Equilibrium", ["WAR"], 0f, MitTarget.Self),
        new(43, "Holmgang", ["WAR"], 10f, MitTarget.Self),
        // DRK
        new(7393, "The Blackest Night", ["DRK"], 7f, MitTarget.Ally),
        new(25754, "Oblation", ["DRK"], 10f, MitTarget.Ally),
        new(3636, "Shadow Wall", ["DRK"], 15f, MitTarget.Self),
        new(36927, "Shadowed Vigil", ["DRK"], 15f, MitTarget.Self),
        new(3634, "Dark Mind", ["DRK"], 10f, MitTarget.Self),
        new(16471, "Dark Missionary", ["DRK"], 15f, MitTarget.Party),
        new(3638, "Living Dead", ["DRK"], 10f, MitTarget.Self),
        // GNB
        new(16161, "Heart of Stone", ["GNB"], 7f, MitTarget.Ally),
        new(25758, "Heart of Corundum", ["GNB"], 8f, MitTarget.Ally),
        new(16148, "Nebula", ["GNB"], 15f, MitTarget.Self),
        new(36935, "Great Nebula", ["GNB"], 15f, MitTarget.Self),
        new(16140, "Camouflage", ["GNB"], 20f, MitTarget.Self),
        new(16160, "Heart of Light", ["GNB"], 15f, MitTarget.Party),
        new(16151, "Aurora", ["GNB"], 18f, MitTarget.Ally),
        new(16152, "Superbolide", ["GNB"], 10f, MitTarget.Self),
        // SCH
        new(188, "Sacred Soil", ["SCH"], 15f, MitTarget.Party),
        new(16538, "Fey Illumination", ["SCH"], 20f, MitTarget.Party),
        new(25868, "Expedient", ["SCH"], 20f, MitTarget.Party),
        new(7434, "Excogitation", ["SCH"], 45f, MitTarget.Ally),
        new(3585, "Deployment Tactics", ["SCH"], 0f, MitTarget.Ally),
        new(16537, "Whispering Dawn", ["SCH"], 21f, MitTarget.Party),
        new(7436, "Chain Stratagem", ["SCH"], 20f, MitTarget.Enemy),
        new(185, "Adloquium", ["SCH"], 30f, MitTarget.Ally),
        new(16545, "Summon Seraph", ["SCH"], 22f, MitTarget.Party),
        new(16546, "Consolation", ["SCH"], 30f, MitTarget.Party),
        new(189, "Lustrate", ["SCH"], 0f, MitTarget.Ally),
        new(16542, "Recitation", ["SCH"], 15f, MitTarget.Self),
        new(25867, "Protraction", ["SCH"], 10f, MitTarget.Ally),
        new(186, "Succor", ["SCH"], 30f, MitTarget.Party),
        new(37013, "Concitation", ["SCH"], 30f, MitTarget.Party),
        new(7438, "Fey Union", ["SCH"], 0f, MitTarget.Ally),
        // SGE
        new(24298, "Kerachole", ["SGE"], 15f, MitTarget.Party),
        new(24310, "Holos", ["SGE"], 20f, MitTarget.Party),
        new(24305, "Haima", ["SGE"], 15f, MitTarget.Ally),
        new(24311, "Panhaima", ["SGE"], 15f, MitTarget.Party),
        new(24302, "Physis II", ["SGE"], 15f, MitTarget.Party),
        new(24300, "Zoe", ["SGE"], 30f, MitTarget.Self),
        new(37034, "Eukrasian Prognosis II", ["SGE"], 30f, MitTarget.Party),
        new(24303, "Taurochole", ["SGE"], 15f, MitTarget.Ally),
        new(24296, "Druochole", ["SGE"], 0f, MitTarget.Ally),
        new(24294, "Soteria", ["SGE"], 15f, MitTarget.Self),
        new(24317, "Krasis", ["SGE"], 10f, MitTarget.Ally),
        new(37035, "Philosophia", ["SGE"], 20f, MitTarget.Party),
        // WHM
        new(3569, "Asylum", ["WHM"], 24f, MitTarget.Party),
        new(7433, "Plenary Indulgence", ["WHM"], 10f, MitTarget.Party),
        new(16536, "Temperance", ["WHM"], 20f, MitTarget.Party),
        new(37011, "Divine Caress", ["WHM"], 10f, MitTarget.Party),
        new(3571, "Assize", ["WHM"], 0f, MitTarget.Party),
        new(140, "Benediction", ["WHM"], 0f, MitTarget.Ally),
        new(137, "Regen", ["WHM"], 18f, MitTarget.Ally),
        new(7432, "Divine Benison", ["WHM"], 15f, MitTarget.Ally),
        new(25861, "Aquaveil", ["WHM"], 8f, MitTarget.Ally),
        new(25862, "Liturgy of the Bell", ["WHM"], 20f, MitTarget.Party),
        // AST
        new(16553, "Celestial Opposition", ["AST"], 15f, MitTarget.Party),
        new(3613, "Collective Unconscious", ["AST"], 18f, MitTarget.Party),
        new(16559, "Neutral Sect", ["AST"], 20f, MitTarget.Party),
        new(7439, "Earthly Star", ["AST"], 20f, MitTarget.Party),
        new(8324, "Stellar Detonation", ["AST"], 0f, MitTarget.Party),
        new(3595, "Aspected Benefic", ["AST"], 15f, MitTarget.Ally),
        new(16556, "Celestial Intersection", ["AST"], 30f, MitTarget.Ally),
        new(25873, "Exaltation", ["AST"], 8f, MitTarget.Ally),
        new(25874, "Macrocosmos", ["AST"], 15f, MitTarget.Party),
        new(37031, "Sun Sign", ["AST"], 15f, MitTarget.Party),
        new(16552, "Divination", ["AST"], 20f, MitTarget.Party),
        // DPS support
        new(7405, "Troubadour", ["BRD"], 15f, MitTarget.Party),
        new(16889, "Tactician", ["MCH"], 15f, MitTarget.Party),
        new(16012, "Shield Samba", ["DNC"], 15f, MitTarget.Party),
        new(16015, "Curing Waltz", ["DNC"], 0f, MitTarget.Party),
        new(7408, "Nature's Minne", ["BRD"], 15f, MitTarget.Party),
        new(65, "Mantra", ["MNK"], 15f, MitTarget.Party),
        new(2887, "Dismantle", ["MCH"], 10f, MitTarget.Enemy),
        new(25857, "Magick Barrier", ["RDM"], 10f, MitTarget.Party),
        new(34685, "Tempera Coat", ["PCT"], 10f, MitTarget.Self),
        new(34686, "Tempera Grassa", ["PCT"], 10f, MitTarget.Party),
        // Tank limit breaks
        new(197, "Shield Wall", Tanks, 10f, MitTarget.Party),
        new(198, "Stronghold", Tanks, 10f, MitTarget.Party),
        new(199, "Last Bastion", ["PLD"], 8f, MitTarget.Party),
        new(4240, "Land Waker", ["WAR"], 8f, MitTarget.Party),
        new(4241, "Dark Force", ["DRK"], 8f, MitTarget.Party),
        new(17105, "Gunmetal Soul", ["GNB"], 8f, MitTarget.Party),
    ];

    private static readonly Dictionary<string, MitAction> ByName = All.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<uint, MitAction> ById = All.ToDictionary(a => a.Id);

    public static MitAction? Find(string name) => ByName.GetValueOrDefault(name);
    public static MitAction? Find(uint id) => ById.GetValueOrDefault(id);
    public static IEnumerable<MitAction> ForJob(string job) => All.Where(a => a.Jobs.Contains(job, StringComparer.OrdinalIgnoreCase));
}
