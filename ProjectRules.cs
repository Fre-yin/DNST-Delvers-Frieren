using System.Text;
using System.Text.RegularExpressions;

namespace FrierenPortrait;

internal static class FrierenNameMigrationRules
{
    internal static bool ShouldMigrate(string profileKey, string existingName,
        string frierenProfileKey, string legacyNameKey, string currentNameKey)
        => profileKey == frierenProfileKey && existingName == legacyNameKey
            && legacyNameKey != currentNameKey;
}

internal static class FrierenBiographyMigrationRules
{
    internal static bool ShouldMigrate(string profileKey, string existingBiography,
        string frierenProfileKey)
        => string.Equals(profileKey, frierenProfileKey, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(existingBiography);
}

public static class CompatibilityModIdentity
{
    public const string ExtendedHotbarAssembly = "DungeonSettlers10Slots";
    public const string ExtendedHotbarName = "Extended Hotbar";
    // Patch releases (1.0.x) keep compatibility; Frieren neither references nor patches Hotbar.
    public const string SupportedExtendedHotbarVersion = "1.0.x";

    public static bool IsExtendedHotbar(string assemblyName, string modName)
        => string.Equals(assemblyName, ExtendedHotbarAssembly, StringComparison.OrdinalIgnoreCase)
            || string.Equals(modName, ExtendedHotbarName, StringComparison.OrdinalIgnoreCase);

    public static ExtendedHotbarCompatibilityStatus ClassifyExtendedHotbar(
        string assemblyName, string modName, string version)
    {
        if (!IsExtendedHotbar(assemblyName, modName))
            return ExtendedHotbarCompatibilityStatus.NotDetected;
        if (string.IsNullOrWhiteSpace(version))
            return ExtendedHotbarCompatibilityStatus.UnknownVersion;
        if (!Version.TryParse(version.Trim().TrimStart('v', 'V'), out var parsed))
            return ExtendedHotbarCompatibilityStatus.UnknownVersion;
        return parsed.Major == 1 && parsed.Minor == 0
            ? ExtendedHotbarCompatibilityStatus.Compatible
            : ExtendedHotbarCompatibilityStatus.UnsupportedVersion;
    }
}

public enum ExtendedHotbarCompatibilityStatus
{
    NotDetected,
    Compatible,
    UnsupportedVersion,
    UnknownVersion
}

// A candidate's starting items are native (key, count) tuples built by the
// game's RecruitHelper.ParseItems from sheet strings "<key>_<count>", for
// example "ITEM_WoodSword_1" in DS_B.0.4.23; a bare key parses to nothing.
internal static class FrierenEquipmentRules
{
    internal static string SheetEntry(string itemKey, string nativeSample)
        => nativeSample == null || Regex.IsMatch(nativeSample, @"_\d+$") ? itemKey + "_1" : itemKey;

    internal static bool IsOneOfEach(IReadOnlyList<(string Key, int Count)> items, IReadOnlyList<string> keys)
    {
        if (items == null || keys == null || items.Count != keys.Count) return false;
        for (var i = 0; i < keys.Count; i++)
            if (!string.Equals(items[i].Key, keys[i], StringComparison.Ordinal) || items[i].Count != 1)
                return false;
        return true;
    }
}

// Every item a Booklover uses reaches the native success iterator. Only a
// technique book may be marked for the reward; an unknown item still gets the
// later carried-stack check.
internal static class FrierenBookloverRules
{
    internal const string BookKey = "ITEM_TechBook";

    internal static bool IsPossibleBookRead(string usedItemKey)
        => usedItemKey == null || string.Equals(usedItemKey, BookKey, StringComparison.Ordinal);
}

internal static class FrierenRecruitmentRules
{
    internal static int BookCost(int gold)
        => Math.Max(1, (int)(((long)Math.Max(0, gold) + 149) / 150));
}

// Every Frieren ID that ends up in players' saves. Renaming one needs an entry in
// FrierenLegacyIds, otherwise existing saves lose it.
internal static class FrierenSaveIds
{
    internal const string ProfileKey = "UNITVISUAL_ElfSlimUnisex_10_DelversFrieren";
    internal const string PortraitKey = "Delvers_Frieren_ElfFemale";
    internal const string ArchmageTraitKey = "AFFECTER_DelversElfArchmage";
    internal const string BookloverTraitKey = "AFFECTER_DelversBooklover";
    internal const string BookloverMoodKey = "AFFECTER_DelversBookloverMood";
    internal const string BiographyKey = "TEXTKEY_UNITBIO_DelversFrieren";

    internal static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[]
        { ProfileKey, PortraitKey, ArchmageTraitKey, BookloverTraitKey, BookloverMoodKey, BiographyKey });
}

// Up to Frieren 0.3.x these IDs used a personal name as prefix instead of "Delvers". Saves
// still contain them, so Frieren registers the renames with Core, which rewrites them on
// load. The old prefix is stored Base64-encoded on purpose: it should appear neither in the
// source nor in the DLL or in logs players share.
internal static class FrierenLegacyIds
{
    internal const string CurrentPrefix = "Delvers";
    private static readonly string LegacyPrefix = Encoding.UTF8.GetString(Convert.FromBase64String("RGFubnk="));

    // Name key from early development; FrierenNameMigration still replaces it.
    internal static readonly string NameKey = LegacyPrefix + "_Frieren_Name";

    // Old save ID to current save ID. Each old ID is the current one with the old prefix.
    internal static readonly IReadOnlyDictionary<string, string> SaveRenames = BuildSaveRenames();

    private static IReadOnlyDictionary<string, string> BuildSaveRenames()
    {
        var renames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var id in FrierenSaveIds.All)
            renames.Add(id.Replace(CurrentPrefix, LegacyPrefix, StringComparison.Ordinal), id);
        return renames;
    }
}

internal static class FrierenUniqueCandidateRules
{
    internal const string ProfileKey = FrierenSaveIds.ProfileKey;
    internal const string BackgroundTraitKey = FrierenSaveIds.ArchmageTraitKey;

    internal static bool IsUniqueCandidate(string profileKey, string backgroundTrait)
        => string.Equals(profileKey, ProfileKey, StringComparison.Ordinal)
            && string.Equals(backgroundTrait, BackgroundTraitKey, StringComparison.Ordinal);
}

// Traits Frieren always has, in the order a fresh Frieren stores them. Core
// restores missing ones if a save was written while this pack was inactive.
internal static class FrierenFixedTraitRules
{
    internal const string RaceTraitKey = "AFFECTER_Elf";
    internal const string BookloverTraitKey = FrierenSaveIds.BookloverTraitKey;

    internal static readonly IReadOnlyList<string> OrderedFixedTraits = Array.AsReadOnly(new[]
        { RaceTraitKey, FrierenUniqueCandidateRules.BackgroundTraitKey, BookloverTraitKey });
}
