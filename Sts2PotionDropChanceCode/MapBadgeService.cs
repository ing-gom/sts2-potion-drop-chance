using System;
using System.Reflection;
using Godot;
using HarmonyLib;
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
    // Per-NNormalMapPoint meta key for the *base* _iconContainer.Scale captured at
    // RefreshState time — bypasses the per-frame oscillation _Process applies for
    // travelable nodes, which would otherwise make sibling badges render at different
    // sizes depending on the sin-wave phase at the moment we read.
    private const string IconScaleMetaKey = "Sts2PotionDropChance_BaseIconScale";
    private const int FontSize = 20;
    private const int IconSize = 28;
    private const int CornerRadius = 6;
    private const int GapToNodePx = 10;
    // Above-specific extra gap. Visually the icon's drawn top sits slightly inside the
    // rect's top edge (icon art doesn't fill the full Control), so a uniform 10px gap
    // reads as too tight when the badge is placed above. Side/below placements look fine.
    private const int AboveExtraGapPx = 0;

    private static readonly bool _disabled =
        System.Environment.GetEnvironmentVariable("STS2_POTION_DROP_CHANCE_DISABLED") == "1";

    /// <summary>
    /// When true, suppresses the badge on Unknown (?) map nodes. Toggled at runtime
    /// by <see cref="ModConfigBridge"/> when the ModConfig framework is installed.
    /// </summary>
    internal static volatile bool HideUnknownNodes;

    internal enum BadgePosition { Right, Left, Above, Below }

    /// <summary>
    /// Where the badge appears relative to the map node. Toggled at runtime via ModConfig.
    /// </summary>
    internal static BadgePosition Position = BadgePosition.Right;

    /// <summary>
    /// Set by <see cref="MapBadgeOverlayWatcher"/> while any modal popup
    /// (Pause menu, Settings, confirm dialogs) is open — keeps the badge from
    /// bleeding through UI that lives on a different CanvasLayer.
    /// </summary>
    private static volatile bool _globalSuppress;

    // Lazily resolved — NMapScreen.IsTraveling visibility (public vs internal)
    // isn't known at compile time, so we read it via reflection to stay robust.
    private static MethodInfo? _isTravelingGetter;
    private static bool _isTravelingResolved;

    // SmallerMap (and any sibling map-resize mod) shrinks NNormalMapPoint._iconContainer.Scale
    // without changing the host node's Control rect — so our anchor-positioned badge would
    // remain at original size while the visible icon shrinks. Mirror the icon's scale onto
    // the badge container so it stays visually proportional.
    private static FieldInfo? _iconContainerField;
    private static bool _iconContainerFieldResolved;

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

            if (HideUnknownNodes && nmp.Point.PointType == MapPointType.Unknown)
            { HideContainer(nmp); return; }

            var results = PotionDropCalculator.ComputeAll(me, nmp.Point, state);
            if (results.Count == 0) { HideContainer(nmp); return; }

            var container = GetOrCreateContainer(nmp);
            ClearChildren(container);

            bool showTypeIcon = nmp.Point.PointType == MapPointType.Unknown;
            foreach (var r in results)
                container.AddChild(BuildRow(r, showTypeIcon, nmp.GetTree()));

            // iconScale governs both (a) where to anchor (visible icon edge, not rect edge)
            // and (b) badge content scale. Computed per-call so runtime ModConfig changes propagate.
            var iconScale = TryGetIconScale(nmp);
            ApplyPosition(container, nmp, iconScale);
            container.Scale = new Vector2(iconScale, iconScale);
            container.PivotOffset = ComputePivotForBadge(container.GetCombinedMinimumSize());
            container.Visible = !_globalSuppress;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] badge update failed: {ex.Message}");
        }
    }

    private static VBoxContainer GetOrCreateContainer(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<VBoxContainer>(ContainerName);
        if (existing != null) return existing; // EnsureBadgeUpdated reapplies position+scale every call.

        // ZIndex stays low (1) so any modal/popup in the same CanvasLayer paints over us;
        // without this, popups like the in-game options panel would render below the badge.
        var vbox = new VBoxContainer
        {
            Name = ContainerName,
            ZIndex = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        vbox.AddThemeConstantOverride("separation", 4);
        nmp.AddChild(vbox);
        return vbox;
    }

    /// <summary>
    /// Anchors the badge from the parent NMapPoint's center, offset by the visible icon's
    /// half-extent (parent-rect-half × iconScale) plus a fixed gap. Anchoring from the
    /// rect edge would leave a growing gap when sibling mods (SmallerMap etc.) shrink
    /// _iconContainer.Scale without changing the host Control's Size — the rect edge stays
    /// at the un-scaled position while the visible icon collapses inward.
    /// </summary>
    private static void ApplyPosition(VBoxContainer vbox, NNormalMapPoint nmp, float iconScale)
    {
        var halfX = nmp.Size.X * 0.5f * iconScale;
        var halfY = nmp.Size.Y * 0.5f * iconScale;

        // Anchor at parent center for every position; offset carries direction + magnitude.
        vbox.AnchorLeft = vbox.AnchorRight = 0.5f;
        vbox.AnchorTop = vbox.AnchorBottom = 0.5f;

        switch (Position)
        {
            case BadgePosition.Left:
                vbox.OffsetLeft = vbox.OffsetRight = -(halfX + GapToNodePx);
                vbox.OffsetTop = vbox.OffsetBottom = 0;
                vbox.GrowHorizontal = Control.GrowDirection.Begin;
                vbox.GrowVertical = Control.GrowDirection.Both;
                break;
            case BadgePosition.Above:
                vbox.OffsetLeft = vbox.OffsetRight = 0;
                vbox.OffsetTop = vbox.OffsetBottom = -(halfY + GapToNodePx + AboveExtraGapPx);
                vbox.GrowHorizontal = Control.GrowDirection.Both;
                vbox.GrowVertical = Control.GrowDirection.Begin;
                break;
            case BadgePosition.Below:
                vbox.OffsetLeft = vbox.OffsetRight = 0;
                vbox.OffsetTop = vbox.OffsetBottom = halfY + GapToNodePx;
                vbox.GrowHorizontal = Control.GrowDirection.Both;
                vbox.GrowVertical = Control.GrowDirection.End;
                break;
            case BadgePosition.Right:
            default:
                vbox.OffsetLeft = vbox.OffsetRight = halfX + GapToNodePx;
                vbox.OffsetTop = vbox.OffsetBottom = 0;
                vbox.GrowHorizontal = Control.GrowDirection.End;
                vbox.GrowVertical = Control.GrowDirection.Both;
                break;
        }
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

    /// <summary>Called from <c>NNormalMapPoint.RefreshState</c> postfix.</summary>
    public static void CaptureBaseIconScale(NNormalMapPoint nmp)
    {
        nmp.SetMeta(IconScaleMetaKey, ReadLiveIconScale(nmp));
    }

    private static float TryGetIconScale(NNormalMapPoint nmp)
    {
        if (nmp.HasMeta(IconScaleMetaKey))
            return (float)nmp.GetMeta(IconScaleMetaKey).AsSingle();
        return ReadLiveIconScale(nmp);
    }

    private static float ReadLiveIconScale(NNormalMapPoint nmp)
    {
        if (!_iconContainerFieldResolved)
        {
            _iconContainerField = AccessTools.Field(typeof(NNormalMapPoint), "_iconContainer");
            _iconContainerFieldResolved = true;
        }
        if (_iconContainerField == null) return 1f;
        return _iconContainerField.GetValue(nmp) switch
        {
            Node2D n2d => n2d.Scale.X,
            Control ctrl => ctrl.Scale.X,
            _ => 1f,
        };
    }

    private static Vector2 ComputePivotForBadge(Vector2 size) => Position switch
    {
        BadgePosition.Right => new Vector2(0, size.Y * 0.5f),
        BadgePosition.Left => new Vector2(size.X, size.Y * 0.5f),
        BadgePosition.Above => new Vector2(size.X * 0.5f, size.Y),
        BadgePosition.Below => new Vector2(size.X * 0.5f, 0),
        _ => Vector2.Zero,
    };

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

    public static void HideAllBadgesIfTraveling(NMapPoint clicked)
    {
        if (_disabled) return;
        try
        {
            Node? n = clicked;
            while (n != null && n is not NMapScreen) n = n.GetParent();
            if (n is not NMapScreen screen) return;
            if (!IsTraveling(screen)) return;

            HideAllBadgesUnder(screen);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] hide-on-select failed: {ex.Message}");
        }
    }

    private static bool IsTraveling(NMapScreen screen)
    {
        if (!_isTravelingResolved)
        {
            _isTravelingGetter = AccessTools.PropertyGetter(typeof(NMapScreen), "IsTraveling");
            _isTravelingResolved = true;
        }
        if (_isTravelingGetter == null) return false;
        return _isTravelingGetter.Invoke(screen, null) is true;
    }

    private static void HideAllBadgesUnder(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child.Name == ContainerName && child is VBoxContainer vbox)
                vbox.Visible = false;
            else if (child.GetChildCount() > 0)
                HideAllBadgesUnder(child);
        }
    }

    /// <summary>
    /// Called by <see cref="MapBadgeOverlayWatcher"/> on every modal-state edge.
    /// On suppress→true, hide every badge container in one pass. On suppress→false,
    /// re-run <see cref="EnsureBadgeUpdated"/> on each NMapPoint so per-node decisions
    /// (e.g., <see cref="HideUnknownNodes"/>) are honored — a blanket Visible=true
    /// would resurrect intentionally hidden containers.
    /// </summary>
    public static void SetGlobalSuppress(bool suppress, SceneTree? tree)
    {
        _globalSuppress = suppress;
        if (tree?.Root == null) return;
        if (suppress)
            SetVisibilityUnder(tree.Root, false);
        else
            RefreshAllUnder(tree.Root);
    }

    private static void SetVisibilityUnder(Node root, bool visible)
    {
        foreach (var child in root.GetChildren())
        {
            if (child.Name == ContainerName && child is VBoxContainer vbox)
                vbox.Visible = visible;
            else if (child.GetChildCount() > 0)
                SetVisibilityUnder(child, visible);
        }
    }

    /// <summary>
    /// Forces every badge in the SceneTree to re-run <see cref="EnsureBadgeUpdated"/>.
    /// Use when a setting (e.g., <see cref="HideUnknownNodes"/>) changes and we need
    /// the result reflected without waiting for the next natural visual refresh.
    /// </summary>
    public static void RefreshAllBadges(SceneTree? tree)
    {
        if (tree?.Root == null) return;
        RefreshAllUnder(tree.Root);
    }

    private static void RefreshAllUnder(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is NNormalMapPoint nmp) EnsureBadgeUpdated(nmp);
            if (child.GetChildCount() > 0) RefreshAllUnder(child);
        }
    }

    private static void RemoveContainer(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<VBoxContainer>(ContainerName);
        existing?.QueueFree();
    }
}
