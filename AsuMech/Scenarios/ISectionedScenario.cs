using System.Collections.Generic;

namespace AsuMech.Scenarios;

// A scenario that can also be played one stretch at a time; Sections[0] is the whole scenario.
public interface ISectionedScenario : IScenario
{
    IReadOnlyList<ScenarioSection> Sections { get; }
    int SelectedSection { get; set; }
}

public sealed record ScenarioSection(string Label, string Covers);
