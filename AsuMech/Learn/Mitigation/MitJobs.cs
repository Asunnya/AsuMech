using AsuMech.Core.UserActions;

namespace AsuMech.Learn.Mitigation;

public static class MitJobs
{
    // The three-letter job the sheets and the catalog speak of; a base class counts as its job.
    public static string? Abbreviation(uint classJob) => (JobId)classJob switch
    {
        JobId.Gladiator or JobId.Paladin => "PLD",
        JobId.Marauder or JobId.Warrior => "WAR",
        JobId.DarkKnight => "DRK",
        JobId.Gunbreaker => "GNB",
        JobId.Conjurer or JobId.WhiteMage => "WHM",
        JobId.Scholar => "SCH",
        JobId.Astrologian => "AST",
        JobId.Sage => "SGE",
        JobId.Pugilist or JobId.Monk => "MNK",
        JobId.Lancer or JobId.Dragoon => "DRG",
        JobId.Rogue or JobId.Ninja => "NIN",
        JobId.Samurai => "SAM",
        JobId.Reaper => "RPR",
        JobId.Viper => "VPR",
        JobId.Archer or JobId.Bard => "BRD",
        JobId.Machinist => "MCH",
        JobId.Dancer => "DNC",
        JobId.Thaumaturge or JobId.BlackMage => "BLM",
        JobId.Arcanist or JobId.Summoner => "SMN",
        JobId.RedMage => "RDM",
        JobId.Pictomancer => "PCT",
        _ => null,
    };
}
