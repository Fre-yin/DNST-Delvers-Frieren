using HarmonyLib;

namespace FrierenPortrait;

// Every hook on TextKeyExtensions runs for each text the game shows and costs a managed
// string per call, so only the lookups the text table cannot answer are hooked:
// - GetName/GetDesc/GetFlavor read "TEXTKEY_<key>_NAME|_DESC|_FLAVOR" from the table;
//   FrierenLocalization registers those rows, so they need no hook.
// - GetNameText and TryGetNameTextIncludeRemnant return a key without the TEXTKEY
//   prefix unchanged, so Frieren's name key "Frieren" needs them for her localized name.
// - GetText reports "TextKey not found" when the text equals its key, which is the case
//   for "Frieren" in every Latin-script language (self-test).

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.GetNameText))]
internal static class FrierenNameTextPatch
{
    private static bool Prefix(string nameTextKey, ref string __result)
    {
        if (nameTextKey != FrierenGameplay.NameKey && nameTextKey != FrierenGameplay.LegacyNameKey) return true;
        __result = FrierenLocalization.Name;
        return false;
    }
}

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.GetText))]
internal static class FrierenRawNameTextPatch
{
    private static bool Prefix(string textKey, ref string __result)
    {
        if (textKey != FrierenGameplay.NameKey && textKey != FrierenGameplay.LegacyNameKey) return true;
        __result = FrierenLocalization.Name;
        return false;
    }
}

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.TryGetNameTextIncludeRemnant))]
internal static class FrierenRemnantNameTextPatch
{
    private static bool Prefix(string nameTextKey, ref string __result)
    {
        if (nameTextKey != FrierenGameplay.NameKey && nameTextKey != FrierenGameplay.LegacyNameKey) return true;
        __result = FrierenLocalization.Name;
        return false;
    }
}
