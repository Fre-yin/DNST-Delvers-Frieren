using System.Text;

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

internal static class FrierenRecruitmentRules
{
    internal static int BookCost(int gold)
        => Math.Max(1, (int)(((long)Math.Max(0, gold) + 149) / 150));
}

internal static class FrierenUniqueCandidateRules
{
    internal const string ProfileKey = "UNITVISUAL_ElfSlimUnisex_10_DannyFrieren";
    internal const string BackgroundTraitKey = "AFFECTER_DannyElfArchmage";

    internal static bool IsUniqueCandidate(string profileKey, string backgroundTrait)
        => string.Equals(profileKey, ProfileKey, StringComparison.Ordinal)
            && string.Equals(backgroundTrait, BackgroundTraitKey, StringComparison.Ordinal);
}

// Traits Frieren always has, in the order a fresh Frieren stores them. Core
// restores missing ones if a save was written while this pack was inactive.
internal static class FrierenFixedTraitRules
{
    internal const string RaceTraitKey = "AFFECTER_Elf";
    internal const string BookloverTraitKey = "AFFECTER_DannyBooklover";

    internal static readonly IReadOnlyList<string> OrderedFixedTraits = Array.AsReadOnly(new[]
        { RaceTraitKey, FrierenUniqueCandidateRules.BackgroundTraitKey, BookloverTraitKey });
}
