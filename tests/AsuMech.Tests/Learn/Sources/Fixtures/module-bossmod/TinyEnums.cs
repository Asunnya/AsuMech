namespace BossMod.Dawntrail.Savage.Tiny;

public enum OID : uint
{
    Boss = 0x4AEC, // R5.000, x1
    Helper = 0x233C, // R0.500, x25-36, Helper type
}

public enum AID : uint
{
    AutoAttack = 46085, // Boss->player, no cast, single-target
    CrownOfArcadia = 46086, // Boss->self, 5.0s cast, range 60 circle
    RawSteelTrophyAxe = 46114, // Boss->self, 2.0+4.0s cast, single-target
    RawSteelScytheHeavyHitter = 46096, // Helper->self, no cast, range 60 45-degree cone, party stack
    AssaultEvolvedScytheAOE = 46105, // Helper->self, 2.0s cast, range 5-60 donut
    AssaultEvolvedSwordAOE = 46106, // Helper->self, 2.0s cast, range 40 width 10 cross
    SharpTaste = 46109, // Helper->self, no cast, range 60 width 6 rect, sword
    Mystery = 46999, // something the parser can't read
    Bare = 46998,
}

public enum SID : uint
{
    PhysicalVulnerabilityUp = 2940, // applied by RawSteel
}

public enum IconID : uint
{
    Stack = 161, // player
    Spread = 139, // player
}

public enum TetherID : uint
{
    ForegoneFatality = 356, // Comet->player
}
