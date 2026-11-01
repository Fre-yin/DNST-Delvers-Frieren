using HarmonyLib;
using UnityEngine;

namespace FrierenPortrait;

// Zoltraak's hooks: the five patches that add the skill and the break row when the game reads its
// sheets, create the skill through the template's class, rebuild the line area after the skill's
// OnInit and answer the skill icon lookup. The postfix on AffecterSheet.Parse runs after Frieren's own.
[HarmonyPatch(typeof(SkillSheet), nameof(SkillSheet.Parse))]
internal static class ZoltraakSkillSheetPatch
{
    private static void Postfix(SkillSheet __instance)
    {
        Zoltraak.Register(__instance);
        Zoltraak.EnsureChannelRegistered();
    }
}

[HarmonyPatch(typeof(AffecterSheet), nameof(AffecterSheet.Parse))]
internal static class ZoltraakAffecterSheetPatch
{
    private static void Postfix(AffecterSheet __instance) => Zoltraak.RegisterBreak(__instance);
}

[HarmonyPatch(typeof(Skill_Base), nameof(Skill_Base.Create))]
internal static class ZoltraakCreatePatch
{
    private static bool Prefix(string key, ref Skill_Base __result)
    {
        if (key != Zoltraak.SkillKey) return true;
        try { __result = Zoltraak.Create(); }
        catch (Exception ex) { DelversHost.Error("FRIEREN_ZOLTRAAK_CREATE_FAILED: " + ex); __result = null; }
        return false;
    }
}

// OnInit has a real body in ActiveSkill_Base (it builds the execution area), unlike the empty
// Skill_Base.OnInit, so this hook only runs for active skills.
[HarmonyPatch(typeof(ActiveSkill_Base), "OnInit")]
internal static class ZoltraakAreaPatch
{
    private static void Postfix(ActiveSkill_Base __instance)
    {
        try
        {
            if (__instance?._Key_k__BackingField == Zoltraak.SkillKey) Zoltraak.UseStraightLine(__instance);
        }
        catch (Exception ex) { DelversHost.Error("FRIEREN_ZOLTRAAK_AREA_FAILED: " + ex); }
    }
}

[HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.LoadSkillIcon))]
internal static class ZoltraakIconPatch
{
    private static bool Prefix(string key, ref Sprite __result)
    {
        if (key != Zoltraak.SkillKey) return true;
        var sprite = Zoltraak.Icon();
        if (!sprite) return true;   // falls back to the game's lookup for our key
        __result = sprite;
        return false;
    }
}
