using HarmonyLib;

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
    // The game applies the player's locks inside this method, so Frieren must be
    // kept out before the profile pick instead of overwriting the locks afterwards.
    private static void Prefix(ref Il2CppSystem.Collections.Generic.HashSet<string> alreadyExistPortraits,
        EstablishUnitRestriction restriction)
    {
        try { alreadyExistPortraits = FrierenGameplay.ExcludeWhenLocked(alreadyExistPortraits, restriction); }
        catch (Exception ex) { DelversHost.Error("FRIEREN_ESTABLISH_LOCK_EXCLUSION_FAILED: " + ex); }
    }

    private static void Postfix(RecruitHelper __instance, RecruitCandidateData __result)
        => FrierenGameplay.ConfigureCandidate(__instance, __result, false);
}

[HarmonyPatch(typeof(SkillComponent), nameof(SkillComponent.HasEquipBypass))]
internal static class FrierenMagicBypassPatch
{
    private static void Postfix(SkillComponent __instance, string key, ref bool __result)
    {
        // Preserve the complete native path (including Fire Battle Mage) and
        // only widen a failed equipment check for Frieren's background trait.
        if (!__result && FrierenGameplay.IsMagicEquipment(key)
            && FrierenGameplay.HasArchmageTrait(__instance._owner)) __result = true;
    }
}
