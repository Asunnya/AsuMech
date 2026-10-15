namespace AnoMech.Tests;

// A link's stalls (a lost packet holding back everything behind it), drawn as far ahead as they
// are asked about so a seed always lays them out the same way.
internal sealed class LinkStalls(Random random, double every, double min, double max)
{
    private readonly List<(double Start, double End)> spans = [];
    private double drawnTo;

    // The end of the stall `at` falls in, if any.
    public double? Within(double at)
    {
        while (drawnTo <= at)
        {
            var start = drawnTo + -every * Math.Log(1 - random.NextDouble());
            drawnTo = start + min + random.NextDouble() * (max - min);
            spans.Add((start, drawnTo));
        }
        int low = 0, high = spans.Count - 1;
        while (low <= high)
        {
            var middle = (low + high) / 2;
            var (start, end) = spans[middle];
            if (at < start) high = middle - 1;
            else if (at >= end) low = middle + 1;
            else return end;
        }
        return null;
    }
}
