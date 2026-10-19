using System.Collections.Generic;
using System.Numerics;

namespace AnoMech.Scenarios.M10s.InsaneAir;

public enum SurfMove { Snap, Reentry, Vertical }

// One Insane Air as the duty plays it: where each surfboard stops, what it does there, and the
// map effects that draw the boards and the arrows that tell the moves apart.
public sealed record InsaneAirVariant(
    string Name,
    IReadOnlyList<SurfMove> RedHotMoves,
    IReadOnlyList<SurfMove> DeepBlueMoves,
    bool RedHotEndsInTheMiddle,
    IReadOnlyList<(float At, byte Slot, uint State)> MapEffects)
{
    public static readonly Vector2[] RedHotPath = [new(0f, 13f), new(-13f, 13f), new(-13f, 0f)];
    public static readonly Vector2[] DeepBluePath = [new(0f, -13f), new(13f, -13f), new(13f, 0f)];
    public static readonly Vector2 RedHotStart = new(13f, 13f);
    public static readonly Vector2 DeepBlueStart = new(-13f, -13f);

    public Vector2 RedHotStop(int round) => round < 3 ? RedHotPath[round] : RedHotEndsInTheMiddle ? Vector2.Zero : new Vector2(-13f, -13f);
    public Vector2 DeepBlueStop(int round) => round < 3 ? DeepBluePath[round] : RedHotEndsInTheMiddle ? new Vector2(13f, 13f) : Vector2.Zero;

    public static readonly InsaneAirVariant[] All =
    [
        new("A", [SurfMove.Snap, SurfMove.Vertical, SurfMove.Snap, SurfMove.Reentry], [SurfMove.Reentry, SurfMove.Snap, SurfMove.Vertical, SurfMove.Reentry], false,
        [
            (-0.09f, 0x0A, 0x00020001), (-0.09f, 0x0B, 0x00020001), (-0.09f, 0x0F, 0x00200010), (-0.09f, 0x15, 0x02000100),
            (7.66f, 0x0A, 0x00200010), (7.66f, 0x0B, 0x00200010), (7.66f, 0x0F, 0x00080004), (7.66f, 0x15, 0x00080004), (7.66f, 0x10, 0x00020001), (7.66f, 0x14, 0x20001000),
            (15.01f, 0x0A, 0x00800040), (15.01f, 0x0B, 0x00800040), (15.01f, 0x10, 0x00080004), (15.01f, 0x14, 0x00080004), (15.01f, 0x13, 0x00800040), (15.01f, 0x11, 0x02000100),
            (22.35f, 0x0A, 0x02000100), (22.35f, 0x0B, 0x02000100), (22.35f, 0x11, 0x00080004), (22.35f, 0x13, 0x00080004), (22.35f, 0x0E, 0x08000400), (22.35f, 0x12, 0x00200010),
            (29.70f, 0x0A, 0x04000004), (29.70f, 0x0B, 0x04000004), (29.70f, 0x12, 0x00080004), (29.70f, 0x0E, 0x00080004),
        ]),
        new("A, Red Hot ends in the middle", [SurfMove.Snap, SurfMove.Vertical, SurfMove.Snap, SurfMove.Reentry], [SurfMove.Reentry, SurfMove.Snap, SurfMove.Vertical, SurfMove.Reentry], true,
        [
            (-0.09f, 0x0C, 0x00020001), (-0.09f, 0x0D, 0x00020001), (-0.09f, 0x0F, 0x00200010), (-0.09f, 0x15, 0x02000100),
            (7.66f, 0x0C, 0x00200010), (7.66f, 0x0D, 0x00200010), (7.66f, 0x0F, 0x00080004), (7.66f, 0x15, 0x00080004), (7.66f, 0x10, 0x00020001), (7.66f, 0x14, 0x20001000),
            (15.00f, 0x0C, 0x00800040), (15.00f, 0x0D, 0x00800040), (15.00f, 0x10, 0x00080004), (15.00f, 0x14, 0x00080004), (15.00f, 0x13, 0x00800040), (15.00f, 0x11, 0x02000100),
            (22.36f, 0x0C, 0x02000100), (22.36f, 0x0D, 0x02000100), (22.36f, 0x13, 0x00080004), (22.36f, 0x11, 0x00080004), (22.36f, 0x12, 0x08000400), (22.36f, 0x16, 0x00200010),
            (29.70f, 0x0C, 0x04000004), (29.70f, 0x0D, 0x04000004), (29.70f, 0x16, 0x00080004), (29.70f, 0x12, 0x00080004),
        ]),
        new("B", [SurfMove.Vertical, SurfMove.Snap, SurfMove.Reentry, SurfMove.Snap], [SurfMove.Snap, SurfMove.Reentry, SurfMove.Snap, SurfMove.Vertical], false,
        [
            (-0.09f, 0x0A, 0x00020001), (-0.09f, 0x0B, 0x00020001), (-0.09f, 0x0F, 0x00020001), (-0.09f, 0x15, 0x20001000),
            (7.67f, 0x0A, 0x00200010), (7.67f, 0x0B, 0x00200010), (7.67f, 0x0F, 0x00080004), (7.67f, 0x15, 0x00080004), (7.67f, 0x14, 0x02000100), (7.67f, 0x10, 0x00200010),
            (15.02f, 0x0A, 0x00800040), (15.02f, 0x0B, 0x00800040), (15.02f, 0x14, 0x00080004), (15.02f, 0x10, 0x00080004), (15.02f, 0x11, 0x08000400), (15.02f, 0x13, 0x00020001),
            (22.37f, 0x0A, 0x02000100), (22.37f, 0x0B, 0x02000100), (22.37f, 0x13, 0x00080004), (22.37f, 0x11, 0x00080004), (22.37f, 0x12, 0x00800040), (22.37f, 0x0E, 0x02000100),
            (29.73f, 0x0A, 0x04000004), (29.73f, 0x0B, 0x04000004), (29.73f, 0x0E, 0x00080004), (29.73f, 0x12, 0x00080004),
        ]),
        new("C", [SurfMove.Snap, SurfMove.Reentry, SurfMove.Snap, SurfMove.Vertical], [SurfMove.Snap, SurfMove.Vertical, SurfMove.Snap, SurfMove.Reentry], false,
        [
            (-0.09f, 0x08, 0x00020001), (-0.09f, 0x09, 0x00020001), (-0.09f, 0x0F, 0x00020001), (-0.09f, 0x15, 0x02000100),
            (7.65f, 0x08, 0x00200010), (7.65f, 0x09, 0x00200010), (7.65f, 0x0F, 0x00080004), (7.65f, 0x15, 0x00080004), (7.65f, 0x10, 0x00800040), (7.65f, 0x14, 0x08000400),
            (14.98f, 0x08, 0x00800040), (14.98f, 0x09, 0x00800040), (14.98f, 0x10, 0x00080004), (14.98f, 0x14, 0x00080004), (14.98f, 0x13, 0x00020001), (14.98f, 0x11, 0x02000100),
            (22.33f, 0x08, 0x02000100), (22.33f, 0x09, 0x02000100), (22.33f, 0x13, 0x00080004), (22.33f, 0x11, 0x00080004), (22.33f, 0x12, 0x00200010), (22.33f, 0x0E, 0x20001000),
            (29.67f, 0x08, 0x04000004), (29.67f, 0x09, 0x04000004), (29.67f, 0x12, 0x00080004), (29.67f, 0x0E, 0x00080004),
        ]),
        new("C, Red Hot ends in the middle", [SurfMove.Snap, SurfMove.Reentry, SurfMove.Snap, SurfMove.Vertical], [SurfMove.Snap, SurfMove.Vertical, SurfMove.Snap, SurfMove.Reentry], true,
        [
            (-0.09f, 0x06, 0x00020001), (-0.09f, 0x07, 0x00020001), (-0.09f, 0x0F, 0x00020001), (-0.09f, 0x15, 0x02000100),
            (7.67f, 0x06, 0x00200010), (7.67f, 0x07, 0x00200010), (7.67f, 0x0F, 0x00080004), (7.67f, 0x15, 0x00080004), (7.67f, 0x10, 0x00800040), (7.67f, 0x14, 0x08000400),
            (15.02f, 0x06, 0x00800040), (15.02f, 0x07, 0x00800040), (15.02f, 0x10, 0x00080004), (15.02f, 0x14, 0x00080004), (15.02f, 0x13, 0x00020001), (15.02f, 0x11, 0x02000100),
            (22.37f, 0x06, 0x02000100), (22.37f, 0x07, 0x02000100), (22.37f, 0x13, 0x00080004), (22.37f, 0x11, 0x00080004), (22.37f, 0x16, 0x00200010), (22.37f, 0x12, 0x20001000),
            (29.73f, 0x06, 0x04000004), (29.73f, 0x07, 0x04000004), (29.73f, 0x16, 0x00080004), (29.73f, 0x12, 0x00080004),
        ]),
    ];
}
