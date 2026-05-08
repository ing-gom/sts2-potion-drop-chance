using Godot;

namespace Sts2PotionDropChance;

/// <summary>
/// Loads the same map sprites the game uses for Monster / Elite nodes.
/// Resource paths come from <c>NNormalMapPoint.IconName</c> + <c>IconPath</c>.
/// </summary>
internal static class MapIconLoader
{
    private const string MonsterPath = "res://images/atlases/ui_atlas.sprites/map/icons/map_monster.tres";
    private const string ElitePath   = "res://images/atlases/ui_atlas.sprites/map/icons/map_elite.tres";

    private static Texture2D? _monster;
    private static Texture2D? _elite;

    public static Texture2D? Monster() =>
        _monster ??= ResourceLoader.Load<Texture2D>(MonsterPath, null, ResourceLoader.CacheMode.Reuse);

    public static Texture2D? Elite() =>
        _elite ??= ResourceLoader.Load<Texture2D>(ElitePath, null, ResourceLoader.CacheMode.Reuse);
}
