using System;

namespace AnoMech.Core.Native.Interfaces;

// ActorMove u8 @2, as other players' moves carry it.
[Flags]
public enum ActorMoveFlags : byte
{
    None = 0,
    Walk = 0x02,
    Strafe = 0x04,
    Knockback = 0x08,
}
