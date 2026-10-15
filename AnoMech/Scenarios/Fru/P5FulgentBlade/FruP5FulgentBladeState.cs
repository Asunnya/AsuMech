using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// Three trains of four exalines each, rolling 5y every 2s across the arena from three of the
// pattern's edges. Their layout comes from FRU-Sim's exawave scenes, whose 11.855 units are the
// game's 5y step.
public sealed class FruP5FulgentBladeState
{
    public const int GroupCount = 3;
    public const int WavesPerGroup = 4;
    public const int HitCount = 7;
    public const float Step = 5f;
    public const float ReferenceScale = Step / 11.855f;

    public static float HitAt(int group, int hit) => 20.5f + group * 4f + hit * 2f;

    // Which of the four cardinal pockets the safe path starts in, and how the trains turn around it.
    public int PositionIndex { get; }
    public int RotationIndex { get; }
    public bool EastFirst { get; }

    // One random member of each light party: MT/H1/M1/R1 and OT/H2/M2/R2.
    public PartyRole AkhMornFirstLightParty { get; }
    public PartyRole AkhMornSecondLightParty { get; }

    public FruP5FulgentBladeState(Rng rng, FruP5FulgentBladeStateOverrides overrides)
    {
        PositionIndex = overrides.PositionIndex ?? rng.Next(4);
        RotationIndex = overrides.RotationIndex ?? rng.Next(4);
        EastFirst = overrides.EastFirst ?? rng.NextBool();
        AkhMornFirstLightParty = (PartyRole)(2 * rng.Next(4));
        AkhMornSecondLightParty = (PartyRole)(2 * rng.Next(4) + 1);
    }

    public Vector3 Origin => (PositionIndex switch
    {
        0 => new Vector3(0f, 0f, 17f),
        1 => new Vector3(0f, 0f, -17f),
        2 => new Vector3(17f, 0f, 0f),
        _ => new Vector3(-17f, 0f, 0f),
    }) * ReferenceScale;

    public float Rotation => RotationIndex * MathF.PI / 2f;

    // Odd waves are light, even ones dark; each strip is Step deep and spans the arena.
    public (Placement Placement, bool IsLight) Wave(int group, int wave, int hit = 0)
    {
        var emitter = group == 1 ? 2 : (group == 0) == EastFirst ? 0 : 1;
        var (position, yaw) = emitter switch
        {
            0 => (new Vector3(9.821f, 0f, 23.710f), 0f),
            1 => (new Vector3(-9.821f, 0f, -23.710f), MathF.PI),
            _ => (new Vector3(23.710f, 0f, -9.821f), MathF.PI / 2f),
        };
        var localYaw = wave switch
        {
            0 => 0f,
            1 => MathF.PI,
            2 => 5f * MathF.PI / 4f,
            _ => MathF.PI / 4f,
        };
        var origin = Origin + Vector3.Transform(position * ReferenceScale, Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rotation));
        var facing = Rotation + yaw + localYaw + MathF.PI;
        var forward = new Vector3(MathF.Sin(facing), 0f, MathF.Cos(facing));
        return (new Placement(origin + forward * (hit * Step), facing), wave % 2 == 1);
    }
}
