using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P3Apocalypse;

// Dark Water at three durations on two players each (two get none), the Apocalypse lights rolling
// around the ring from a random spoke, Spirit Taker on a random player, and Darkest Dance on the
// farthest.
public sealed class FruP3ApocalypseState
{
    public const float RingRadius = 14f;
    public static float WaterAt(int duration) => duration switch { 1 => 23.1f, 2 => 41.9f, _ => 50.9f };
    public static float WaveAt(int wave) => 33.3f + 2f * wave;
    public const int WaveCount = 6;

    // Eighths of a turn from north where the first lights go up.
    public int Rotation { get; }
    public bool Clockwise { get; }
    // Indexed by PartyRole: 0 = no Dark Water, else its duration slot 1-3.
    public IReadOnlyList<int> WaterDurations { get; }
    public PartyRole SpiritTakerTarget { get; }

    public FruP3ApocalypseState(Rng rng, FruP3ApocalypseStateOverrides overrides)
    {
        Rotation = overrides.Rotation ?? rng.Next(4);
        Clockwise = overrides.Clockwise ?? rng.NextBool();
        WaterDurations = rng.Shuffle(0, 0, 1, 1, 2, 2, 3, 3).ToList();
        SpiritTakerTarget = rng.NextRole();
    }

    public int WaterDuration(PartyRole role) => WaterDurations[(int)role];

    public IEnumerable<PartyRole> WaterHolders(int duration)
        => Enum.GetValues<PartyRole>().Where(role => WaterDuration(role) == duration);

    // The center goes off twice, then each step's pair of opposite spokes, three steps at a time.
    public IEnumerable<Vector3> Explosions(int wave)
    {
        if (wave < 2) yield return Vector3.Zero;
        for (var step = Math.Max(0, wave - 2); step <= wave; step++)
        {
            var angle = (Rotation * 45 + (Clockwise ? 45 : -45) * step) * MathF.PI / 180;
            var position = new Vector3(MathF.Sin(angle), 0, -MathF.Cos(angle)) * RingRadius;
            yield return position;
            yield return -position;
        }
    }
}
