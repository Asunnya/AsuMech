using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace AsuMech.Windows.Asu;

// Ways to support the project. Opened only from its buttons: nothing here ever pops up by itself.
internal sealed class SupportWindow : Window
{
    // The Ko-fi card hides while this is empty.
    public const string KoFi = "https://ko-fi.com/asunnya";
    public const string PixKey = "1286e0e5-ab51-4a73-9b3a-ec20e94b5da9";
    // The key's static BR Code. Its name field reads ASUMECH: the payer's bank shows the name the
    // key is registered to, never this one, so the code itself names nobody.
    public const string PixCode = "00020126580014br.gov.bcb.pix01361286e0e5-ab51-4a73-9b3a-ec20e94b5da95204000053039865802BR5907ASUMECH6009SAO PAULO62070503***6304014C";
    private const string PixQrResource = "AsuMech.Windows.Asu.Images.pix-qr.png";
    private const float QrSizePx = 168f;

    private const float CardWidthPx = 340f;
    private const float CardPaddingPx = 12f;
    private const float CardRoundingPx = 8f;
    private const double CopiedSeconds = 2.5;

    private IDisposable? theme;
    private double copiedAt = double.NegativeInfinity;

    public SupportWindow() : base("Support AsuMech###AsuMechSupport", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
    }

    public override void PreDraw() => theme = AsuTheme.Push();

    public override void PostDraw()
    {
        theme?.Dispose();
        theme = null;
    }

    public override void Draw()
    {
        DrawHeader();
        ImGui.Spacing();
        if (KoFi.Length > 0)
        {
            Card("kofi", DrawKoFi);
            ImGui.Spacing();
        }
        Card("pix", DrawPix);
    }

    private static void DrawHeader()
    {
        using (AsuTheme.PushTextColor(AsuTheme.Accent)) Icon(FontAwesomeIcon.Heart);
        ImGui.SameLine();
        ImGui.TextUnformatted("Support AsuMech");
        using (AsuTheme.PushTextColor(AsuTheme.Muted))
        {
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + CardWidthPx * AsuTheme.Scale);
            ImGui.TextUnformatted("AsuMech is free and always will be. If it helped you clear, you can chip in here.");
            ImGui.PopTextWrapPos();
        }
    }

    private static void DrawKoFi(float width)
    {
        ImGui.TextUnformatted("Ko-fi");
        using (AsuTheme.PushTextColor(AsuTheme.Muted)) ImGui.TextUnformatted("PayPal or card, from anywhere");
        using (AsuTheme.PushAccentButton())
        using (AsuTheme.PushPill())
            if (ImGui.Button("Open Ko-fi  ↗##kofi", new Vector2(width, 0f))) Util.OpenLink(KoFi);
    }

    private void DrawPix(float width)
    {
        ImGui.TextUnformatted("Pix");
        using (AsuTheme.PushTextColor(AsuTheme.Muted)) ImGui.TextUnformatted("Brasil · escaneie ou copie a chave");

        var texture = Plugin.TextureProvider.GetFromManifestResource(typeof(SupportWindow).Assembly, PixQrResource);
        if (texture.TryGetWrap(out var wrap, out _))
        {
            var size = QrSizePx * AsuTheme.Scale;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (width - size) / 2f);
            ImGui.Image(wrap.Handle, new Vector2(size, size));
        }

        var copyLabel = Copied ? "Copiado!" : "Copiar";
        var copyWidth = ImGui.CalcTextSize("Copiado!").X + ImGui.GetStyle().FramePadding.X * 4f;
        var key = PixKey;
        ImGui.SetNextItemWidth(width - copyWidth - ImGui.GetStyle().ItemSpacing.X);
        ImGui.InputText("##pix-key", ref key, PixKey.Length + 1, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
        ImGui.SameLine();
        using (Copied ? AsuTheme.PushTextColor(AsuTheme.Ok) : null)
        using (AsuTheme.PushPill())
            if (ImGui.Button($"{copyLabel}##pix-copy", new Vector2(copyWidth, 0f)))
            {
                ImGui.SetClipboardText(PixKey);
                copiedAt = ImGui.GetTime();
            }
    }

    private bool Copied => ImGui.GetTime() - copiedAt < CopiedSeconds;

    // A rounded panel behind its content, drawn after the content so its height is known.
    private static void Card(string id, Action<float> content)
    {
        var scale = AsuTheme.Scale;
        var padding = CardPaddingPx * scale;
        var width = CardWidthPx * scale;
        var draw = ImGui.GetWindowDrawList();
        draw.ChannelsSplit(2);
        draw.ChannelsSetCurrent(1);
        var start = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(start + new Vector2(padding, padding));
        ImGui.PushID(id);
        ImGui.BeginGroup();
        content(width - padding * 2f);
        ImGui.EndGroup();
        ImGui.PopID();
        var end = new Vector2(start.X + width, ImGui.GetItemRectMax().Y + padding);
        draw.ChannelsSetCurrent(0);
        draw.AddRectFilled(start, end, AsuTheme.U32(AsuTheme.Element), CardRoundingPx * scale);
        draw.ChannelsMerge();
        ImGui.SetCursorScreenPos(new Vector2(start.X, end.Y));
        ImGui.Dummy(new Vector2(width, 0f));
    }

    private static void Icon(FontAwesomeIcon icon)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextUnformatted(icon.ToIconString());
        ImGui.PopFont();
    }
}
