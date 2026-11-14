using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P2Nael;

public sealed class UcobP2NaelTuuflessAi : UcobP2NaelAi, IUcobStrat
{
    public override string Name => "Tuufless (Elemental)";
    public UcobMarkers Markers => UcobMarkers.Elemental;

    private static readonly Vector2 NaelCentredFacingNorth = new(0f, -3.1f);

    private static readonly Dictionary<PartyRole, (float Degrees, float Radius)> FirstBurstSpread = new()
    {
        [MainTank] = (22.5f, 9.4f), [OffTank] = (292.5f, 10f),
        [RegenHealer] = (202.5f, 19f), [ShieldHealer] = (157.5f, 19f),
        [MeleeDpsA] = (247.5f, 9.5f), [MeleeDpsB] = (112.5f, 9.5f),
        [PhysRangedDps] = (247.5f, 19f), [CasterDps] = (112.5f, 19f),
    };

    private static readonly Dictionary<PartyRole, (float Degrees, float Radius)> SecondBurstSpread = new()
    {
        [OffTank] = (315f, 9.8f),
        [RegenHealer] = (225f, 19f), [ShieldHealer] = (135f, 19f),
        [MeleeDpsA] = (270f, 11f), [MeleeDpsB] = (90f, 11f),
        [PhysRangedDps] = (270f, 19.5f), [CasterDps] = (90f, 19.5f),
    };

    private static readonly Dictionary<PartyRole, Vector2> RavenDiveSpreadFromNael = new()
    {
        [MainTank] = new(-2.75f, -4f), [OffTank] = new(2.75f, -4f),
        [MeleeDpsA] = new(-4.5f, 3f), [MeleeDpsB] = new(4.5f, 3f),
        [RegenHealer] = new(-11.8f, 0.75f), [ShieldHealer] = new(11.8f, 0.75f),
        [PhysRangedDps] = new(-7.3f, 9.5f), [CasterDps] = new(7.3f, 9.5f),
    };

    private const float FirstThunderEarRadius = 5.2f;
    private const float FirstThunderEarDegrees = 118f;

    protected override Vector2 StackForHeavensfall
    {
        get
        {
            var south = Markers.Waymarks.First(w => w.Slot == WaymarkSlot.Three).Offset;
            return new Vector2(south.X, south.Z);
        }
    }

    protected override Vector2 OpenerSpot(PartyRole role, bool secondSet)
    {
        if (secondSet && role == MainTank) return NaelCentredFacingNorth;
        var (degrees, radius) = secondSet ? SecondBurstSpread[role] : FirstBurstSpread[role];
        return Compass(degrees) * radius;
    }

    protected override Vector2 FirstThunderEar(Vector2 naelAt, Vector2 inward, IReadOnlyList<PartyRole> holders, int holder)
    {
        var goesLeft = holders[holder] == holders.Min();
        return naelAt + Rotate(inward, goesLeft ? FirstThunderEarDegrees : -FirstThunderEarDegrees) * FirstThunderEarRadius;
    }

    protected override Vector2 RavenSpreadSpot(PartyRole role, Vector2 naelAt, Vector2 inward) => naelAt + RavenDiveSpreadFromNael[role];
}
