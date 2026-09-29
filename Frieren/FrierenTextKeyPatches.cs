using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppList = Il2CppSystem.Collections.Generic.List<string>;
using ItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.ValueTuple<string, int>>;

namespace FrierenPortrait;

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.GetName))]
internal static class FrierenTraitNamePatch
{
    private static bool Prefix(string __0, ref string __result)
    {
        if (__0 != FrierenGameplay.TraitKey) return true;
        __result = FrierenGameplay.TraitName;
        return false;
    }
}

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.GetDesc))]
internal static class FrierenTraitDescriptionPatch
{
    private static bool Prefix(string __0, ref string __result)
    {
        if (__0 != FrierenGameplay.TraitKey) return true;
        __result = FrierenGameplay.TraitDescription;
        return false;
    }
}

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.GetFlavor))]
internal static class FrierenTraitFlavorPatch
{
    private static bool Prefix(string __0, ref string __result)
    {
        if (__0 != FrierenGameplay.TraitKey) return true;
        __result = FrierenGameplay.TraitFlavor;
        return false;
    }
}

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.GetNameText))]
internal static class FrierenNameTextPatch
{
    private static bool Prefix(string __0, ref string __result)
    {
        if (__0 != FrierenGameplay.NameKey && __0 != FrierenGameplay.LegacyNameKey) return true;
        __result = "Frieren";
        return false;
    }
}

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.GetText))]
internal static class FrierenRawNameTextPatch
{
    private static bool Prefix(string __0, ref string __result)
    {
        if (__0 != FrierenGameplay.NameKey && __0 != FrierenGameplay.LegacyNameKey) return true;
        __result = "Frieren";
        return false;
    }
}

[HarmonyPatch(typeof(TextKeyExtensions), nameof(TextKeyExtensions.TryGetNameTextIncludeRemnant))]
internal static class FrierenRemnantNameTextPatch
{
    private static bool Prefix(string __0, ref string __result)
    {
        if (__0 != FrierenGameplay.NameKey && __0 != FrierenGameplay.LegacyNameKey) return true;
        __result = "Frieren";
        return false;
    }
}
