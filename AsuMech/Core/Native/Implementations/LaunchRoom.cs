using System.Collections.Generic;

namespace AsuMech.Core.Native.Implementations;

// Where a stay may start: a private room the server keeps nobody else in, which the client can
// reload on its own when the stay ends.
internal static class LaunchRoom
{
    // TerritoryIntendedUse.Inn (from ECommons TerritoryIntendedUseEnum).
    public const uint InnIntendedUse = 2;

    // Private Chambers of an FC house: Mist, Lavender Beds, The Goblet, Shirogane, Empyreum.
    // UNVERIFIED that the client-side reload on Leave brings back the furniture and the room's
    // fixtures, which the server sends on a real zone-in; hence opt-in.
    public static readonly IReadOnlySet<uint> FcPrivateChambers = new HashSet<uint> { 384, 385, 386, 652, 983 };

    public static bool Allows(uint territory, uint intendedUse, bool allowFcChambers)
        => intendedUse == InnIntendedUse || (allowFcChambers && FcPrivateChambers.Contains(territory));

    public const string Description = "an inn room";
    public const string DescriptionWithChambers = "an inn room or your FC private chambers";
}
