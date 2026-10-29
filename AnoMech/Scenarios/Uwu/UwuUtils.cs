using AnoMech.Core.Native.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Uwu.UwuConstants;

namespace AnoMech.Scenarios.Uwu;

public enum LandslideType
{
    Normal,
    Awaken,
    Ultima
}

public partial class UwuUtils(SimWorld world)
{
    private readonly SimWorld world = world;

    public void UpdateArena(byte value)
    {
        byte[] unionData = [value];
        Natives.Director.SetDirectorData(1, 0, unionData);
    }

    public void Awaken(SimEnemy? enemy, bool isUltima)
    {
        enemy?.AddStatusParam(StatusId.Woken, isUltima ? 97 : 0);
        enemy?.SetAnimationState(0, 1);
    }

    // Five rains, each dropped where its target stood at `targetOffset`.
    public void FeatherRain(Func<SimEnemy?> getCaster, float targetOffset, float castOffset,
        IReadOnlyList<PartyRole>? targets = null, Func<SimCharacter?>? getExcluded = null)
    {
        const int rains = 5;
        var positions = new List<Vector3>();

        world.Events.Add(targetOffset, () =>
        {
            PartyRole[] excluded = getExcluded?.Invoke() is ISimPartyMember { Role: var role } ? [role] : [];
            positions.AddRange(
                (targets ?? RoleList.AllExcept(world.Rng, world.Party, excluded).List.Take(rains))
                .Select(x => world.Party.Get(x)!.Position));
        });

        world.Events.Add(castOffset, () =>
        {
            foreach (var position in positions.Take(rains))
                getCaster()?.CastFromHelper(UwuActions.FeatherRain, position, at: OnLocation(position));
        });
    }

    public void EruptionPuddle(Func<SimEnemy?> getCaster, Func<SimCharacter?> getBait, float castOffset)
        => world.Events.Add(castOffset, () =>
        {
            var location = getBait()!.Position;
            getCaster()?.CastFromHelper(UwuActions.EruptionPuddle, location, at: OnLocation(location));
        });

    // Retail stands a location-targeted helper on its location, turned north.
    public static Placement OnLocation(Vector3 location) => new(location, MathF.PI);

    // One line per offset, each from the enemy's spot turned by its offset.
    public void LandslideLines(Func<SimEnemy?> getEnemy, float castOffset, LandslideType type)
    {
        var (rotationOffsets, action) = type switch
        {
            LandslideType.Normal => (Geometry.TitanLandslideOffsets.ToArray(), UwuActions.LandslideLine),
            LandslideType.Awaken => (Geometry.TitanLandslideAwakenOffsets.ToArray(), UwuActions.LandslideAwaken),
            LandslideType.Ultima => (Geometry.UltimaLandslideOffsets.ToArray(), UwuActions.LandslideLineUltima),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        world.Events.Add(castOffset, () =>
        {
            var enemy = getEnemy()!;
            foreach (var offset in rotationOffsets)
                enemy.CastFromHelper(action, at: new Placement(enemy.Position, enemy.Rotation + offset));
        });
    }
}
