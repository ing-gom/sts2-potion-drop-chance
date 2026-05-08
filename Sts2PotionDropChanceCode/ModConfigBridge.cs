using System;
using System.Linq;
using System.Reflection;
using Godot;

namespace Sts2PotionDropChance;

/// <summary>
/// Optional integration with the ModConfig framework (Nexus #27). All access is via
/// reflection so this mod has zero hard dependency: when ModConfig is missing, the
/// probe silently no-ops and the badge service runs with default settings.
/// Resolved API surface (ModConfig 0.2.3):
///   ModConfig.ModConfigApi.Register(string modId, string modName, ConfigEntry[] entries)
///   ModConfig.ModConfigApi.GetValue&lt;T&gt;(string modId, string key)
///   ModConfig.ConfigEntry { Key, Type, Label, Description, DefaultValue, Options, OnChanged, ... }
///   ModConfig.ConfigType { Toggle, Slider, Dropdown, ... }
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

        Type? apiType = null;
        Type? entryType = null;
        Type? configTypeEnum = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            apiType = asm.GetType("ModConfig.ModConfigApi", throwOnError: false);
            if (apiType != null)
            {
                entryType = asm.GetType("ModConfig.ConfigEntry", throwOnError: false);
                configTypeEnum = asm.GetType("ModConfig.ConfigType", throwOnError: false);
                break;
            }
        }
        if (apiType == null || entryType == null || configTypeEnum == null)
        {
            MainFile.Logger.Info($"[{MainFile.ModId}] ModConfig not found; in-game settings tab skipped.");
            return;
        }

        try
        {
            var toggleValue = Enum.Parse(configTypeEnum, "Toggle");
            var dropdownValue = Enum.Parse(configTypeEnum, "Dropdown");

            // Entry 1 — Hide on Unknown (toggle).
            var hideEntry = Activator.CreateInstance(entryType)
                ?? throw new InvalidOperationException("ConfigEntry instance creation returned null.");
            SetProp(hideEntry, "Key", EntryKeyHideUnknown);
            SetProp(hideEntry, "Type", toggleValue);
            SetProp(hideEntry, "Label", "Hide on ? (Unknown) nodes");
            SetProp(hideEntry, "Description",
                "Suppresses the potion drop chance badge on Unknown (?) map nodes. Monster and Elite nodes are unaffected.");
            SetProp(hideEntry, "DefaultValue", false);
            Action<object?> onHideChanged = v =>
            {
                MapBadgeService.HideUnknownNodes = v is bool b && b;
                TryRefresh();
            };
            SetProp(hideEntry, "OnChanged", onHideChanged);

            // Entry 2 — Badge position (dropdown).
            var posEntry = Activator.CreateInstance(entryType)
                ?? throw new InvalidOperationException("ConfigEntry instance creation returned null.");
            SetProp(posEntry, "Key", EntryKeyPosition);
            SetProp(posEntry, "Type", dropdownValue);
            SetProp(posEntry, "Label", "Badge position");
            SetProp(posEntry, "Description",
                "Where the potion drop chance badge appears relative to the map node.");
            SetProp(posEntry, "DefaultValue", PositionOptions[0]); // "Right"
            SetProp(posEntry, "Options", PositionOptions);
            Action<object?> onPosChanged = v =>
            {
                if (TryParsePosition(v, out var p))
                {
                    MapBadgeService.Position = p;
                    TryRefresh();
                }
            };
            SetProp(posEntry, "OnChanged", onPosChanged);

            var entriesArray = Array.CreateInstance(entryType, 2);
            entriesArray.SetValue(hideEntry, 0);
            entriesArray.SetValue(posEntry, 1);

            var register = apiType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Register"
                                     && m.GetParameters().Length == 3
                                     && m.GetParameters()[1].ParameterType == typeof(string));
            if (register == null)
            {
                MainFile.Logger.Warn($"[{MainFile.ModId}] ModConfigApi.Register(string,string,ConfigEntry[]) not found; skipping.");
                return;
            }
            register.Invoke(null, new object?[] { MainFile.ModId, "Potion Drop Chance", entriesArray });

            // Pull persisted values so toggles are honored from the very first map screen.
            var getValue = apiType.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static);
            if (getValue != null && getValue.IsGenericMethodDefinition)
            {
                try
                {
                    var typedBool = getValue.MakeGenericMethod(typeof(bool));
                    if (typedBool.Invoke(null, new object?[] { MainFile.ModId, EntryKeyHideUnknown }) is bool b)
                        MapBadgeService.HideUnknownNodes = b;
                }
                catch { /* GetValue<bool> not callable yet — OnChanged will sync later. */ }

                try
                {
                    var typedString = getValue.MakeGenericMethod(typeof(string));
                    var savedPos = typedString.Invoke(null, new object?[] { MainFile.ModId, EntryKeyPosition });
                    if (TryParsePosition(savedPos, out var p))
                        MapBadgeService.Position = p;
                }
                catch { /* GetValue<string> not callable yet — OnChanged will sync later. */ }
            }

            MainFile.Logger.Info($"[{MainFile.ModId}] ModConfig integration active (entries: {EntryKeyHideUnknown}, {EntryKeyPosition}).");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] ModConfig register failed: {ex.Message}");
        }
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

    private static void SetProp(object target, string name, object? value)
    {
        var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        p?.SetValue(target, value);
    }
}
