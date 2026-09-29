using HarmonyLib;
using UnityEngine;

namespace FrierenPortrait;

internal static class FrierenIconRegistry
{
    internal static bool TryGet(string key, out Sprite icon)
    {
        var assets = FrierenRuntime.Instance?.Assets;
        if (key == FrierenGameplay.TraitKey)
        {
            icon = assets?.TraitIcon;
            return icon;
        }
        if (key == Booklover.TraitKey || key == Booklover.MoodKey)
        {
            icon = assets?.BookloverIcon;
            return icon;
        }
        icon = null;
        return false;
    }

    internal static bool Prefix(string key, ref Sprite result)
    {
        if (!TryGet(key, out var icon)) return true;
        result = icon;
        return false;
    }
}

[HarmonyPatch(typeof(GameResourceLoader), nameof(GameResourceLoader.LoadAffecterIcon))]
internal static class FrierenAffecterIconPatch
{
    private static bool Prefix(string __0, ref Sprite __result)
        => FrierenIconRegistry.Prefix(__0, ref __result);
}

[HarmonyPatch(typeof(GameResourceLoader), nameof(GameResourceLoader.LoadAffecterIconWithSkill))]
internal static class FrierenAffecterIconWithSkillPatch
{
    private static bool Prefix(string __0, ref Sprite __result)
        => FrierenIconRegistry.Prefix(__0, ref __result);
}
