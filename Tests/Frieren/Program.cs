using FrierenPortrait;
using DungeonSettlersDelvers.Core;

// Frieren pack rules. The Core checks live in Tests/Core and ship with the Core repository.
var checks = 0;
void Check(bool condition, string name)
{
    checks++;
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}

Check(FrierenNameMigrationRules.ShouldMigrate("frieren-profile", "old-name-key",
    "frieren-profile", "old-name-key", "Frieren"), "legacy Frieren key migrates");
Check(!FrierenNameMigrationRules.ShouldMigrate("frieren-profile", "Mira",
    "frieren-profile", "old-name-key", "Frieren"), "custom Frieren name is preserved");
Check(!FrierenNameMigrationRules.ShouldMigrate("frieren-profile", "Frieren",
    "frieren-profile", "old-name-key", "Frieren"), "current Frieren name is unchanged");
Check(!FrierenNameMigrationRules.ShouldMigrate("native-profile", "old-name-key",
    "frieren-profile", "old-name-key", "Frieren"), "unrelated profile name is unchanged");

Check(FrierenBiographyMigrationRules.ShouldMigrate("frieren-profile", null,
    "frieren-profile"), "missing Frieren biography migrates");
Check(FrierenBiographyMigrationRules.ShouldMigrate("frieren-profile", "   ",
    "frieren-profile"), "blank Frieren biography migrates");
Check(!FrierenBiographyMigrationRules.ShouldMigrate("frieren-profile", "custom biography",
    "frieren-profile"), "custom Frieren biography is preserved");
Check(!FrierenBiographyMigrationRules.ShouldMigrate("native-profile", null,
    "frieren-profile"), "unrelated missing biography is unchanged");

Check(FrierenUniqueCandidateRules.IsUniqueCandidate(FrierenUniqueCandidateRules.ProfileKey,
    FrierenUniqueCandidateRules.BackgroundTraitKey), "Frieren is recognized by her stable profile and unique background");
Check(!FrierenUniqueCandidateRules.IsUniqueCandidate("ordinary-profile",
    FrierenUniqueCandidateRules.BackgroundTraitKey), "Frieren's background on another profile is not unique");
Check(!FrierenUniqueCandidateRules.IsUniqueCandidate(FrierenUniqueCandidateRules.ProfileKey,
    "AFFECTER_Mage"), "Frieren's profile without her unique background is not treated as Frieren");

var uniquePredicates = new UniqueCandidatePredicateRegistry<UniqueCandidateIdentity>();
var frierenLease = uniquePredicates.Register("frieren", candidate =>
    FrierenUniqueCandidateRules.IsUniqueCandidate(candidate.ProfileKey, candidate.BackgroundTrait));
var renamedFrieren = new UniqueCandidateIdentity(FrierenUniqueCandidateRules.ProfileKey,
    "Mira", FrierenUniqueCandidateRules.BackgroundTraitKey);
Check(uniquePredicates.IsUnique(renamedFrieren, (_, _) => { }),
    "Frieren pack predicate recognizes her after a custom rename");
frierenLease.Dispose();

Check(CompatibilityModIdentity.IsExtendedHotbar("DungeonSettlers10Slots", "Other Name"),
    "Hotbar detected by assembly name");
Check(CompatibilityModIdentity.IsExtendedHotbar("OtherAssembly", "Extended Hotbar"),
    "Hotbar detected by mod metadata name");
Check(!CompatibilityModIdentity.IsExtendedHotbar("HotbarHelper", "Other Mod"),
    "unrelated mod is not treated as Extended Hotbar");
Check(CompatibilityModIdentity.ClassifyExtendedHotbar("DungeonSettlers10Slots", "Other Name", "1.0.0")
    == ExtendedHotbarCompatibilityStatus.Compatible, "Extended Hotbar 1.0.0 is compatible");
Check(CompatibilityModIdentity.ClassifyExtendedHotbar("OtherAssembly", "Extended Hotbar", "v1.0.0")
    == ExtendedHotbarCompatibilityStatus.Compatible, "Hotbar name and version prefix are recognized");
Check(CompatibilityModIdentity.ClassifyExtendedHotbar("DungeonSettlers10Slots", "Other Name", "1.0.1")
    == ExtendedHotbarCompatibilityStatus.Compatible, "Extended Hotbar patch release 1.0.1 is compatible");
Check(CompatibilityModIdentity.ClassifyExtendedHotbar("DungeonSettlers10Slots", "Other Name", "1.1.0")
    == ExtendedHotbarCompatibilityStatus.UnsupportedVersion, "Extended Hotbar 1.1.0 needs a new compatibility check");
Check(CompatibilityModIdentity.ClassifyExtendedHotbar("DungeonSettlers10Slots", "Other Name", "0.3.5")
    == ExtendedHotbarCompatibilityStatus.UnsupportedVersion, "other Hotbar version is unsupported");
Check(CompatibilityModIdentity.ClassifyExtendedHotbar("DungeonSettlers10Slots", "Other Name", null)
    == ExtendedHotbarCompatibilityStatus.UnknownVersion, "missing Hotbar version is unknown");
Check(CompatibilityModIdentity.ClassifyExtendedHotbar("OtherAssembly", "Other Mod", "1.0.0")
    == ExtendedHotbarCompatibilityStatus.NotDetected, "unrelated mod is not version-classified as Hotbar");

Check(FrierenFixedTraitRules.OrderedFixedTraits.SequenceEqual(new[] { "AFFECTER_Elf",
        FrierenUniqueCandidateRules.BackgroundTraitKey, "AFFECTER_DannyBooklover" }),
    "Frieren's fixed traits for Core restore: race, archmage background, Booklover in saved order");
Check(FrierenFixedTraitRules.OrderedFixedTraits.Distinct().Count() == FrierenFixedTraitRules.OrderedFixedTraits.Count,
    "Frieren's fixed traits are distinct, as Core registration requires");

Check(FrierenRecruitmentRules.BookCost(0) == 1, "book cost at gold 0");
Check(FrierenRecruitmentRules.BookCost(1) == 1, "book cost at gold 1");
Check(FrierenRecruitmentRules.BookCost(146) == 1, "book cost at gold 146");
Check(FrierenRecruitmentRules.BookCost(149) == 1, "book cost at gold 149");
Check(FrierenRecruitmentRules.BookCost(150) == 1, "book cost at gold 150");
Check(FrierenRecruitmentRules.BookCost(151) == 2, "book cost at gold 151");
Check(FrierenRecruitmentRules.BookCost(300) == 2, "book cost at gold 300");

Console.WriteLine($"PASS: {checks} deterministic Frieren checks");
