using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppList = Il2CppSystem.Collections.Generic.List<string>;
using ItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.ValueTuple<string, int>>;

namespace FrierenPortrait;

[HarmonyPatch(typeof(TraitSheet), nameof(TraitSheet.Parse))]
internal static class FrierenTraitSheetPatch
{
    private static void Postfix(TraitSheet __instance)
    {
        FrierenGameplay.Register(__instance);
        Booklover.Register(__instance);
    }
}

[HarmonyPatch(typeof(AffecterSheet), nameof(AffecterSheet.Parse))]
internal static class FrierenAffecterSheetPatch
{
    private static void Postfix(AffecterSheet __instance)
    {
        FrierenGameplay.Register(__instance);
        Booklover.Register(__instance);
    }
}

[HarmonyPatch(typeof(RecruitHelper), nameof(RecruitHelper.CreateRecruitCandidate),
    new[] { typeof(ClanRankTableData), typeof(Il2CppSystem.Collections.Generic.HashSet<string>),
        typeof(Il2CppSystem.Collections.Generic.HashSet<string>) })]
internal static class FrierenRecruitCandidatePatch
{
    private static void Postfix(RecruitHelper __instance, RecruitCandidateData __result)
        => FrierenGameplay.ConfigureGeneratedCandidate(__instance, __result, true,
            false);
}

[HarmonyPatch(typeof(RecruitHelper), nameof(RecruitHelper.CreateEstablishCandidate),
    new[] { typeof(Il2CppSystem.Collections.Generic.HashSet<string>),
        typeof(Il2CppSystem.Collections.Generic.HashSet<string>), typeof(bool), typeof(EstablishUnitRestriction) })]
internal static class FrierenEstablishCandidatePatch
{
    private static void Postfix(RecruitHelper __instance, RecruitCandidateData __result)
        => FrierenGameplay.ConfigureCandidate(__instance, __result, false);
}

[HarmonyPatch(typeof(SkillComponent), nameof(SkillComponent.HasEquipBypass))]
internal static class FrierenMagicBypassPatch
{
    private static void Postfix(SkillComponent __instance, string __0, ref bool __result)
    {
        // Preserve the complete native path (including Fire Battle Mage) and
        // only widen a failed equipment check for Frieren's background trait.
        if (!__result && FrierenGameplay.IsMagicEquipment(__0)
            && FrierenGameplay.HasArchmageTrait(__instance._owner)) __result = true;
    }
}
