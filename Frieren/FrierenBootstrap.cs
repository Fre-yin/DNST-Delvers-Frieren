namespace FrierenPortrait;

internal static class FrierenBootstrap
{
    internal static string SupportedBuilds => DelversCoreRuntime.SupportedBuilds;

    internal static void Initialize(HarmonyLib.Harmony harmony)
    {
        if (!DelversCoreRuntime.IsReady || !DelversCoreRuntime.SupportsApi(
                DelversCoreRuntime.MinimumApiVersionForNativeValueLists))
            throw new InvalidOperationException("Dungeon Settlers Delvers Core API 1.4.0 muss vor Frieren geladen sein.");
        LegacyFrierenConflict.ThrowIfPresent();
        PortraitAssets.ValidateFiles();
        harmony.PatchAll(typeof(FrierenBootstrap).Assembly);
    }

}
