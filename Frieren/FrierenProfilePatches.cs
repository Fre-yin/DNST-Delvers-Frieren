using HarmonyLib;

namespace FrierenPortrait;

// FrierenRuntime's profile hooks: the game's profile sheet and portrait database are read
// through these four patches, and each one makes sure that Frieren is registered before the
// game looks her up (FrierenRuntime.Ensure and EnsureVisuals).
[HarmonyPatch(typeof(UnitProfileSheet), nameof(UnitProfileSheet.Parse))]
internal static class ParsedProfilesPatch
{
    private static void Postfix(UnitProfileSheet __instance) => FrierenRuntime.Instance?.Ensure(__instance, true);
}

[HarmonyPatch(typeof(UnitProfileSheet), nameof(UnitProfileSheet.GetProfile))]
internal static class ProfilePoolPatch
{
    private static void Prefix(UnitProfileSheet __instance) => FrierenRuntime.Instance?.Ensure(__instance);
}

[HarmonyPatch(typeof(UnitProfileSheet), nameof(UnitProfileSheet.GetData))]
internal static class ProfileLookupPatch
{
    private static void Prefix(UnitProfileSheet __instance) => FrierenRuntime.Instance?.Ensure(__instance);
}

[HarmonyPatch(typeof(UnitVisualDatabase), nameof(UnitVisualDatabase.GetPortraitSprite))]
internal static class PortraitLookupPatch
{
    private static void Prefix(UnitVisualDatabase __instance, string key)
    {
        if (key == FrierenIds.PortraitKey) FrierenRuntime.Instance?.EnsureVisuals(__instance);
    }
}
