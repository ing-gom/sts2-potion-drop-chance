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
///   ModConfig.ConfigEntry { Key, Type, Label, Description, DefaultValue, OnChanged, ... }
///   ModConfig.ConfigType { Toggle, Slider, Dropdown, ... }
/// </summary>
internal static class ModConfigBridge
{
    private const string EntryKeyHideUnknown = "hideUnknown";

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

            var entry = Activator.CreateInstance(entryType)
                ?? throw new InvalidOperationException("ConfigEntry instance creation returned null.");
            SetProp(entry, "Key", EntryKeyHideUnknown);
            SetProp(entry, "Type", toggleValue);
            SetProp(entry, "Label", "Hide on ? (Unknown) nodes");
            SetProp(entry, "Description",
                "Suppresses the potion drop chance badge on Unknown (?) map nodes. Monster and Elite nodes are unaffected.");
            SetProp(entry, "DefaultValue", false);

            Action<object?> onChanged = v =>
            {
                MapBadgeService.HideUnknownNodes = v is bool b && b;
                // Apply immediately to badges already on screen — without this the
                // change only takes effect on the next natural RefreshVisualsInstantly.
                try
                {
                    if (Engine.GetMainLoop() is SceneTree tree)
                        MapBadgeService.RefreshAllBadges(tree);
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Warn($"[{MainFile.ModId}] refresh-on-toggle failed: {ex.Message}");
                }
            };
            SetProp(entry, "OnChanged", onChanged);

            var entriesArray = Array.CreateInstance(entryType, 1);
            entriesArray.SetValue(entry, 0);

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

            // Pull the persisted value so the toggle is honored from the very first map screen.
            var getValue = apiType.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static);
            if (getValue != null && getValue.IsGenericMethodDefinition)
            {
                try
                {
                    var typed = getValue.MakeGenericMethod(typeof(bool));
                    var saved = typed.Invoke(null, new object?[] { MainFile.ModId, EntryKeyHideUnknown });
                    if (saved is bool b) MapBadgeService.HideUnknownNodes = b;
                }
                catch { /* GetValue<bool> not callable yet — OnChanged will sync later. */ }
            }

            MainFile.Logger.Info($"[{MainFile.ModId}] ModConfig integration active (toggle: {EntryKeyHideUnknown}).");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] ModConfig register failed: {ex.Message}");
        }
    }

    private static void SetProp(object target, string name, object? value)
    {
        var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        p?.SetValue(target, value);
    }
}
