using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#if BEPINEX
using global::Refactor;
#else
using Il2CppRefactor;
#endif
#if BEPINEX
using global::Refactor.Addressable;
#else
using Il2CppRefactor.Addressable;
#endif
#if BEPINEX
using global::Refactor.Component;
#else
using Il2CppRefactor.Component;
#endif
#if BEPINEX
using global::Refactor.Main;
#else
using Il2CppRefactor.Main;
#endif
#if BEPINEX
using global::Refactor.Main.Event;
#else
using Il2CppRefactor.Main.Event;
#endif
#if BEPINEX
using global::Refactor.UI;
#else
using Il2CppRefactor.UI;
#endif
#if BEPINEX
using global::Refactor.Util;
#else
using Il2CppRefactor.Util;
#endif
#if BEPINEX
using global::Util.Sheet;
#else
using Il2CppUtil.Sheet;
#endif
using UnityEngine;
using Il2CppList = Il2CppSystem.Collections.Generic.List<string>;
using ItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.ValueTuple<string, int>>;

namespace FrierenPortrait;

internal static class FrierenGameplay
{
    internal const string TraitKey = FrierenUniqueCandidateRules.BackgroundTraitKey;
    internal const string NameKey = "Frieren";
    internal const string LegacyNameKey = "Danny_Frieren_Name";
    internal static string TraitName => FrierenLocalization.TraitName;
    internal static string TraitMastery => FrierenLocalization.TraitMastery;
    internal const float MaxEnergyBonus = 50f;
    internal const float CooldownReductionBonus = 20f;
    internal static string TraitDescription => FrierenLocalization.TraitDescription;
    internal static string TraitFlavor => FrierenLocalization.TraitFlavor;

    private static readonly string[] MagicEquipment =
        { nameof(ItemSubType.Equipment_FireStaff), nameof(ItemSubType.Equipment_WaterStaff),
          nameof(ItemSubType.Equipment_NatureStaff) };

    private static readonly string[] LevelInscriptions =
    {
        "AFFECTER_DivineInscription",
        "AFFECTER_HighGradeMagicAttackPowerInscription",
        "AFFECTER_CapitalInscription",
        "AFFECTER_HighGradeCooldownReductionInscription",
        "AFFECTER_ChallengeInscription",
        "AFFECTER_HighGradeCriticalDamageBonusInscription",
        "AFFECTER_StormInscription"
    };

    internal static void Register(TraitSheet sheet)
    {
        if (sheet?._traitTable == null || sheet._traitTable.ContainsKey(TraitKey)) return;
        var template = sheet.GetData("AFFECTER_Mage");
        if (template == null) throw new InvalidOperationException("Der originale Magier-Trait fehlt.");
        var trait = new TraitTableData
        {
            Key = TraitKey,
            DevComment1 = TraitName,
            DevComment2 = TraitDescription,
            DevComment3 = TraitFlavor,
            Type = AffecterType.Background,
            Rarity = Rarity.Legendary,
            MaxStack = 1,
            IsPositive = PositiveType.Positive,
            IsExposedToPlayer = true,
            AdjustStats = new Il2CppStringArray(0),
            AdjustStatsPerStack = new Il2CppStringArray(0),
            Categories = new Il2CppStringArray(0),
            FloatValues = new Il2CppStructArray<float>(0),
            BindingValue = string.Empty,
            RecruitAppearClanRank = 99
        };
        sheet._traitTable.Add(TraitKey, trait);
    }

    internal static void Register(AffecterSheet sheet)
    {
        if (sheet?._affecterTable == null || sheet._affecterTable.ContainsKey(TraitKey)) return;
        var template = sheet.GetData("AFFECTER_Mage");
        if (template == null) throw new InvalidOperationException("Die originale Magier-Eigenschaft fehlt.");
        var trait = new AffecterTableData
        {
            Key = TraitKey,
            DevComment1 = TraitName,
            DevComment2 = TraitDescription,
            DevComment3 = TraitFlavor,
            Type = AffecterType.Background,
            Categories = new Il2CppStringArray(0),
            MaxStack = 1,
            IsPositive = PositiveType.Positive,
            IsExposedToPlayer = true,
            AdjustStatsData = new Il2CppStringArray(0),
            AdjustStatsPerStackData = new Il2CppStringArray(0),
            FloatValues = new Il2CppStructArray<float>(0),
            BindingValue = string.Empty,
            AdjustStats = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>(),
            AdjustStatsPerStack = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>()
        };
        trait.AdjustStats.Add(StatType.PhysicalDamageDealtBonus, -40f);
        trait.AdjustStats.Add(StatType.MaxEnergy, MaxEnergyBonus);
        trait.AdjustStats.Add(StatType.CooldownReduction, CooldownReductionBonus);
        sheet._affecterTable.Add(TraitKey, trait);
    }

    internal static bool HasArchmageTrait(IEntity owner)
    {
        if (owner == null) return false;
        var affecters = EntityComponent._instance?.GetComponentOf<AffecterComponent>(owner);
        return affecters != null && affecters.Has(TraitKey);
    }

    internal static void ConfigureGeneratedCandidate(RecruitHelper helper, RecruitCandidateData candidate,
        bool guild, bool forceFrieren)
    {
        if (candidate == null) return;
        if (forceFrieren) candidate.UnitProfileKey = FrierenIds.ProfileKey;
        ConfigureCandidate(helper, candidate, guild);
    }

