using System;

namespace AnoMech.Core.Native.Interfaces;

// ActorMove's u8 @2 (the low byte) and the state byte after it (the high byte), as other players'
// moves carry them. Retail marks a jump InAir | JumpStart, InAir while rising, InAir | Falling |
// Apex at the top, InAir | Falling, then Landed. UNVERIFIED in game: the jump values are read off
// retail's moves.
[Flags]
public enum ActorMoveFlags : ushort
{
    None = 0,
    Walk = 0x02,
    Strafe = 0x04,
    Knockback = 0x08,
    Falling = 0x10,
    InAir = 0x20,
    JumpStart = 0x0100,
    Landed = 0x0200,
    Apex = 0x0400,
}
