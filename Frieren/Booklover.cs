using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace FrierenPortrait;

internal static class Booklover
{
    internal const string TraitKey = FrierenFixedTraitRules.BookloverTraitKey;
    internal const string MoodKey = FrierenSaveIds.BookloverMoodKey;
    internal const int MoodDurationSeconds = 20 * 60;

    // Ordinary individual traits are selected from the race row in TraitSheet.
    // Each native race assigns weight 100 to its ordinary individual traits.
    private static readonly string[] RaceKeys =
    {
        "AFFECTER_Human", "AFFECTER_LizardMan", "AFFECTER_Elf", "AFFECTER_Lycan",
        "AFFECTER_Dwarf", "AFFECTER_WhiteDemon", "AFFECTER_Tal"
    };

    internal static bool HasTrait(IEntity owner)
    {
        if (owner == null) return false;
        var affecters = EntityComponent._instance?.GetComponentOf<AffecterComponent>(owner);
        return affecters != null && affecters.Has(TraitKey);
    }

    internal static void Register(TraitSheet sheet)
    {
        if (sheet?._traitTable == null) return;
        var template = sheet.GetData("AFFECTER_SlowLearner")
            ?? throw new InvalidOperationException("Der originale Slow-Learner-Trait fehlt.");
        var existing = sheet.GetData(TraitKey);
        if (existing != null && (existing.Type != AffecterType.Individual || existing.Rarity != Rarity.Common))
            throw new InvalidOperationException("Die Booklover-Traitkennung ist bereits inkompatibel belegt.");

        // Preflight all seven native race pools before publishing the trait.
        var updates = new List<(TraitTableData race, Il2CppStringArray keys, Il2CppStructArray<int> weights)>();
        foreach (var raceKey in RaceKeys)
        {
            var race = sheet.GetData(raceKey)
                ?? throw new InvalidOperationException("Der originale Trait-Pool fehlt: " + raceKey);
            var keys = race.AvailableIndividualTraits;
            var weights = race.IndividualTraitWeights;
            if (keys == null || weights == null || keys.Length != weights.Length)
                throw new InvalidOperationException("Der originale Trait-Pool ist unvollständig: " + raceKey);
            var present = false;
            for (var i = 0; i < keys.Length; i++)
            {
                if (keys[i] != TraitKey) continue;
                if (present || weights[i] != 100)
                    throw new InvalidOperationException("Der Booklover-Trait steht mehrfach oder mit falschem Gewicht in " + raceKey);
                present = true;
            }
            if (present) continue;
            var extendedKeys = new Il2CppStringArray(keys.Length + 1);
            var extendedWeights = new Il2CppStructArray<int>(weights.Length + 1);
            for (var i = 0; i < keys.Length; i++)
            {
                extendedKeys[i] = keys[i];
                extendedWeights[i] = weights[i];
            }
            extendedKeys[keys.Length] = TraitKey;
            extendedWeights[weights.Length] = 100;
            updates.Add((race, extendedKeys, extendedWeights));
        }

        if (existing == null)
        {
            var trait = new TraitTableData
            {
                Key = TraitKey,
                DevComment1 = "Booklover",
                DevComment2 = "Every consumed technique book grants two main and two secondary skill points and +4 mood for 20 minutes.",
                DevComment3 = "Just one more chapter. Sleep is overrated anyway.",
                Type = AffecterType.Individual,
                Rarity = Rarity.Common,
                MaxStack = template.MaxStack,
                IsPositive = PositiveType.Positive,
                IsExposedToPlayer = true,
                IsAbsolute = template.IsAbsolute,
                Categories = template.Categories,
                RecruitAppearClanRank = template.RecruitAppearClanRank,
                AdjustStats = new Il2CppStringArray(0),
                AdjustStatsPerStack = new Il2CppStringArray(0),
                FloatValues = new Il2CppStructArray<float>(0),
                BindingValue = template.BindingValue,
                DefaultEquipments = template.DefaultEquipments,
                DefaultItems = template.DefaultItems,
                AvailableBackgroundTraits = template.AvailableBackgroundTraits,
                BackgroundTraitWeights = template.BackgroundTraitWeights,
                AvailableIndividualTraits = template.AvailableIndividualTraits,
                IndividualTraitWeights = template.IndividualTraitWeights,
                AvaliableMainSkillTreeTypes = template.AvaliableMainSkillTreeTypes,
                AvaliableSubSkillTreeTypes = template.AvaliableSubSkillTreeTypes,
                SubSkillTreeWeights = template.SubSkillTreeWeights,
                StrengthPerModifierMin = template.StrengthPerModifierMin,
                StrengthPerModifierMax = template.StrengthPerModifierMax,
                ConstitutionPerModifierMin = template.ConstitutionPerModifierMin,
                ConstitutionPerModifierMax = template.ConstitutionPerModifierMax,
                WillPowerPerModifierMin = template.WillPowerPerModifierMin,
                WillPowerPerModifierMax = template.WillPowerPerModifierMax,
                IntelligencePerModifierMin = template.IntelligencePerModifierMin,
                IntelligencePerModifierMax = template.IntelligencePerModifierMax,
                AgilityPerModifierMin = template.AgilityPerModifierMin,
                AgilityPerModifierMax = template.AgilityPerModifierMax,
                PerceptionPerModifierMin = template.PerceptionPerModifierMin,
                PerceptionPerModifierMax = template.PerceptionPerModifierMax
            };
            sheet._traitTable.Add(TraitKey, trait);
        }
        foreach (var update in updates)
        {
            update.race.AvailableIndividualTraits = update.keys;
            update.race.IndividualTraitWeights = update.weights;
        }
    }

