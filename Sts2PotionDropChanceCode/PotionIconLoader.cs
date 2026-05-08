using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace Sts2PotionDropChance;

/// <summary>
/// One-shot lookup for the game's empty-potion-slot texture. The icon is
/// declared in NPotionHolder.tscn (not exposed as a code path), so we steal
/// it at runtime from any live NPotionHolder in the scene tree on first
/// successful call. Cached for the rest of the session.
/// </summary>
internal static class PotionIconLoader
{
    private static Texture2D? _cached;
    private static readonly System.Reflection.FieldInfo? _emptyIconField =
        AccessTools.Field(typeof(NPotionHolder), "_emptyIcon");

    public static Texture2D? Get(SceneTree? tree)
    {
        if (_cached != null) return _cached;
        if (tree?.Root == null || _emptyIconField == null) return null;

        var holder = FindHolder(tree.Root);
        if (holder == null) return null;

        if (_emptyIconField.GetValue(holder) is TextureRect rect && rect.Texture != null)
            _cached = rect.Texture;

        return _cached;
    }

    private static NPotionHolder? FindHolder(Node node)
    {
        if (node is NPotionHolder h) return h;
        foreach (var child in node.GetChildren())
        {
            var found = FindHolder(child);
            if (found != null) return found;
        }
        return null;
    }
}
