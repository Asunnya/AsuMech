namespace BossMod.Dawntrail.Savage.Tiny;

public enum OID : uint
{
    Boss = 0x4AEC,
    Helper = 0x233C,
}

public enum AID : uint
{
    CrownOfArcadia = 46086, // 4AEC->self, 5.0s cast, range 60 circle
    RawSteel_HeavyHitter = 46096, // 233C->self, no cast, range 60 ?-degree cone ; Party stack cone for RawSteelTrophy
    AssaultEvolved_ScytheDonut = 46105, // 233C->self, 2.0s cast, range 5-60 donut ; Donut AOE after jumping to scythe
    Cometite = 46099, // 233C->location, 3.0s cast, range 6 circle ; these are the multi puddle meteors
    Flatliner1 = 47760, // 233C->self, 6.0s cast, range 60 circle
}

public enum IconID : uint
{
    CometIcon = 139, // player
    CrushingCometIcon = 161, // player
}

public enum TetherID : uint
{
    Tether57 = 57, // player->player
}
