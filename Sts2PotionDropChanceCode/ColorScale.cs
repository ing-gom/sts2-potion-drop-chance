using Godot;

namespace Sts2PotionDropChance;

/// <summary>
/// 5-stop discrete-tier interpolation. Stops chosen so that adjacent tiers are
/// visually distinct — the 100% stop is teal (cool green) instead of lime so
/// it doesn't blur into the 75% lime-green stop.
/// 0% red → 25% orange → 50% yellow → 75% lime → 100% teal.
/// </summary>
internal static class ColorScale
{
    private static readonly (float P, Color C)[] Stops =
    {
        (0.00f, new Color(0.86f, 0.15f, 0.15f)), // #dc2626 red
        (0.25f, new Color(0.98f, 0.45f, 0.09f)), // #f97316 orange
        (0.50f, new Color(0.92f, 0.70f, 0.03f)), // #eab308 yellow
        (0.75f, new Color(0.52f, 0.80f, 0.09f)), // #84cc16 lime
        (1.00f, new Color(0.05f, 0.58f, 0.53f)), // #0d9488 teal
    };

    public static Color For(float p)
    {
        p = p < 0f ? 0f : (p > 1f ? 1f : p);
        for (int i = 0; i < Stops.Length - 1; i++)
        {
            var (p0, c0) = Stops[i];
            var (p1, c1) = Stops[i + 1];
            if (p <= p1)
                return c0.Lerp(c1, (p - p0) / (p1 - p0));
        }
        return Stops[Stops.Length - 1].C;
    }
}
