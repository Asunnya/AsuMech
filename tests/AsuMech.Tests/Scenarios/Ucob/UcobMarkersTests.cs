using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Scenarios.Ucob;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Tests;

public class UcobMarkersTests
{
    private static Vector2 Marker(System.Collections.Generic.IReadOnlyList<Waymark> layout, WaymarkSlot slot)
    {
        var at = layout.Single(w => w.Slot == slot).Offset;
        return new Vector2(at.X, at.Z);
    }

    [TestCase(WaymarkSlot.One)]
    [TestCase(WaymarkSlot.Two)]
    [TestCase(WaymarkSlot.Three)]
    public void ElementalMirrorsEachAetherNeurolinkMarkerOntoItsOwn(WaymarkSlot slot)
    {
        var placed = UcobMarkers.Elemental.Place(Marker(AetherWaymarks, slot));
        Assert.That(Vector2.Distance(placed, Marker(ElementalWaymarks, slot)), Is.LessThan(1.2f));
    }

    [Test]
    public void AetherLeavesSpotsAndFacingAlone()
    {
        Assert.That(UcobMarkers.Aether.Place(new Vector2(3f, -4f)), Is.EqualTo(new Vector2(3f, -4f)));
        Assert.That(UcobMarkers.Aether.Face(1.2f), Is.EqualTo(1.2f));
    }

    [Test]
    public void ElementalTurnsANorthFacingTowardMarkerOne()
    {
        var face = UcobMarkers.Elemental.Face(System.MathF.PI);
        var one = Vector2.Normalize(Marker(ElementalWaymarks, WaymarkSlot.One));
        Assert.That(Vector2.Dot(new Vector2(System.MathF.Sin(face), System.MathF.Cos(face)), one), Is.GreaterThan(0.99f));
    }
}
