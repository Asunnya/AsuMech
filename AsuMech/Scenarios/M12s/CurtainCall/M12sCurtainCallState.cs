using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.CurtainCall;

public sealed class M12sCurtainCallState
{
    public static readonly PartyRole[] Supports = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];
    public static readonly PartyRole[] Dps = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    // One role gets Rotting Flesh, the other Bursting Grotesquerie.
    public bool RottingOnSupports { get; }
    // The dragon head rises on the east (cleaving west) or the west (cleaving east).
    public bool DragonEast { get; }
    public bool NorthWestSafe { get; }
    public IReadOnlyDictionary<PartyRole, PartyRole> ChainPartner { get; }

    public M12sCurtainCallState(Rng rng, M12sCurtainCallStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sCurtainCallStateOverrides();
        var rottingSupports = rng.NextBool();
        var dragonEast = rng.NextBool();
        var northWest = rng.NextBool();
        var partners = rng.Shuffle(Dps).ToList();
        if (player is { } me && overrides.PlayerRotting is { } rotting)
            rottingSupports = rotting == !me.IsDps();
        RottingOnSupports = rottingSupports;
        DragonEast = overrides.DragonEast ?? dragonEast;
        NorthWestSafe = overrides.NorthWestSafe ?? northWest;
        var pairs = new Dictionary<PartyRole, PartyRole>();
        for (var i = 0; i < Supports.Length; i++)
        {
            pairs[Supports[i]] = partners[i];
            pairs[partners[i]] = Supports[i];
        }
        ChainPartner = pairs;
    }

    public IReadOnlyList<PartyRole> Rotting => RottingOnSupports ? Supports : Dps;
    public IReadOnlyList<PartyRole> Bursting => RottingOnSupports ? Dps : Supports;
    public bool HasRotting(PartyRole role) => Rotting.Contains(role);

    // East: +1, west: -1.
    public int DragonSide => DragonEast ? 1 : -1;
    public Vector3 ConeOrigin => new(DragonSide * 10f, 0f, 0f);
    public Vector3 HeadSpot => new(DragonSide * 10f, 0f, -15f);

    // Their mirror in x spares the other two corners.
    private static readonly Vector3[] NorthWestSafeBlobs = [new(-9f, 0f, 13f), new(15f, 0f, 0f), new(0f, 0f, 0f), new(-15f, 0f, 0f), new(10f, 0f, -13f)];
    private static readonly Vector3[] NorthEastSafeBlobs = [new(9f, 0f, 13f), new(15f, 0f, 0f), new(0f, 0f, 0f), new(-15f, 0f, 0f), new(-9f, 0f, -13f)];

    public IReadOnlyList<Vector3> Blobs => NorthWestSafe ? NorthWestSafeBlobs : NorthEastSafeBlobs;

    // The safe corner on a side (-1 west, 1 east), as -1 north / 1 south.
    public int SafeCornerZ(int side) => (side < 0) == NorthWestSafe ? -1 : 1;
}
