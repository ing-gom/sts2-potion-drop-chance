using System;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2PotionDropChance;

/// <summary>
/// Owns badge UI lifecycle. Each travelable Monster/Elite/Unknown map point gets
/// a VBoxContainer child holding 1–2 colored "row" badges (one per drop hypothesis).
/// Disable via env var <c>STS2_POTION_DROP_CHANCE_DISABLED=1</c>.
/// </summary>
internal static class MapBadgeService
{
    private const string ContainerName = "Sts2PotionDropChance_Badge";
    private const int FontSize = 20;
    private const int IconSize = 28;
    private const int CornerRadius = 6;
    private const int GapToNodePx = 10;

    private static readonly bool _disabled =
        System.Environment.GetEnvironmentVariable("STS2_POTION_DROP_CHANCE_DISABLED") == "1";

    public static void Install(SceneTree _) { /* Harmony-driven; no scene node needed. */ }

    /// <summary>Patches call this on every map-point visual refresh. Idempotent.</summary>
    public static void EnsureBadgeUpdated(NNormalMapPoint nmp)
    {
        if (_disabled) { RemoveContainer(nmp); return; }

        try
        {
            if (nmp.State != MapPointState.Travelable) { HideContainer(nmp); return; }

            var rm = RunManager.Instance;
            var state = rm?.State;
            if (state == null || nmp.Point == null) { HideContainer(nmp); return; }

            var me = LocalContext.GetMe(state.Players);
            if (me == null) { HideContainer(nmp); return; }

            var results = PotionDropCalculator.ComputeAll(me, nmp.Point, state);
            if (results.Count == 0) { HideContainer(nmp); return; }

            var container = GetOrCreateContainer(nmp);
            ClearChildren(container);

            bool showTypeIcon = nmp.Point.PointType == MapPointType.Unknown;
            foreach (var r in results)
                container.AddChild(BuildRow(r, showTypeIcon, nmp.GetTree()));
            container.Visible = true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] badge update failed: {ex.Message}");
        }
    }

    private static VBoxContainer GetOrCreateContainer(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<VBoxContainer>(ContainerName);
        if (existing != null) return existing;

        // Anchor to node's right-center so the badge stays glued to the node regardless of icon size.
        // GrowVertical=Both makes the VBox expand symmetrically around the anchor → vertically centered.
        var vbox = new VBoxContainer
        {
            Name = ContainerName,
            ZIndex = 100,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 1.0f,
            AnchorRight = 1.0f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = GapToNodePx,
            OffsetTop = 0,
            OffsetRight = GapToNodePx,
            OffsetBottom = 0,
            GrowVertical = Control.GrowDirection.Both,
            GrowHorizontal = Control.GrowDirection.End,
        };
        vbox.AddThemeConstantOverride("separation", 4);
        nmp.AddChild(vbox);
        return vbox;
    }

    private static PanelContainer BuildRow(PotionDropCalculator.Result r, bool includeTypeIcon, SceneTree? tree)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var bg = ColorScale.For(r.Probability);
        bg.A = 0.92f;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = CornerRadius,
            CornerRadiusTopRight = CornerRadius,
            CornerRadiusBottomLeft = CornerRadius,
            CornerRadiusBottomRight = CornerRadius,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        });

        var hbox = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        hbox.AddThemeConstantOverride("separation", 5);

        if (includeTypeIcon)
        {
            var typeTex = r.Kind == PotionDropCalculator.NodeKind.Elite
                ? MapIconLoader.Elite()
                : MapIconLoader.Monster();
            if (typeTex != null) hbox.AddChild(MakeIcon(typeTex));
        }

        var potionTex = PotionIconLoader.Get(tree);
        if (potionTex != null) hbox.AddChild(MakeIcon(potionTex));

        var label = new Label
        {
            Text = $"{r.Probability * 100f:0}%",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", FontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.7f));
        label.AddThemeConstantOverride("outline_size", 3);
        hbox.AddChild(label);

        panel.AddChild(hbox);
        return panel;
    }

    private static TextureRect MakeIcon(Texture2D tex) => new()
    {
        Texture = tex,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        CustomMinimumSize = new Vector2(IconSize, IconSize),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren()) child.QueueFree();
    }

    private static void HideContainer(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<VBoxContainer>(ContainerName);
        if (existing != null) existing.Visible = false;
    }

    private static void RemoveContainer(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<VBoxContainer>(ContainerName);
        existing?.QueueFree();
    }
}
