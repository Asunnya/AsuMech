using AnoMech.Core.Game;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows.Asu;

// Filled in by later redesign steps.
public sealed partial class AsuMainWindow
{
    private enum FeedbackKind { Scenario, Strat, Bug }

    private void DrawFeedbackButton() { }
    private void DrawRequestLink(FeedbackKind kind) { }
    private void DrawLearn(Game game, IScenario scenario, bool inSession) => Muted("Learn mode is not built yet.");
}
