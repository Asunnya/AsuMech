using System.Numerics;

namespace AsuMech.Windows;

// The main window a side panel docks to; whichever layout is active.
internal interface IPanelHost
{
    bool IsOpen { get; }
    bool IsActuallyCollapsed { get; }
    bool ShowingMultiplayerSetup { get; }
    Vector2 RightPanelAnchor { get; }
    float ScenarioPanelHeight { get; }
}
