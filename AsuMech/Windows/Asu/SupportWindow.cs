using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace AsuMech.Windows.Asu;

// Ways to support the project. Opened only from its buttons: nothing here ever pops up by itself.
internal sealed class SupportWindow : Window
{
    // The button hides while this is empty.
    public const string KoFi = "https://ko-fi.com/asunnya";
    public const string PixKey = "1286e0e5-ab51-4a73-9b3a-ec20e94b5da9";

    private bool copied;

    public SupportWindow() : base("Support AsuMech###AsuMechSupport", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("AsuMech is free. If it helps you clear, you can chip in here.");
        ImGui.Spacing();

        if (KoFi.Length > 0)
        {
            if (ImGui.Button("Ko-fi ↗")) Util.OpenLink(KoFi);
            ImGui.SameLine();
            using (AsuTheme.PushTextColor(AsuTheme.Muted)) ImGui.TextUnformatted("PayPal or card, from anywhere");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        ImGui.TextUnformatted("Pix (Brasil), chave aleatória:");
        ImGui.TextUnformatted(PixKey);
        if (ImGui.Button("Copy Pix key"))
        {
            ImGui.SetClipboardText(PixKey);
            copied = true;
        }
        if (copied)
        {
            ImGui.SameLine();
            using (AsuTheme.PushTextColor(AsuTheme.Muted)) ImGui.TextUnformatted("Copied.");
        }
    }

    public override void OnClose() => copied = false;
}
