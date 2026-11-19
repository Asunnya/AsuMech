using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using Scenario = AsuMech.Scenarios.Ucob.P3SeventhUmbralEra.UcobP3SeventhUmbralEraScenario;

namespace AsuMech.Scenarios.Ucob.P3SeventhUmbralEra;

public sealed class UcobP3SeventhUmbralEraAi(bool tuufless = false) : IScenarioAi<UcobMarkers>, IUcobStrat
{
    public string Name => tuufless ? "Tuufless (Elemental)" : "Stack on marker 3 for the knockback";

    public UcobMarkers Markers => tuufless ? UcobMarkers.Elemental : UcobMarkers.Aether;

    private static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();

    private const float StackRingRadius = 0.8f;

    private static readonly Dictionary<PartyRole, Vector2> PickUpSpots = new()
    {
        [MainTank] = new(0f, -3f), [OffTank] = new(0f, 2.5f), [RegenHealer] = new(-1.5f, 4f), [ShieldHealer] = new(1.5f, 4f),
        [MeleeDpsA] = new(-1f, 3f), [MeleeDpsB] = new(1f, 3f), [PhysRangedDps] = new(-1f, 5.5f), [CasterDps] = new(1f, 5.5f),
    };

    private const float StackOnMarkerThree = 0.3f;
    private const float WalkBackAfterTheKnockback = Scenario.SeventhUmbralEra + 0.9f;
    private const float PickUpBahamut = 11f;

    public void Run(UcobMarkers markers, SimWorld world)
    {
        var ai = new AiManager(world);
        var markerThree = MarkerThree(markers);

        ai.Move(StackOnMarkerThree, () => StackOn(markerThree), jitter: 0f, sprint: true);
        ai.Move(WalkBackAfterTheKnockback, () => StackOn(markerThree), jitter: 0f, sprint: true);
        ai.Move(PickUpBahamut, () => Spots(r => PickUpSpots[r]), jitter: 0f, sprint: true);
    }

    private static Vector2 MarkerThree(UcobMarkers markers)
    {
        var offset = markers.Waymarks.First(w => w.Slot == WaymarkSlot.Three).Offset;
        return new Vector2(offset.X, offset.Z);
    }

    private static IAiMove StackOn(Vector2 marker)
        => Spots(r => marker + StackRingRadius * new Vector2(MathF.Sin((int)r * MathF.Tau / Roles.Length), MathF.Cos((int)r * MathF.Tau / Roles.Length)));

    private static IAiMove Spots(Func<PartyRole, Vector2> spot)
    {
        var coords = new Vector2?[Roles.Length];
        foreach (var role in Roles) coords[(int)role] = spot(role);
        return AiMove.Create(coords).NaturalOrder();
    }
}
