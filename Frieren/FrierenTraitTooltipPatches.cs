using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppList = Il2CppSystem.Collections.Generic.List<string>;
using ItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.ValueTuple<string, int>>;

namespace FrierenPortrait;

[HarmonyPatch(typeof(Tooltip_Affecter), "SetDetail")]
internal static class FrierenTraitTooltipPatch
{
    internal static bool RenderingArchmage;

    private static void Prefix(AffecterTableData __0)
        => RenderingArchmage = __0?.Key == FrierenGameplay.TraitKey;

    private static void Postfix(Tooltip_Affecter __instance, AffecterTableData __0)
    {
        RenderingArchmage = false;
        if (__0?.Key != FrierenGameplay.TraitKey) return;
        // The extra row reuses the prefab's native value rows, so it inherits the
        // stat-row font/color and stays in the layout before the description.
        __instance.AddStatRow(FrierenLocalization.TraitMasteryKey, string.Empty);
    }
}

// Show these stats like "Meister aller Magieschulen": the trait's own name without
// a value, so players discover the effect. The native stat values stay active.
[HarmonyPatch(typeof(Tooltip_Affecter), nameof(Tooltip_Affecter.AddStatRow))]
internal static class FrierenTraitStatRowNamePatch
{
    private static readonly (StatType Stat, string TextKey)[] NamedRows =
    {
        (StatType.MaxEnergy, FrierenLocalization.TraitManaReserveKey),
        (StatType.CooldownReduction, FrierenLocalization.TraitPracticedMageKey)
    };

    private static readonly bool[] RowLogged = new bool[NamedRows.Length];

    private static void Prefix(ref string __0, ref string __1)
    {
        if (!FrierenTraitTooltipPatch.RenderingArchmage || string.IsNullOrEmpty(__0)) return;
        for (var i = 0; i < NamedRows.Length; i++)
        {
            // Tooltip_Base.StatsToRows passes the localized stat name, not a text key.
            // Its fallback for a missing name is the key without the STATUS_ prefix.
            var statName = NamedRows[i].Stat.ToString();
            if (__0 != TextKeyExtensions.GetName("STATUS_" + statName) && __0 != statName) continue;
            if (!RowLogged[i])
            {
                RowLogged[i] = true;
                DelversHost.Info($"FRIEREN_TOOLTIP_NAMED_ROW stat={statName} nativeName={__0}");
            }
            __0 = NamedRows[i].TextKey;
            __1 = string.Empty;
            return;
        }
    }
}

[HarmonyPatch(typeof(Tooltip_Affecter), nameof(Tooltip_Affecter.Show), new[] { typeof(string) })]
internal static class FrierenTraitGrantedSkillsTooltipPatch
{
    private static bool capacityWarningLogged;

    private static void Postfix(Tooltip_Affecter __instance, string __0)
    {
        if (__0 != FrierenGameplay.TraitKey) return;
        try
        {
            var slots = __instance._skills;
            var count = slots?.Count ?? 0;
            var visible = Math.Min(count, FrierenMagicSkills.Granted.Length);
            for (var i = 0; i < count; i++)
            {
                var slot = slots[i];
                if (!slot) continue;
                var active = i < visible;
                if (active) slot.SetUI(FrierenMagicSkills.Granted[i], __instance.TooltipID);
                slot.gameObject.SetActive(active);
            }
            __instance.SetSkillSection(visible > 0);
            if (visible == FrierenMagicSkills.Granted.Length || capacityWarningLogged) return;
            capacityWarningLogged = true;
            DelversHost.Warning(
                $"FRIEREN_TRAIT_TOOLTIP_CAPACITY expected={FrierenMagicSkills.Granted.Length} actual={count}");
        }
        catch (Exception ex)
        {
            DelversHost.Error("FRIEREN_TRAIT_TOOLTIP_SKILLS_FAILED: " + ex);
        }
    }
}
