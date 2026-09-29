using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppList = Il2CppSystem.Collections.Generic.List<string>;
using ItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.ValueTuple<string, int>>;

namespace FrierenPortrait;

internal static class FrierenGameplay
{
    internal const string TraitKey = FrierenUniqueCandidateRules.BackgroundTraitKey;
    internal const string NameKey = "Frieren";
    internal static readonly string LegacyNameKey = FrierenLegacyIds.NameKey;
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
        ConfigureEquipment(helper, candidate, level);
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
                if (helper._levelUpHelper == null) helper._levelUpHelper = new LevelUpHelper();
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

    // The (item, count) tuples come from the game's own RecruitHelper.ParseItems
    // (see FrierenEquipmentRules). A ValueTuple<string, int> created in managed
    // code and added to the native list crashed BepInEx 6 be.788 (Il2CppInterop
    // 1.5.3) with an access violation in UnitSpawner.SpawnPlayableUnit when
    // Frieren spawned as a new unit; MelonLoader was unaffected.
    private static bool equipmentFormatLogged;
    private static string nativeItemSample;

    private static void ConfigureEquipment(RecruitHelper helper, RecruitCandidateData candidate, int level)
    {
        var (armor, head, staff) = level <= 2
            ? ("ITEM_ClothArmor", "ITEM_ClothCoif", "ITEM_FireWoodStaff")
            : level <= 4
                ? ("ITEM_HideArmor", "ITEM_HideHelmet", "ITEM_FireBoneStaff")
                : ("ITEM_LeatherArmor", "ITEM_LeatherHelmet", "ITEM_FireCarapaceStaff");
        var keys = new[] { armor, head, staff };

        nativeItemSample ??= FindNativeItemSample();
        var entries = keys.Select(key => FrierenEquipmentRules.SheetEntry(key, nativeItemSample)).ToArray();
        ItemList parsed = null;
        Exception parseError = null;
        try { parsed = helper?.ParseItems(new Il2CppStringArray(entries)); }
        catch (Exception ex) { parseError = ex; }

        var read = NativeItemReader.TryRead(parsed, out var items, out var readError);
        if (read && FrierenEquipmentRules.IsOneOfEach(items, keys))
        {
            candidate.InitialEquipments = parsed;
            if (!equipmentFormatLogged)
            {
                equipmentFormatLogged = true;
                DelversHost.Info("FRIEREN_EQUIPMENT_NATIVE_PARSED entries=" + string.Join("|", entries));
            }
        }
        else if (DelversCoreRuntime.LoaderProfile == "Melon")
        {
            // Proven under MelonLoader; kept only as its fallback.
            candidate.InitialEquipments ??= new ItemList();
            candidate.InitialEquipments.Clear();
            foreach (var key in keys)
                candidate.InitialEquipments.Add(new Il2CppSystem.ValueTuple<string, int>(key, 1));
        }
        else if (!equipmentFormatLogged)
        {
            // Keep the generator's native equipment rather than risk the crash.
            equipmentFormatLogged = true;
            DelversHost.Warning("FRIEREN_EQUIPMENT_KEPT_NATIVE items=" + string.Join(",", keys)
                + " sent=" + string.Join("|", entries)
                + " nativeSample=" + (nativeItemSample ?? "none")
                + " parsed=" + (read ? Describe(items) : "unreadable(" + readError + ")")
                + " parseError=" + (parseError?.Message ?? "none"));
        }

        if (candidate.InitialInventoryItems != null) candidate.InitialInventoryItems.Clear();
        else if (helper != null) candidate.InitialInventoryItems = helper.ParseItems(new Il2CppStringArray(0));
    }

    // A native background row's first starting item confirms the sheet format
    // at runtime; the parsed result is still verified before it is used.
    private static string FindNativeItemSample()
    {
        var table = DataSheetManager.Instance?._trait?._traitTable;
        if (table == null) return null;
        foreach (var trait in table.Values)
        {
            var defaults = trait?.DefaultEquipments;
            if (defaults == null) continue;
            for (var i = 0; i < defaults.Length; i++)
                if (defaults[i]?.StartsWith("ITEM_", StringComparison.Ordinal) == true) return defaults[i];
        }
        return null;
    }

    private static string Describe(List<(string Key, int Count)> items)
        => items.Count == 0 ? "empty" : string.Join("|", items.Select(item => item.Key + "x" + item.Count));

    private static void ConfigureRecruitPrice(RecruitHelper helper, RecruitCandidateData candidate,
        bool guild, int level)
    {
        if (guild)
            candidate.RecruitPrice = helper.CalculateRecruitPrice(level, candidate.InitialEquipments,
                candidate.InitialInventoryItems, candidate.InscriptionsTraits, candidate.BackgroundTrait);
    }

    internal static bool IsMagicEquipment(string key) => Array.IndexOf(MagicEquipment, key) >= 0;
}
