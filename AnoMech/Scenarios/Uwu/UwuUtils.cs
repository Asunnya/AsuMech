using AnoMech.Core.Native.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using static AnoMech.Scenarios.Uwu.UwuConstants;

namespace AnoMech.Scenarios.Uwu;

public enum LandslideType
{
    Normal,
    Awaken,
    Ultima
}

public class UwuUtils(SimWorld world)
{
    private readonly SimWorld world = world;

    public void UpdateArena(byte value)
    {
        byte[] unionData = [value];
        Natives.Director.SetDirectorData(1, 0, unionData, true);
    }

    public void Awaken(SimEnemy? enemy, bool isUltima)
    {
        enemy?.AddStatusParam(StatusId.Woken, isUltima ? 97 : 0);
        enemy?.SetAnimationState(0, 1);
    }

    public void ResolveSnapshot(IReadOnlyList<SimCharacter> snapshot, uint actionId, string? explanation = null)
    {
        foreach (var character in snapshot)
        {
            if (character.HasStatus(StatusId.Fetters))
            {
                continue;
            }

            character.Die(actionId, explanation);
        }
    }

    public void Cast(Func<SimEnemy?> getEnemy,
        float castOffset, UwuUtilsRecords castInfo, float effectOffset, ActionEffectInfo actionEffectInfo,
        DynamicInfo dynamicInfo,
        float resolveDelay = 0, Action<IReadOnlyList<SimCharacter>>? resolveAction = null)
    {
        world.Events.Add(castOffset, () =>
        {
            var enemy = getEnemy();

            enemy?.NativeCast(
                castInfo.ActionId,
                castInfo.ActionType,
                castInfo.OmenDelay,
                castInfo.CastTime,
                castInfo.Interruptible,
                dynamicInfo.GetValue(dynamicInfo.CastRotation),
                dynamicInfo.GetValue(dynamicInfo.CastPosition),
                dynamicInfo.GetValue(dynamicInfo.CastTarget),
                dynamicInfo.GetValue(dynamicInfo.CastBallistaTarget)
                );
        });

        IReadOnlyList<SimCharacter> snapshot = null!;

        if (resolveAction != null)
        {
            world.Events.Add(castOffset + castInfo.CastTime, () =>
            {
                var enemy = getEnemy();
                Placement placement;

                if (dynamicInfo.CastRotation != null && dynamicInfo.CastPosition != null)
                {
                    placement = new(dynamicInfo.CastPosition(), dynamicInfo.CastRotation());
                }
                else if (dynamicInfo.CastRotation != null)
                {
                    placement = new(enemy!.Position, dynamicInfo.CastRotation());
                }
                else if (dynamicInfo.CastPosition != null)
                {
                    placement = new(dynamicInfo.CastPosition(), enemy!.Rotation);
                }
                else
                {
                    placement = enemy!.Placement();
                }

                snapshot = world.Party.Find.InsideActionAoe(castInfo.ActionId, placement);
            });
        }

        world.Events.Add(effectOffset, () =>
        {
            var enemy = getEnemy();

            enemy?.NativeActionEffect(
                actionEffectInfo.ActionId,
                actionEffectInfo.AnimationLock,
                actionEffectInfo.SpellId,
                actionEffectInfo.AnimationVariaton,
                actionEffectInfo.ActionType,
                actionEffectInfo.Flags,
                dynamicInfo.GetValue(dynamicInfo.ActionEffectRotation),
                dynamicInfo.GetValue(dynamicInfo.ActionEffectPosition),
                dynamicInfo.GetValue(dynamicInfo.ActionEffectAnimationTarget),
                dynamicInfo.GetValue(dynamicInfo.ActionEffectActionTarget),
                dynamicInfo.GetValue(dynamicInfo.ActionEffectBallistaTarget)
                );
        });

        if (resolveAction != null)
        {
            world.Events.Add(effectOffset + resolveDelay, () => resolveAction(snapshot));
        }
    }

    // Each dummy drops its rain where its target stood at `targetOffset`.
    public void FeatherRain(Func<SimEnemy?>[] getDummies, float targetOffset, float castOffset,
        IReadOnlyList<PartyRole>? targets = null)
    {
        var positions = new List<Vector3>();

        world.Events.Add(targetOffset, () => positions.AddRange(
            (targets ?? RoleList.Random(world.Rng, world.Party, getDummies.Length).List)
            .Select(x => world.Party.Get(x)!.Position)));

        world.Events.Add(castOffset, () =>
        {
            for (var i = 0; i < getDummies.Length; i++)
            {
                var dummy = getDummies[i]();
                dummy?.SetPosition(new Placement(positions[i], 0));
                dummy?.Cast(UwuActions.FeatherRain, positions[i]);
            }
        });
    }

    public void EruptionPuddle(Func<SimEnemy?> getDummy, Func<SimCharacter?> getBait, float castOffset)
        => world.Events.Add(castOffset, () => getDummy()?.Cast(UwuActions.EruptionPuddle, getBait()!.Position));

    public void LandslideLines(Func<SimEnemy?> getEnemy, Func<SimEnemy?>[] getDummies, float castOffset, LandslideType type)
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
            for (var i = 0; i < getDummies.Length; i++)
            {
                var dummy = getDummies[i]();
                dummy?.SetPosition(new Placement(enemy.Position, enemy.Rotation + rotationOffsets[i]));
                dummy?.Cast(action);
            }
        });
    }
}
