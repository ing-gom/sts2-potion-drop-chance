using System;
using Godot;
using MkBridge = Sts2.ModKit.Config.ModConfigBridge;

namespace Sts2PotionDropChance;

/// <summary>
/// In-game settings registration, delegated to the shared <see cref="Sts2.ModKit.Config.ModConfigBridge"/>.
/// That bridge integrates with EITHER the RitsuLib or the ModConfig framework (RitsuLib preferred),
/// and no-ops with defaults when neither is installed — so this mod has zero hard dependency on
/// either. The local class name is kept so <c>MainFile</c>'s call site is unchanged.
/// </summary>
internal static class ModConfigBridge
{
    private const string EntryKeyHideUnknown = "hideUnknown";
    private const string EntryKeyPosition = "badgePosition";

    private static readonly string[] PositionOptions = { "Right", "Left", "Above", "Below" };

    private static bool _attempted;

    public static void TryRegister()
    {
        if (_attempted) return;
        _attempted = true;

        MkBridge.For(MainFile.ModId, "Potion Drop Chance", MainFile.Logger)
            .Toggle(EntryKeyHideUnknown, "Hide on ? (Unknown) nodes", defaultValue: false,
                onChanged: v => { MapBadgeService.HideUnknownNodes = v; TryRefresh(); })
                .Description("Suppresses the potion drop chance badge on Unknown (?) map nodes. Monster and Elite nodes are unaffected.")
            .Dropdown(EntryKeyPosition, "Badge position", defaultValue: PositionOptions[0], options: PositionOptions,
                onChanged: v => { if (TryParsePosition(v, out var p)) { MapBadgeService.Position = p; TryRefresh(); } })
                .Description("Where the potion drop chance badge appears relative to the map node.")
            .Register();

        // Apply persisted values now so the very first map screen honors saved settings.
        MapBadgeService.HideUnknownNodes = MkBridge.GetValue(MainFile.ModId, EntryKeyHideUnknown, false);
        if (TryParsePosition(MkBridge.GetValue<string>(MainFile.ModId, EntryKeyPosition, PositionOptions[0]), out var pos))
            MapBadgeService.Position = pos;
    }

    private static bool TryParsePosition(object? raw, out MapBadgeService.BadgePosition pos)
    {
        if (raw is string s && Enum.TryParse(s, ignoreCase: true, out MapBadgeService.BadgePosition parsed))
        {
            pos = parsed;
            return true;
        }
        pos = MapBadgeService.BadgePosition.Right;
        return false;
    }

    private static void TryRefresh()
    {
        try
        {
            if (Engine.GetMainLoop() is SceneTree tree)
                MapBadgeService.RefreshAllBadges(tree);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] refresh-on-toggle failed: {ex.Message}");
        }
    }
}