    internal static void ConfigureCandidate(RecruitHelper helper, RecruitCandidateData candidate, bool guild)
    {
        if (!ConfigureIdentityAndTraits(candidate, out var level)) return;
        ConfigureInscriptions(candidate, level);
        ConfigureStatsTalentsAndGrowth(helper, candidate, level);
        ConfigureSkillTrees(candidate);
        ConfigureEquipment(candidate, level);
        ConfigureRecruitPrice(helper, candidate, guild, level);
    }

    private static bool ConfigureIdentityAndTraits(RecruitCandidateData candidate, out int level)
    {
        level = 1;
        if (candidate == null || candidate.UnitProfileKey != FrierenIds.ProfileKey) return false;
        level = Math.Min(Math.Max(candidate.Level, 1), 8);
        candidate.Level = level;
        candidate.NameTextKey = NameKey;
        candidate.BioTextKey = FrierenLocalization.BiographyKey;
        candidate.RaceTrait = "AFFECTER_Elf";
        candidate.BackgroundTrait = TraitKey;
        candidate.IndividualTraits ??= new Il2CppList();
        candidate.IndividualTraits.Clear();
        candidate.IndividualTraits.Add(Booklover.TraitKey);
        return true;
    }

    private static void ConfigureInscriptions(RecruitCandidateData candidate, int level)
    {
        candidate.InscriptionsTraits ??= new Il2CppList();
        candidate.InscriptionsTraits.Clear();
        for (var i = 2; i <= level; i++) candidate.InscriptionsTraits.Add(LevelInscriptions[i - 2]);
    }

    private static void ConfigureStatsTalentsAndGrowth(RecruitHelper helper,
        RecruitCandidateData candidate, int level)
    {
        var stats = candidate.MajorStats;
        stats[StatType.Strength] = 11;
        stats[StatType.Constitution] = 12;
        stats[StatType.WillPower] = 16;
        stats[StatType.Intelligence] = 17;
        stats[StatType.Agility] = 12;
        stats[StatType.Perception] = 13;
        var talents = candidate.MajorStatTalents;
        talents[StatType.TalentStrength] = TalentType.Poor;
        talents[StatType.TalentConstitution] = TalentType.Moderate;
        talents[StatType.TalentWillPower] = TalentType.Genius;
        talents[StatType.TalentIntelligence] = TalentType.Genius;
        talents[StatType.TalentAgility] = TalentType.Moderate;
        talents[StatType.TalentPerception] = TalentType.Moderate;

        // Use the game's own level-up growth, but with a fixed roll for each
        // level. Frieren therefore has the same attributes in every campaign,
        // and generating her does not alter the campaign's random state.
        if (level > 1)
        {
            var previousRandom = UnityEngine.Random.state;
            try
            {
#if BEPINEX
                if (helper._levelUpHelper == null) helper._levelUpHelper = new global::Refactor.Main.LevelUpHelper();
#else
                if (helper._levelUpHelper == null) helper._levelUpHelper = new Il2CppRefactor.Main.LevelUpHelper();
#endif
                for (var nextLevel = 2; nextLevel <= level; nextLevel++)
                {
                    UnityEngine.Random.InitState(20260924 + nextLevel);
                    helper.ApplyLevelUpMajorStatGrowth(stats, talents);
                }
            }
            finally { UnityEngine.Random.state = previousRandom; }
        }
    }

    private static void ConfigureSkillTrees(RecruitCandidateData candidate)
    {
        candidate.MainSkillTrees.Clear();
        candidate.MainSkillTrees.Add(MainSkillTreeType.FireMagic);
        candidate.MainSkillTrees.Add(MainSkillTreeType.WaterMagic);
        candidate.MainSkillTrees.Add(MainSkillTreeType.NatureMagic);
        candidate.SubSkillTrees.Clear();
        candidate.SubSkillTrees.Add(SubSkillTreeType.Burst);
        candidate.SubSkillTrees.Add(SubSkillTreeType.Tide);
        candidate.SubSkillTrees.Add(SubSkillTreeType.Harmony);
    }

    private static void ConfigureEquipment(RecruitCandidateData candidate, int level)
    {
        var (armor, head, staff) = level <= 2
            ? ("ITEM_ClothArmor", "ITEM_ClothCoif", "ITEM_FireWoodStaff")
            : level <= 4
                ? ("ITEM_HideArmor", "ITEM_HideHelmet", "ITEM_FireBoneStaff")
                : ("ITEM_LeatherArmor", "ITEM_LeatherHelmet", "ITEM_FireCarapaceStaff");
        candidate.InitialEquipments ??= new ItemList();
        candidate.InitialEquipments.Clear();
        candidate.InitialEquipments.Add(new Il2CppSystem.ValueTuple<string, int>(armor, 1));
        candidate.InitialEquipments.Add(new Il2CppSystem.ValueTuple<string, int>(head, 1));
        candidate.InitialEquipments.Add(new Il2CppSystem.ValueTuple<string, int>(staff, 1));
        candidate.InitialInventoryItems ??= new ItemList();
        candidate.InitialInventoryItems.Clear();
    }

    private static void ConfigureRecruitPrice(RecruitHelper helper, RecruitCandidateData candidate,
        bool guild, int level)
    {
        if (guild)
            candidate.RecruitPrice = helper.CalculateRecruitPrice(level, candidate.InitialEquipments,
                candidate.InitialInventoryItems, candidate.InscriptionsTraits, candidate.BackgroundTrait);
    }

    internal static bool IsMagicEquipment(string key) => Array.IndexOf(MagicEquipment, key) >= 0;
}

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