    internal static void Register(AffecterSheet sheet)
    {
        if (sheet?._affecterTable == null) return;
        if (sheet.GetData(TraitKey) == null)
        {
            var template = sheet.GetData("AFFECTER_SlowLearner")
                ?? throw new InvalidOperationException("Die originale Slow-Learner-Eigenschaft fehlt.");
            var trait = new AffecterTableData
            {
                Key = TraitKey,
                DevComment1 = "Booklover",
                DevComment2 = "Every consumed technique book grants two main and two secondary skill points and +4 mood for 20 minutes.",
                DevComment3 = "Just one more chapter. Sleep is overrated anyway.",
                Type = AffecterType.Individual,
                MaxStack = template.MaxStack,
                IsPositive = PositiveType.Positive,
                IsExposedToPlayer = true,
                IsAbsolute = template.IsAbsolute,
                Categories = template.Categories,
                AdjustStatsData = new Il2CppStringArray(0),
                AdjustStatsPerStackData = new Il2CppStringArray(0),
                FloatValues = new Il2CppStructArray<float>(0),
                BindingValue = string.Empty,
                AdjustStats = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>(),
                AdjustStatsPerStack = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>()
            };
            sheet._affecterTable.Add(TraitKey, trait);
        }

        if (sheet.GetData(MoodKey) == null)
        {
            var template = sheet.GetData("AFFECTER_PlayedCard")
                ?? throw new InvalidOperationException("Der originale Kartenspiel-Stimmungsbuff fehlt.");
            var mood = new AffecterTableData
            {
                Key = MoodKey,
                DevComment1 = "Booklover reading joy",
                DevComment2 = "+4 mood for 20 minutes after reading a technique book.",
                Type = template.Type,
                MaxStack = template.MaxStack,
                IsPositive = PositiveType.Positive,
                HasDuration = true,
                IsAbsolute = template.IsAbsolute,
                IsExposedToPlayer = true,
                Categories = template.Categories,
                AdjustStatsData = new Il2CppStringArray(new[] { "Mood_4" }),
                AdjustStatsPerStackData = new Il2CppStringArray(0),
                FloatValues = new Il2CppStructArray<float>(new[] { (float)MoodDurationSeconds }),
                BindingValue = string.Empty,
                AdjustStats = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>(),
                AdjustStatsPerStack = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>()
            };
            mood.AdjustStats.Add(StatType.Mood, 4f);
            sheet._affecterTable.Add(MoodKey, mood);
        }
    }
}
