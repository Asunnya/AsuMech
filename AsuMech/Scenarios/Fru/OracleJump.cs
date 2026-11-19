using System.Numerics;

namespace AsuMech.Scenarios.Fru;

// Where the Oracle lands when Spirit Taker or Somber Dance jumps her at a player.
public static class OracleJump
{
    // UNVERIFIED: she stays for targets up to 6.9y away and jumps for 11y and beyond.
    private const float StaysWithin = 7.5f;
    private const float LandsShort = 4.5f;

    public static Vector3 Landing(Vector3 from, Vector3 target)
    {
        var offset = target - from;
        return offset.Length() <= StaysWithin ? from : target - Vector3.Normalize(offset) * LandsShort;
    }
}
