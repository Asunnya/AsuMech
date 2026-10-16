using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeHaterList(SimTimeline timeline) : IHaterList
{
    private List<uint> listed = [];

    // Rows only for enemies entering or leaving the list, like the replay's `enemy_list`.
    public void Receive(IReadOnlyList<Hater> haters)
    {
        var now = haters.Select(h => h.EntityId).ToList();
        foreach (var id in now.Where(id => !listed.Contains(id)))
            timeline.Add("enemy_list", id, id: "1", name: "added", detail: $"position={now.IndexOf(id)}");
        foreach (var id in listed.Where(id => !now.Contains(id)))
            timeline.Add("enemy_list", id, id: "0", name: "removed");
        listed = now;
    }
}
