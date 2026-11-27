using System.Globalization;
using System.Text.RegularExpressions;
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

// Amfi's mod loader puts Core and Frieren into folders of their own: Frieren finds its files next to its own DLL first, then
// next to Core's, then in the old Mods folder (FrierenAssetFolderRules).
{
    var root = Path.Combine(Path.GetTempPath(), "frieren-asset-folders-" + Guid.NewGuid().ToString("N").Substring(0, 8));
    var coreDir = Path.Combine(root, "BepInEx", "plugins", "delvers-core");
    var frierenDir = Path.Combine(root, "BepInEx", "plugins", "delvers-frieren");
    var legacy = Path.Combine(root, "Mods", "FrierenPortraitAssets");
    Directory.CreateDirectory(Path.Combine(frierenDir, FrierenAssetFolderRules.SubFolder));
    Directory.CreateDirectory(coreDir);
    bool Holds(string folder) => File.Exists(Path.Combine(folder, "Frieren_Normal.png"));
    File.WriteAllText(Path.Combine(frierenDir, FrierenAssetFolderRules.SubFolder, "Frieren_Normal.png"), "x");
    var split = FrierenAssetFolderRules.Pick(frierenDir, coreDir, legacy, Holds);
    Directory.CreateDirectory(Path.Combine(coreDir, FrierenAssetFolderRules.SubFolder));
    File.WriteAllText(Path.Combine(coreDir, FrierenAssetFolderRules.SubFolder, "Frieren_Normal.png"), "x");
    var both = FrierenAssetFolderRules.Pick(frierenDir, coreDir, legacy, Holds);
    var shared = FrierenAssetFolderRules.Pick(coreDir, coreDir, legacy, Holds);
    var coreOnly = FrierenAssetFolderRules.Pick(Path.Combine(root, "elsewhere"), coreDir, legacy, Holds);
    var none = FrierenAssetFolderRules.Pick(Path.Combine(root, "a"), Path.Combine(root, "b"), legacy, Holds);
    var noCore = FrierenAssetFolderRules.Pick(frierenDir, null, legacy, Holds);
    Directory.Delete(root, true);
    Check(split == Path.Combine(frierenDir, "Frieren") && both == Path.Combine(frierenDir, "Frieren") && shared == Path.Combine(coreDir, "Frieren")
            && coreOnly == Path.Combine(coreDir, "Frieren") && none == Path.Combine(root, "b", "Frieren") && noCore == Path.Combine(frierenDir, "Frieren"),
        "asset folders: Frieren's own plugin folder first, then Core's, the old Mods folder last; without files Core's folder for the error");
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
        "AFFECTER_DelversElfArchmage", "AFFECTER_DelversBooklover" }),
    "Frieren's fixed traits for Core restore: race, archmage background, Booklover in saved order");
Check(FrierenFixedTraitRules.OrderedFixedTraits.Distinct().Count() == FrierenFixedTraitRules.OrderedFixedTraits.Count,
    "Frieren's fixed traits are distinct, as Core registration requires");

// The legacy prefix is compared, never written out: it must not appear in this source either.
var saveIds = FrierenSaveIds.All;
Check(saveIds.Count == 6 && saveIds.Distinct().Count() == 6
        && saveIds.All(id => id.Split(FrierenLegacyIds.CurrentPrefix).Length == 2),
    "every save-persistent Frieren ID contains the Delvers prefix exactly once");
string LegacyPart(string oldId, string newId)
{
    var at = newId.IndexOf(FrierenLegacyIds.CurrentPrefix, StringComparison.Ordinal);
    var tail = newId.Substring(at + FrierenLegacyIds.CurrentPrefix.Length);
    return oldId.StartsWith(newId.Substring(0, at), StringComparison.Ordinal) && oldId.EndsWith(tail, StringComparison.Ordinal)
        ? oldId.Substring(at, oldId.Length - at - tail.Length) : null;
}
var legacyParts = FrierenLegacyIds.SaveRenames.Select(pair => LegacyPart(pair.Key, pair.Value)).Distinct().ToArray();
Check(FrierenLegacyIds.SaveRenames.Count == 6 && FrierenLegacyIds.SaveRenames.Values.OrderBy(id => id).SequenceEqual(saveIds.OrderBy(id => id))
        && legacyParts.Length == 1 && !string.IsNullOrEmpty(legacyParts[0]) && legacyParts[0] != FrierenLegacyIds.CurrentPrefix,
    "each old save ID maps to the current ID with only the prefix swapped, the same prefix for all");
Check(!FrierenLegacyIds.SaveRenames.Keys.Any(id => id.Contains(FrierenLegacyIds.CurrentPrefix)),
    "no old save ID already carries the Delvers prefix, so no rename chain exists");
Check(FrierenLegacyIds.NameKey == legacyParts[0] + "_Frieren_Name",
    "the development-era name key uses the same legacy prefix");

Check(FrierenEquipmentRules.SheetEntry("ITEM_ClothArmor", "ITEM_WoodSword_1") == "ITEM_ClothArmor_1",
    "starting item uses the native sheet format with a count suffix");
Check(FrierenEquipmentRules.SheetEntry("ITEM_ClothArmor", null) == "ITEM_ClothArmor_1",
    "starting item falls back to the verified DS_B.0.4.23 sheet format");
Check(FrierenEquipmentRules.SheetEntry("ITEM_ClothArmor", "ITEM_WoodSword") == "ITEM_ClothArmor",
    "starting item follows a native sheet format without a count suffix");
var starterKeys = new[] { "ITEM_ClothArmor", "ITEM_ClothCoif", "ITEM_FireWoodStaff" };
Check(FrierenEquipmentRules.IsOneOfEach(new[] { ("ITEM_ClothArmor", 1), ("ITEM_ClothCoif", 1),
        ("ITEM_FireWoodStaff", 1) }, starterKeys),
    "parsed starting items are accepted when every item appears once in order");
Check(!FrierenEquipmentRules.IsOneOfEach(Array.Empty<(string, int)>(), starterKeys),
    "an empty parse result, as from a bare key, is rejected");
Check(!FrierenEquipmentRules.IsOneOfEach(new[] { ("ITEM_ClothArmor", 1), ("ITEM_ClothCoif", 2),
        ("ITEM_FireWoodStaff", 1) }, starterKeys),
    "a parsed starting item with a wrong count is rejected");
Check(!FrierenEquipmentRules.IsOneOfEach(new[] { ("ITEM_ClothArmor", 1), ("ITEM_WoodSword", 1),
        ("ITEM_FireWoodStaff", 1) }, starterKeys),
    "a parsed starting item with a different key is rejected");
Check(!FrierenEquipmentRules.IsOneOfEach(null, starterKeys),
    "an unreadable parse result is rejected");

Check(FrierenBookloverRules.IsPossibleBookRead("ITEM_TechBook"),
    "a used technique book is marked for the Booklover reward");
Check(!FrierenBookloverRules.IsPossibleBookRead("ITEM_Bread"),
    "a used non-book item is never marked for the Booklover reward");
Check(!FrierenBookloverRules.IsPossibleBookRead("item_techbook"),
    "the technique book key is matched exactly");
Check(FrierenBookloverRules.IsPossibleBookRead(null),
    "an unknown used item falls back to the carried-stack check");

Check(FrierenRecruitmentRules.BookCost(0) == 1, "book cost at gold 0");
Check(FrierenRecruitmentRules.BookCost(1) == 1, "book cost at gold 1");
Check(FrierenRecruitmentRules.BookCost(146) == 1, "book cost at gold 146");
Check(FrierenRecruitmentRules.BookCost(149) == 1, "book cost at gold 149");
Check(FrierenRecruitmentRules.BookCost(150) == 1, "book cost at gold 150");
Check(FrierenRecruitmentRules.BookCost(151) == 2, "book cost at gold 151");
Check(FrierenRecruitmentRules.BookCost(300) == 2, "book cost at gold 300");

var names = FrierenNameTexts.ByLanguage;
Check(names.Count == 10 && names.All(text => !string.IsNullOrWhiteSpace(text) && text.Trim() == text
        && text.IndexOf('–') < 0 && text.IndexOf('—') < 0),
    "Frieren's name exists for all ten game languages");
Check(new[] { 0, 2, 3, 8, 9 }.All(i => names[i] == "Frieren"),
    "Latin-script languages keep the name Frieren");
Check(names[1] == "프리렌" && names[4] == "Фрирен"
        && names[5] == "芙莉莲" && names[6] == "芙莉蓮"
        && names[7] == "フリーレン",
    "Korean, Russian, Chinese and Japanese use the licensed native-script names");

var frierenProfile = FrierenUniqueCandidateRules.ProfileKey;
Check(!FrierenEstablishRules.ExcludeFrieren(false, false, false, false, null),
    "start group without locks keeps Frieren in the pool");
Check(FrierenEstablishRules.ExcludeFrieren(true, false, false, false, null),
    "locked stats exclude Frieren from the start group");
Check(FrierenEstablishRules.ExcludeFrieren(false, true, false, false, null),
    "locked main traits exclude Frieren from the start group");
Check(FrierenEstablishRules.ExcludeFrieren(false, false, true, false, null),
    "locked sub traits exclude Frieren from the start group");
Check(!FrierenEstablishRules.ExcludeFrieren(true, true, true, true, frierenProfile),
    "a slot restricted to Frieren's profile keeps her despite locks");
Check(FrierenEstablishRules.ExcludeFrieren(true, false, false, true, "UNITVISUAL_ElfSlimUnisex_10"),
    "a slot restricted to another profile still excludes Frieren while locked");
Check(FrierenEstablishRules.ExcludeFrieren(true, false, false, false, frierenProfile),
    "Frieren's key without an active profile restriction does not keep her");

// Harmony patches bind game arguments by name, never by position (__0, __1...): DS_B.0.4.29
// inserted a parameter into a game method and an index binding in Core then broke under Melon
// with an IL compile error. Frieren also binds probes by hand (AccessTools), so the
// rule covers every identifier __<n> in the sources, not only methods named Prefix or Postfix.
// The rule table of Tests/Shared/SourceRules.cs, the same as in Core's tests, runs over every source folder of this project: the argument
// rule here under its Frieren name, the known traps, the native lists, the trap-ok markers and the folders found.
Check(SourceRuleReader.IndexBoundArguments("private static void AfterAddAffecter(AffecterComponent __instance, string __0, int __1)").Count == 2
        && SourceRuleReader.IndexBoundArguments("private static bool Prefix(string key, ref Sprite __result)\n"
            + "BookloverItemUse.MarkReadApply(__instance.__2__current, __state);").Count == 0,
    "the argument rule finds index-bound arguments and accepts __instance, __result, __state and iterator fields");
var frierenSourceDirectory = FindFrierenSourceDirectory();
SourceRuleChecks.Run(Check, names: new Dictionary<string, string>
{
    ["index-binding"] = "every Frieren Harmony patch binds game arguments by name"
});

Check(!ZoltraakLoopRules.Stale(false, 10f, 0f) && !ZoltraakLoopRules.Stale(true, 10f, 9f) && ZoltraakLoopRules.Stale(true, 10f, 7.9f)
        && ZoltraakLoopRules.StaleSeconds == 2f,
    "zoltraak loops: a known loop without a heartbeat for 2 s counts as gone, so a new one starts");
// (menue-pause): after a load the speed cache takes the campaign's multiplier, not the pause's 0 from before.
Check(ZoltraakLoopRules.AfterLoad(1f) == 1f && ZoltraakLoopRules.AfterLoad(0f) == 0f && ZoltraakLoopRules.AfterLoad(2f) == 2f
        && ZoltraakLoopRules.AfterLoad(null) == 1f && ZoltraakLoopRules.AfterLoad(float.NaN) == 1f && ZoltraakLoopRules.AfterLoad(-1f) == 0f,
    "zoltraak speed: after a load the campaign's multiplier of its current speed, 1 when none is known");
// The load path calls the speed sync as its own step with its own try, not behind another step's return.
{
    var migrations = frierenSourceDirectory == null ? null : Path.Combine(frierenSourceDirectory, "FrierenSaveMigrations.cs");
    var text = migrations != null && File.Exists(migrations) ? File.ReadAllText(migrations) : "";
    var body = text.IndexOf("internal static void AfterCampaignLoaded(", StringComparison.Ordinal) is var start and >= 0 ? text.Substring(start) : "";
    Check(System.Text.RegularExpressions.Regex.IsMatch(body, @"try \{ DelversHost\.Info\(""FRIEREN_ZOLTRAAK_SPEED "" \+ ZoltraakGameSpeedPatch\.SyncAfterLoad\(\)\); \}")
            && !File.ReadAllText(Path.Combine(frierenSourceDirectory, "FrierenMagicSkills.cs")).Contains("SyncAfterLoad", StringComparison.Ordinal),
        "zoltraak speed: AfterCampaignLoaded calls SyncAfterLoad in a try of its own");
}
// Zoltraak hits along the real angle; the game's own line snaps to 8 directions.
Check(ZoltraakLineRules.Tiles(0, 0, 1, 0, 3) is { } straight
        && straight.SequenceEqual(new[] { (1, 0), (2, 0), (3, 0) }),
    "Zoltraak's line runs its full range towards the target without the caster's tile");
Check(ZoltraakLineRules.Tiles(0, 0, 1, 3, 3) is { } steep
        && steep.SequenceEqual(new[] { (0, 1), (1, 2), (1, 3) }),
    "Zoltraak's line keeps a steep angle instead of snapping straight up");
Check(ZoltraakLineRules.Tiles(0, 0, 2, 1, 2) is { } right && right.SequenceEqual(new[] { (1, 1), (2, 1) })
        && ZoltraakLineRules.Tiles(0, 0, -2, -1, 2) is { } left && left.SequenceEqual(new[] { (-1, -1), (-2, -1) }),
    "Zoltraak's line rounds half steps away from the caster in every direction");
Check(ZoltraakLineRules.Tiles(10, -4, 12, -4, 2) is { } shifted && shifted.SequenceEqual(new[] { (11, -4), (12, -4) }),
    "Zoltraak's line starts next to the caster wherever she stands");
Check(ZoltraakLineRules.Tiles(5, 5, 5, 5, 3) == null && ZoltraakLineRules.Tiles(0, 0, 1, 0, 0) == null,
    "without a direction or range Zoltraak keeps the game's own line");

// The beam stops at the first blocked tile.
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 5, (x, y) => x == 3) is { } walled
        && walled.Open.SequenceEqual(new[] { (1, 0), (2, 0) }) && walled.Blocked == (3, 0),
    "Zoltraak's line hits the tiles before the first blocked tile and names that tile");
Check(ZoltraakLineRules.Trace(0, 0, 1, 1, 4, (x, y) => x == 1 && y == 1) is { } adjacent
        && adjacent.Open.Count == 0 && adjacent.Blocked == (1, 1),
    "a wall right next to Frieren leaves Zoltraak no open tile");
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 3, (x, y) => x >= 4) is { } clear
        && clear.Open.SequenceEqual(new[] { (1, 0), (2, 0), (3, 0) }) && clear.Blocked == null,
    "a wall beyond the range does not shorten Zoltraak's line");
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 6, (x, y) => x == 2 || x == 4) is { } first
        && first.Open.Count == 1 && first.Blocked == (2, 0)
        && ZoltraakLineRules.Trace(2, 2, 2, 2, 3, (x, y) => true) == null,
    "only the first wall counts, and without a direction there is no line");
Check(ZoltraakLineRules.StopReason(false, false, false) == "range"
        && ZoltraakLineRules.StopReason(true, false, false) == "rock"
        && ZoltraakLineRules.StopReason(true, true, true) == "wall"
        && ZoltraakLineRules.StopReason(true, true, false) == "building",
    "Zoltraak names why its line ends: range, rock, wall or a destructible building");
// The beam ends at the first enemy instead of passing through.
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 6, (x, y) => false, (x, y) => x == 3 || x == 5) is { } atEnemy
        && atEnemy.Open.SequenceEqual(new[] { (1, 0), (2, 0), (3, 0) }) && atEnemy.Target == (3, 0) && atEnemy.Blocked == null,
    "Zoltraak's beam ends on the first enemy's tile and reaches no one behind it");
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 6, (x, y) => x == 2, (x, y) => x == 4) is { } wallFirst
        && wallFirst.Blocked == (2, 0) && wallFirst.Target == null && wallFirst.Open.Count == 1
        && ZoltraakLineRules.Trace(0, 0, 1, 0, 3, (x, y) => false, (x, y) => x == 5) is { } far
        && far.Target == null && far.Open.Count == 3,
    "a wall before the enemy stops the beam first, and an enemy beyond the range does not count");
// Her own and allied summons (seed, blooms) let the beam through unharmed;
// built buildings and enemy summons still stop it.
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 6, (x, y) => x == 2, (x, y) => x == 4, (x, y) => x == 2) is { } throughSeed
        && throughSeed.Open.SequenceEqual(new[] { (1, 0), (2, 0), (3, 0), (4, 0) }) && throughSeed.Target == (4, 0)
        && throughSeed.Blocked == null && throughSeed.Passed.SequenceEqual(new[] { (2, 0) }),
    "Zoltraak's beam passes a friendly summon and still ends on the enemy behind it");
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 6, (x, y) => x == 2 || x == 4, null, (x, y) => x == 2) is { } seedThenWall
        && seedThenWall.Open.SequenceEqual(new[] { (1, 0), (2, 0), (3, 0) }) && seedThenWall.Blocked == (4, 0)
        && seedThenWall.Passed.SequenceEqual(new[] { (2, 0) }),
    "past a friendly summon the next wall still stops the beam");
Check(ZoltraakLineRules.Trace(0, 0, 1, 0, 6, (x, y) => x == 2, (x, y) => x == 4, (x, y) => false) is { } enemySummon
        && enemySummon.Blocked == (2, 0) && enemySummon.Target == null && enemySummon.Open.Count == 1
        && enemySummon.Passed.Count == 0
        && ZoltraakLineRules.Trace(0, 0, 1, 0, 6, (x, y) => x == 2) is { } noRule && noRule.Blocked == (2, 0) && noRule.Passed.Count == 0,
    "an enemy summon or a built building still stops the beam, and without the rule nothing passes");

// The circle's rims stand, only the drawing inside turns.
Check(ZoltraakCircleRules.IsRim(0, 0, 32) && ZoltraakCircleRules.IsRim(31, 16, 32) && ZoltraakCircleRules.IsRim(16, 26, 32)
        && !ZoltraakCircleRules.IsRim(15, 15, 32) && !ZoltraakCircleRules.IsRim(16, 25, 32) && !ZoltraakCircleRules.IsRim(6, 16, 32),
    "the circle splits at radius 10.5 into standing rims and a turning drawing");
Check(Enumerable.Range(0, 32 * 32).All(i => ZoltraakCircleRules.IsRim(i % 32, i / 32, 32)
        == ZoltraakCircleRules.IsRim(31 - i / 32, i % 32, 32)),
    "the split of the circle is the same after a quarter turn");
// The pointed start runs on into the band without a seam: its last column is
// the full beam, its first a two-pixel tip in the middle.
Check(Enumerable.Range(0, 12).All(y => ZoltraakTaperRules.Inside(15, y, 16, 12))
        && Enumerable.Range(0, 12).Count(y => ZoltraakTaperRules.Inside(0, y, 16, 12)) == 2
        && ZoltraakTaperRules.Inside(0, 5, 16, 12) && ZoltraakTaperRules.Inside(0, 6, 16, 12)
        && Enumerable.Range(0, 15).All(x => Enumerable.Range(0, 12).Count(y => ZoltraakTaperRules.Inside(x, y, 16, 12))
            <= Enumerable.Range(0, 12).Count(y => ZoltraakTaperRules.Inside(x + 1, y, 16, 12))),
    "Zoltraak's pointed start grows from a two-pixel tip to the full beam without a step at the band");
// The segment's light wave runs once round without a jump and the tiles join
// without a seam. Read from the shipped PNG: frame k is frame 0 shifted by k * 32 / N (cyclic), and
// in every frame the crest is one run of 12 columns, also where column 31 meets column 0.
var segmentFile = frierenSourceDirectory == null ? null
    : Path.Combine(frierenSourceDirectory, "Assets", "Zoltraak_Strahlsegment_32x12_8Bilder.png");
var segmentImage = segmentFile != null && File.Exists(segmentFile) ? ReadPng(segmentFile) : default;
var segmentFrameCount = segmentImage.Width / 32;
uint SegmentPixel(int frame, int x, int y)
    => BitConverter.ToUInt32(segmentImage.Rgba, ((y * segmentImage.Width) + frame * 32 + x) * 4);
Check(segmentImage.Rgba != null && segmentImage.Height == 12 && segmentFrameCount == 8 && 32 % segmentFrameCount == 0
        && Enumerable.Range(1, segmentFrameCount - 1).All(k => Enumerable.Range(0, 12).All(y => Enumerable.Range(0, 32).All(x =>
            SegmentPixel(k, x, y) == SegmentPixel(0, ((x - k * 32 / segmentFrameCount) % 32 + 32) % 32, y))))
        && Enumerable.Range(0, segmentFrameCount).All(k =>
            Enumerable.Range(0, 32).Count(x => SegmentPixel(k, x, 5) == SegmentPixel(k, 0, 6)) == 12
            && Enumerable.Range(0, 32).Count(x => (SegmentPixel(k, x, 5) == SegmentPixel(k, 0, 6))
                != (SegmentPixel(k, (x + 31) % 32, 5) == SegmentPixel(k, 0, 6))) == 2),
    "Zoltraak's beam segment loops its light wave over 8 frames of 4 px and tiles without a seam");
Check(ZoltraakTextRules.Percent(40f, true) == "40\u00A0%" && ZoltraakTextRules.Percent(40f, false) == "40%"
        && ZoltraakTextRules.Percent(12.5f, false) == "12.5%",
    "Zoltraak's hit share reads 40 % with a fixed space or 40% by language, never with a line break");
// Tooltip step 1: Zoltraak names its numbers as {n} from the skill row and the stat as the game's tag.
{
    var values = ZoltraakTextRules.TooltipValues(40f, 15, 0.2f, 1f, 12f, 30);
    var texts = ZoltraakTextRules.Descriptions;
    Check(values.SequenceEqual(new[] { 40f, 15f, 3f, 1f, 12f, 30f }) && texts.Length == 10
            && texts.All(text => ZoltraakTextRules.HighestPlaceholder(text) == values.Length - 1
                && Enumerable.Range(0, values.Length).All(i => text.Contains("{" + i + "}"))
                && new[] { "[STATUS_MagicAttackPower]", "[STATUS_PhysicalResistance]", "[STATUS_MagicResistance]", "[AFFECTER_DelversZoltraakBreak]" }.All(text.Contains)
                && text.IndexOfAny(new[] { '-', '–', '—' }) < 0)
            && ZoltraakTextRules.DescriptionGerman.Contains("{0}% × [STATUS_MagicAttackPower]")
            && ZoltraakTextRules.HighestPlaceholder("no value") == -1 && ZoltraakTextRules.HighestPlaceholder("{{7}} {3}") == 3,
        "Zoltraak tooltip: 40 % per hit, 15 hits, 3 s, break 1 point, 12 s, 30 stacks as {0} to {5}, the break with its tags; every {n} has a value, no hyphens, in all ten languages");
}

// Zoltraak's sounds: 16 and 24 bit PCM, anything else is refused instead of played as noise.
static byte[] Wav(short format, short channels, int rate, short bits, byte[] data)
{
    var stream = new MemoryStream();
    var writer = new BinaryWriter(stream);
    writer.Write(Ascii("RIFF")); writer.Write(36 + data.Length); writer.Write(Ascii("WAVE"));
    writer.Write(Ascii("fmt ")); writer.Write(16); writer.Write(format); writer.Write(channels); writer.Write(rate);
    writer.Write(rate * channels * bits / 8); writer.Write((short)(channels * bits / 8)); writer.Write(bits);
    writer.Write(Ascii("data")); writer.Write(data.Length); writer.Write(data);
    return stream.ToArray();
}
static byte[] Ascii(string text) => System.Text.Encoding.ASCII.GetBytes(text);
var wav16 = ZoltraakWavRules.Read(Wav(1, 2, 48000, 16, new byte[] { 0x00, 0x40, 0x00, 0xC0, 0xFF, 0x7F, 0x00, 0x80 }));
Check(wav16.Channels == 2 && wav16.Rate == 48000 && wav16.Frames == 2
        && wav16.Samples.SequenceEqual(new[] { 0.5f, -0.5f, 32767f / 32768f, -1f }),
    "a 16 bit stereo WAV reads as interleaved samples from -1 to 1");
var wav24 = ZoltraakWavRules.Read(Wav(1, 1, 44100, 24, new byte[] { 0x00, 0x00, 0x40, 0x00, 0x00, 0xC0, 0x01, 0x00, 0x00 }));
Check(wav24.Channels == 1 && wav24.Rate == 44100 && wav24.Frames == 3
        && wav24.Samples[0] == 0.5f && wav24.Samples[1] == -0.5f && wav24.Samples[2] == 1f / 8388608f,
    "a 24 bit mono WAV such as the Freesound originals reads correctly");
Check(Refused(() => ZoltraakWavRules.Read(Wav(3, 1, 48000, 32, new byte[8])))
        && Refused(() => ZoltraakWavRules.Read(Wav(1, 1, 48000, 16, Array.Empty<byte>())))
        && Refused(() => ZoltraakWavRules.Read(System.Text.Encoding.ASCII.GetBytes("ID3 not a wave file"))),
    "float, empty and non WAV files are refused, so Zoltraak stays silent instead of hissing");
// Lauf 13 and 14, AudioClip.Create (its CreateUserSound turns the name into a span) and
// AudioClip.SetData(float[]) run Unity's managed bodies over ReadOnlySpan.GetPinnableReference, which
// the game stripped ("Method not found"). Zoltraak's sounds build their clips through the native
// CreateUserSound_Injected and SetData_Injected; no Frieren source may call the managed ones.
var managedAudio = new List<string>();
var soundSource = frierenSourceDirectory == null ? null : Path.Combine(frierenSourceDirectory, "Zoltraak", "ZoltraakSound.cs");
if (frierenSourceDirectory != null)
    foreach (var file in Directory.GetFiles(frierenSourceDirectory, "*.cs", SearchOption.AllDirectories))
        if (System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(file), @"\.SetData\s*\(|AudioClip\.Create\s*\("))
            managedAudio.Add(Path.GetRelativePath(frierenSourceDirectory, file));
Check(soundSource != null && File.Exists(soundSource) && File.ReadAllText(soundSource) is { } sound
        && sound.Contains("AudioClip.CreateUserSound_Injected(") && sound.Contains("AudioClip.SetData_Injected(")
        && managedAudio.Count == 0,
    "Zoltraak's sounds build their clips through the native CreateUserSound_Injected and SetData_Injected, never the managed Create or SetData ("
        + string.Join(", ", managedAudio) + ")");
// A release plays the candidates W1, A1 and D1 under their release names. The files
// named in ZoltraakSound lie in Assets\Sounds, nothing else does, and each has its licence line.
var releaseSounds = new List<string>();
if (soundSource != null && File.Exists(soundSource))
    foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
        File.ReadAllText(soundSource), @"Release(?:Cast|Shot|Loop)File = ""([^""]+)"""))
        releaseSounds.Add(match.Groups[1].Value);
var soundFolder = frierenSourceDirectory == null ? null : Path.Combine(frierenSourceDirectory, "Assets", "Sounds");
var licenceFile = frierenSourceDirectory == null ? null : Path.Combine(frierenSourceDirectory, "Assets", "LIZENZEN.md");
var licences = licenceFile != null && File.Exists(licenceFile) ? File.ReadAllText(licenceFile) : string.Empty;
Check(releaseSounds.Count == 3 && soundFolder != null && Directory.Exists(soundFolder)
        && Directory.GetFiles(soundFolder).Select(file => Path.GetFileName(file)).OrderBy(name => name, StringComparer.Ordinal)
            .SequenceEqual(releaseSounds.OrderBy(name => name, StringComparer.Ordinal))
        && releaseSounds.All(name => ZoltraakWavRules.Read(File.ReadAllBytes(Path.Combine(soundFolder, name))).Frames > 0)
        && releaseSounds.All(name => licences.Contains("`" + name + "`")),
    "a release ships exactly the three Zoltraak sounds named in ZoltraakSound, readable and listed in LIZENZEN.md");
// Every Zoltraak image the code loads is in Assets and no other one is (the light
// point replaced the end cap image, which would otherwise ship unused).
var namedImages = new SortedSet<string>(StringComparer.Ordinal);
if (frierenSourceDirectory != null)
    foreach (var file in Directory.GetFiles(Path.Combine(frierenSourceDirectory, "Zoltraak"), "*.cs"))
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
            File.ReadAllText(file), @"""(Zoltraak_[^""]+\.png)"""))
            namedImages.Add(match.Groups[1].Value);
var shippedImages = frierenSourceDirectory == null ? new SortedSet<string>(StringComparer.Ordinal)
    : new SortedSet<string>(Directory.GetFiles(Path.Combine(frierenSourceDirectory, "Assets"), "Zoltraak_*.png")
        .Select(file => Path.GetFileName(file)), StringComparer.Ordinal);
Check(namedImages.Count == 8 && namedImages.SetEquals(shippedImages),
    "the eight Zoltraak images in the code and in Assets are the same (" + string.Join(", ", shippedImages.Except(namedImages)) + ")");
// The light point at the beam's tip sits on its sprite centre; mirrored in both axes it lies on the
// beam's end at any angle.
var tipFile = frierenSourceDirectory == null ? null
    : Path.Combine(frierenSourceDirectory, "Assets", "Zoltraak_Lichtpunkt_17x17_4Bilder.png");
var tipImage = tipFile != null && File.Exists(tipFile) ? ReadPng(tipFile) : default;
uint TipPixel(int frame, int x, int y) => BitConverter.ToUInt32(tipImage.Rgba, ((y * tipImage.Width) + frame * 17 + x) * 4);
Check(tipImage.Rgba != null && tipImage.Width == 4 * 17 && tipImage.Height == 17
        && Enumerable.Range(0, 4).All(k => (TipPixel(k, 8, 8) >> 24) != 0
            && Enumerable.Range(0, 17).All(y => Enumerable.Range(0, 17).All(x =>
                TipPixel(k, x, y) == TipPixel(k, 16 - x, y) && TipPixel(k, x, y) == TipPixel(k, x, 16 - y)))),
    "Zoltraak's light point has four frames of 17 px, each lit and mirrored around its middle pixel");
// Zoltraak Break's symbol comes through FrierenIconRegistry like Frieren's other
// affecter icons; no second patch on the game's affecter icons may answer for it.
var affecterIconPatches = new List<string>();
if (frierenSourceDirectory != null)
    foreach (var file in Directory.GetFiles(frierenSourceDirectory, "*.cs", SearchOption.AllDirectories))
        if (File.ReadAllText(file).Contains("LoadAffecterIcon"))
            affecterIconPatches.Add(Path.GetRelativePath(frierenSourceDirectory, file));
var iconRegistry = frierenSourceDirectory == null ? null : Path.Combine(frierenSourceDirectory, "FrierenIconRegistry.cs");
Check(affecterIconPatches.SequenceEqual(new[] { "FrierenIconRegistry.cs" })
        && File.ReadAllText(iconRegistry).Contains("Zoltraak.BreakKey"),
    "Zoltraak Break's symbol goes through FrierenIconRegistry only (" + string.Join(", ", affecterIconPatches) + ")");

// The recruitment's patch audit names every Harmony owner that is not a Delvers pack, each once and sorted.
Check(FrierenRecruitPatchAuditRules.Foreign(new[] { "fre-yin.dungeonsettlers.delvers.core", "other.mod", "fre-yin.dungeonsettlers.delvers.frieren",
            "another.mod", "other.mod", "fre-yin.dungeonsettlers.delvers.core.probes.X" }).SequenceEqual(new[] { "another.mod", "other.mod" })
        && FrierenRecruitPatchAuditRules.Line("RecruitHelper.RefreshExist", true, new string[0]) == "FRIEREN_RECRUIT_PATCH_AUDIT target=RecruitHelper.RefreshExist found=1 foreign=-"
        && FrierenRecruitPatchAuditRules.Line("X", false, new[] { "a", "b" }) == "FRIEREN_RECRUIT_PATCH_AUDIT target=X found=0 foreign=a,b",
    "recruit patch audit: other mods on the recruitment's methods are named, the Delvers packs are not");

// The numbers that texts, log lines and developer comments repeat equal the constants they describe. The
// values are read from the sources, so a changed constant whose text was left behind turns this red.
{
    string Source(string relative) => frierenSourceDirectory == null ? "" : ReadSource(Path.Combine(frierenSourceDirectory, relative));
    var zoltraakSource = Source("Zoltraak/Zoltraak.cs");
    var registrationSource = Source("Zoltraak/ZoltraakRegistration.cs");
    var localizationSource = Source("FrierenLocalization.cs");
    var bookloverSource = Source("Booklover.cs");
    var itemUseSource = Source("BookloverItemUse.cs");
    var runtimeSource = Source("FrierenRuntime.cs");
    var gameplaySource = Source("FrierenGameplay.cs");
    var recruitmentSource = Source("FrierenRecruitment.cs");
    var problems = new List<string>();

    var ticks = ConstantValue(zoltraakSource, "Ticks", problems);
    var tickInterval = ConstantValue(zoltraakSource, "TickInterval", problems);
    var headShare = ConstantValue(zoltraakSource, "HeadShare", problems);
    var bodyShare = ConstantValue(zoltraakSource, "BodyShare", problems);
    var breakStacks = ConstantValue(zoltraakSource, "BreakMaxStack", problems);
    var breakSeconds = ConstantValue(zoltraakSource, "BreakDuration", problems);
    var perStack = ConstantValue(zoltraakSource, "BreakPerStack", problems);
    var moodSeconds = ConstantValue(bookloverSource, "MoodDurationSeconds", problems);
    var maxEnergy = ConstantValue(gameplaySource, "MaxEnergyBonus", problems);
    var cooldownReduction = ConstantValue(gameplaySource, "CooldownReductionBonus", problems);
    var physicalDamage = LiteralValue(gameplaySource, @"PhysicalDamageDealtBonus, (-?\d+(?:\.\d+)?)f\)", "the physical damage of the trait", problems);
    var mood = LiteralValue(bookloverSource, @"Add\(StatType\.Mood, (\d+(?:\.\d+)?)f\)", "the mood of the reading joy", problems);
    var moodData = LiteralValue(bookloverSource, @"""Mood_(\d+(?:\.\d+)?)""", "the mood in the affecter's stat list", problems);
    var mainPoints = LiteralValue(itemUseSource, @"AddProgressionValue\(StatType\.MainSkillPoint, (\d+)\)", "the main skill points of a read book", problems);
    var subPoints = LiteralValue(itemUseSource, @"AddProgressionValue\(StatType\.SubSkillPoint, (\d+)\)", "the secondary skill points of a read book", problems);
    var moodMinutes = moodSeconds / 60;
    if (mood != moodData) problems.Add($"Mood is {mood} in the stat dictionary but {moodData} in the stat list");

    // Zoltraak: developer comments, the break row's comment and log, the cooldown reasoning.
    AddDifference(problems, "the comment on the channel in Zoltraak.cs",
        TextNumbers(CommentBlock(zoltraakSource, "hits over")), new[] { ticks, ticks * tickInterval, perStack, breakSeconds, breakStacks, breakStacks / ticks });
    AddDifference(problems, "the comment on the head share in Zoltraak.cs",
        TextNumbers(CommentBlock(zoltraakSource, "goes surely to the head")), new[] { headShare * 100 });
    if (Math.Abs(headShare + bodyShare - 1) > 1e-6) problems.Add($"HeadShare {headShare} and BodyShare {bodyShare} do not add up to the whole hit");
    AddDifference(problems, "the break row's developer comment",
        TextNumbers(Regex.Match(registrationSource, @"DevComment2 = ""(PhysicalResistance[^""]*)""").Groups[1].Value), new[] { perStack, perStack });
    AddDifference(problems, "the break registration log",
        TextNumbers(Statement(registrationSource, "FRIEREN_ZOLTRAAK_BREAK_REGISTERED")?.Split("perStack=").Last()), new[] { perStack, perStack });
    AddDifference(problems, "the cooldown comment in ZoltraakRegistration.cs",
        TextNumbers(CommentBlock(registrationSource, "second channel to reach")), new[] { breakSeconds, breakStacks });
    var cooldown = LiteralValue(registrationSource, @"Cooldown = (\d+(?:\.\d+)?)f,", "the cooldown", problems);
    if (!(cooldown < breakSeconds)) problems.Add($"Cooldown {cooldown} s is not below the break's {breakSeconds} s, so a second channel cannot reach {breakStacks} stacks");
    var floatValuesComment = Regex.Match(registrationSource, @"DevComment_SkillFloatValues = ""([^""]*)""").Groups[1].Value;
    var tooltipValues = ZoltraakTextRules.TooltipValues((float)ConstantValue(zoltraakSource, "DefaultHitPercent", problems), (int)ticks,
        (float)tickInterval, (float)perStack, (float)breakSeconds, (int)breakStacks);
    if (ZoltraakTextRules.HighestPlaceholder(floatValuesComment) != tooltipValues.Length - 1)
        problems.Add($"the skill row's comment names {{0}} to {{{ZoltraakTextRules.HighestPlaceholder(floatValuesComment)}}} but the row holds {tooltipValues.Length} values");

    // The registration logs of the traits and the Booklover.
    AddLogDifferences(problems, "FRIEREN_TRAIT_REGISTERED", Statement(runtimeSource, "FRIEREN_TRAIT_REGISTERED"),
        ("physicalDamage", physicalDamage), ("maxEnergy", maxEnergy), ("cooldownReduction", cooldownReduction));
    AddLogDifferences(problems, "BOOKLOVER_REGISTERED", Statement(runtimeSource, "BOOKLOVER_REGISTERED"),
        ("mood", mood), ("duration", moodSeconds));
    var readLog = Statement(itemUseSource, "BOOKLOVER_READ_SUCCESS");
    AddLogDifferences(problems, "BOOKLOVER_READ_SUCCESS", readLog, ("mood", mood), ("duration", moodSeconds));
    var pointsLog = Regex.Match(readLog ?? "", @"points=(\d+)\+(\d+)");
    if (!pointsLog.Success || double.Parse(pointsLog.Groups[1].Value, CultureInfo.InvariantCulture) != mainPoints
        || double.Parse(pointsLog.Groups[2].Value, CultureInfo.InvariantCulture) != subPoints)
        problems.Add($"BOOKLOVER_READ_SUCCESS says {pointsLog.Value} but a read book grants {mainPoints}+{subPoints} points");
    if (!Regex.IsMatch(readLog ?? "", @"\bbook=" + FrierenBookloverRules.BookKey + @"\b"))
        problems.Add("BOOKLOVER_READ_SUCCESS does not name " + FrierenBookloverRules.BookKey);

    // The Booklover's developer comments: the trait row twice, the mood row once.
    var bookloverComments = Regex.Matches(bookloverSource, @"DevComment2 = ""([^""]*)""").Select(match => match.Groups[1].Value).ToList();
    if (bookloverComments.Count != 3) problems.Add($"Booklover.cs holds {bookloverComments.Count} DevComment2 lines, not the three the check knows");
    else
    {
        AddDifference(problems, "the trait row's comment (traits)", TextNumbers(bookloverComments[0]), new[] { mainPoints, subPoints, mood, moodMinutes });
        AddDifference(problems, "the trait row's comment (affecters)", TextNumbers(bookloverComments[1]), new[] { mainPoints, subPoints, mood, moodMinutes });
        AddDifference(problems, "the mood row's comment", TextNumbers(bookloverComments[2]), new[] { mood, moodMinutes });
    }

    // The same texts in all ten languages (FrierenLocalization): the break, the Booklover and its joy.
    AddLocalizedDifferences(problems, "the break's description", RowTexts(localizationSource, "Zoltraak.BreakDescriptionTextKey"),
        new[] { perStack, breakStacks }, false);
    AddLocalizedDifferences(problems, "the Booklover's description", RowTexts(localizationSource, "BookloverDescKey"),
        new[] { mainPoints, subPoints, mood, moodMinutes }, true);
    AddLocalizedDifferences(problems, "the reading joy's description", RowTexts(localizationSource, "BookloverMoodDescKey"),
        new[] { moodMinutes }, false);

    // The book is named by one key: every spelling of it in the pack is the key of FrierenBookloverRules.
    var allSources = frierenSourceDirectory == null ? ""
        : string.Join("\n", Directory.GetFiles(frierenSourceDirectory, "*.cs", SearchOption.AllDirectories).Select(file => File.ReadAllText(file)));
    var bookSpellings = Regex.Matches(allSources, @"ITEM_TechBook", RegexOptions.IgnoreCase).Select(match => match.Value).Distinct().ToList();
    if (!bookSpellings.SequenceEqual(new[] { FrierenBookloverRules.BookKey })) problems.Add("the technique book is spelled " + string.Join(", ", bookSpellings));
    var recruitmentBook = Regex.Matches(recruitmentSource, @"(?:BookKey = |name == )""([^""]+)""").Select(match => match.Groups[1].Value).ToList();
    if (recruitmentBook.Count != 2 || recruitmentBook.Any(key => key != FrierenBookloverRules.BookKey))
        problems.Add("FrierenRecruitment names the book " + string.Join(", ", recruitmentBook) + " instead of " + FrierenBookloverRules.BookKey);

    Check(frierenSourceDirectory != null && problems.Count == 0,
        "numbers in Frieren's texts, log lines and developer comments equal their constants (" + string.Join("; ", problems) + ")");

    // Game texts carry no hyphen or dash (the rule for new texts): every row of FrierenLocalization in all ten languages; the rows
    // of the test mode exist only in test builds and are left out.
    var releaseRows = Regex.Replace(localizationSource, @"#if [A-Z]+_QA\b.*?#endif", "", RegexOptions.Singleline);
    var rowKeys = Regex.Matches(releaseRows, @"\bRow\(([\w.]+),").Select(match => match.Groups[1].Value).Distinct().ToList();
    var hyphened = rowKeys.SelectMany(key => RowTexts(releaseRows, key).Where(text => text.IndexOfAny(new[] { '-', '\u2013', '\u2014' }) >= 0)
        .Select(text => key + ": " + text)).ToList();
    Check(frierenSourceDirectory != null && rowKeys.Count > 10 && hyphened.Count == 0,
        "Frieren's game texts carry no hyphen or dash in any language (" + string.Join("; ", hyphened) + ")");
}

// The channel's fallback: when the lazy routine cannot be created (its registration or creation failed), the ticks are
// built at cast time instead of the cast failing. The decision is two statements in ChannelSkills.TryBuildRoutine, not a
// rule of its own, so the source is what can be pinned offline; the run in the game is the self-test's.
{
    var channelSource = frierenSourceDirectory == null ? "" : ReadSource(Path.Combine(frierenSourceDirectory, "Zoltraak", "ChannelSkills.cs"));
    Check(Regex.IsMatch(channelSource, @"if \(DelversChannelRoutine\.TryCreate\(Sequence\(definition, skill, inMapData, context, ""lazy""\), out var lazy\)\)\s*"
            + @"\{\s*routine = lazy;\s*return true;\s*\}\s*var items = new ExecutionDataList\(\);\s*"
            + @"foreach \(var data in Sequence\(definition, skill, inMapData, context, ""fixed""\)\) items\.Add\(data\);\s*"
            + @"routine = items\.Cast<ExecutionDataSequence>\(\)\.GetEnumerator\(\);\s*return true;"),
        "ChannelSkills builds the ticks at cast time when the lazy channel routine cannot be created");
}

Console.WriteLine($"PASS: {checks} deterministic Frieren checks");

// A plain PNG reader for the offline checks: 8-bit RGBA, not interlaced, filters 0 to 4.
static (int Width, int Height, byte[] Rgba) ReadPng(string path)
{
    var data = File.ReadAllBytes(path);
    static int BigEndian(byte[] b, int i) => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];
    int position = 8, width = 0, height = 0;
    var compressed = new MemoryStream();
    while (position + 8 <= data.Length)
    {
        var length = BigEndian(data, position);
        var type = System.Text.Encoding.ASCII.GetString(data, position + 4, 4);
        if (type == "IHDR")
        {
            width = BigEndian(data, position + 8);
            height = BigEndian(data, position + 12);
            if (data[position + 16] != 8 || data[position + 17] != 6 || data[position + 20] != 0)
                throw new InvalidDataException("nur RGBA mit 8 Bit, nicht interlaced");
        }
        else if (type == "IDAT") compressed.Write(data, position + 8, length);
        else if (type == "IEND") break;
        position += 12 + length;
    }
    compressed.Position = 0;
    var raw = new MemoryStream();
    using (var inflate = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionMode.Decompress))
        inflate.CopyTo(raw);
    var bytes = raw.ToArray();
    var stride = width * 4;
    var pixels = new byte[stride * height];
    var previous = new byte[stride];
    for (var y = 0; y < height; y++)
    {
        var filter = bytes[y * (stride + 1)];
        var line = new byte[stride];
        Array.Copy(bytes, y * (stride + 1) + 1, line, 0, stride);
        for (var i = 0; i < stride; i++)
        {
            int left = i >= 4 ? line[i - 4] : 0, up = previous[i], upLeft = i >= 4 ? previous[i - 4] : 0;
            var predictor = filter switch
            {
                0 => 0,
                1 => left,
                2 => up,
                3 => (left + up) / 2,
                4 => Paeth(left, up, upLeft),
                _ => throw new InvalidDataException("unbekannter PNG-Filter " + filter)
            };
            line[i] = (byte)(line[i] + predictor);
        }
        Array.Copy(line, 0, pixels, y * stride, stride);
        previous = line;
    }
    return (width, height, pixels);
}

static int Paeth(int a, int b, int c)
{
    int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
    return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
}

static bool Refused(Action action)
{
    try { action(); return false; }
    catch (InvalidDataException) { return true; }
}

static string ReadSource(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

// The value of `const <type> Name = 15;`, `= 0.2f;` or `= 20 * 60;`; NaN and a problem when it is not there.
static double ConstantValue(string source, string name, List<string> problems)
{
    var match = Regex.Match(source, @"\bconst\s+\w+\s+" + name + @"\s*=\s*(\d+(?:\.\d+)?f?(?:\s*\*\s*\d+(?:\.\d+)?f?)*)\s*;");
    if (!match.Success)
    {
        problems.Add("constant " + name + " not found");
        return double.NaN;
    }
    return match.Groups[1].Value.Split('*').Aggregate(1d, (product, factor) => product * double.Parse(factor.Trim().TrimEnd('f'), CultureInfo.InvariantCulture));
}

// The number in group 1 of a pattern that must occur in the source (a literal that has no constant).
static double LiteralValue(string source, string pattern, string what, List<string> problems)
{
    var match = Regex.Match(source, pattern);
    if (!match.Success)
    {
        problems.Add(what + " not found");
        return double.NaN;
    }
    return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
}

// The numbers a text names, in order: digits (dates such as do not count) and the words two to ten.
// "one" is left out on purpose, it is as often an article as a number.
static List<double> TextNumbers(string text)
{
    var words = new[] { "", "", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
    var numbers = new List<double>();
    foreach (Match match in Regex.Matches(Regex.Replace(text ?? "", @"\d{2}\.\d{2}\.\d{4}", " "),
        @"\d+(?:\.\d+)?|\b(?:two|three|four|five|six|seven|eight|nine|ten)\b", RegexOptions.IgnoreCase))
        numbers.Add(char.IsDigit(match.Value[0]) ? double.Parse(match.Value, CultureInfo.InvariantCulture) : Array.IndexOf(words, match.Value.ToLowerInvariant()));
    return numbers;
}

static void AddDifference(List<string> problems, string what, IReadOnlyList<double> found, IReadOnlyList<double> expected)
{
    if (found.Count != expected.Count || found.Where((number, i) => Math.Abs(number - expected[i]) > 1e-6).Any())
        problems.Add($"{what} names [{string.Join(", ", found)}] but its constants are [{string.Join(", ", expected)}]");
}

// The comment lines around the first line that holds the marker; null when the marker is not there.
static string CommentBlock(string source, string marker)
{
    var lines = source.Replace("\r\n", "\n").Split('\n');
    var at = Array.FindIndex(lines, line => line.Contains(marker, StringComparison.Ordinal));
    if (at < 0) return null;
    int first = at, last = at;
    while (first > 0 && lines[first - 1].TrimStart().StartsWith("//", StringComparison.Ordinal)) first--;
    while (last + 1 < lines.Length && lines[last + 1].TrimStart().StartsWith("//", StringComparison.Ordinal)) last++;
    return string.Join("\n", lines[first..(last + 1)]);
}

// A call from its marker to the closing `);`, for the log lines that continue over several lines; null when absent.
static string Statement(string source, string marker)
{
    var start = source.IndexOf(marker, StringComparison.Ordinal);
    var end = start < 0 ? -1 : source.IndexOf(");", start, StringComparison.Ordinal);
    return end < 0 ? null : source.Substring(start, end - start);
}

// `name=value` pairs of a log line ("maxEnergy=+50", "mood=4", "physicalDamage=-40%").
static void AddLogDifferences(List<string> problems, string log, string statement, params (string Name, double Value)[] expected)
{
    if (statement == null)
    {
        problems.Add(log + " not found");
        return;
    }
    foreach (var (name, value) in expected)
    {
        var pair = Regex.Match(statement, @"\b" + name + @"=([+-]?\d+(?:\.\d+)?)");
        if (!pair.Success || Math.Abs(double.Parse(pair.Groups[1].Value, CultureInfo.InvariantCulture) - value) > 1e-6)
            problems.Add($"{log} says {pair.Value} but the constant {name} is {value}");
    }
}

// The texts of a FrierenLocalization row, in the order of the file: the string literals of the call `Row(key, ...)`
// up to its closing bracket (the export removes the rows after it, so the next row cannot be the end).
static List<string> RowTexts(string source, string key)
{
    var texts = new List<string>();
    var start = source.IndexOf("Row(" + key + ",", StringComparison.Ordinal);
    if (start < 0) return texts;
    var depth = 0;
    for (var i = start + 3; i < source.Length; i++)
    {
        if (source[i] == '"')
        {
            var end = i + 1;
            while (source[end] != '"') end += source[end] == '\\' ? 2 : 1;
            texts.Add(Regex.Unescape(source.Substring(i + 1, end - i - 1)));
            i = end;
        }
        else if (source[i] == '(') depth++;
        else if (source[i] == ')' && --depth == 0) break;
    }
    return texts;
}

// Each of the ten texts names the expected numbers. A language may say "2 each" once instead of twice and may add a
// "1" for "one book" (allowBookCount), so the distinct values are compared.
static void AddLocalizedDifferences(List<string> problems, string what, IReadOnlyList<string> texts, IReadOnlyList<double> expected, bool allowBookCount)
{
    if (texts.Count != 10)
    {
        problems.Add($"{what} has {texts.Count} texts, not ten");
        return;
    }
    var wanted = expected.Distinct().OrderBy(number => number).ToList();
    for (var language = 0; language < texts.Count; language++)
    {
        var found = Regex.Matches(texts[language], @"\d+").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))
            .Where(number => !(allowBookCount && number == 1 && !wanted.Contains(1))).Distinct().OrderBy(number => number).ToList();
        if (!found.SequenceEqual(wanted))
            problems.Add($"{what}, language {language}, names [{string.Join(", ", found)}] but its constants are [{string.Join(", ", wanted)}]");
    }
}

static string FindFrierenSourceDirectory()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
    {
        var candidate = Path.Combine(directory.FullName, "Frieren");
        if (File.Exists(Path.Combine(candidate, "DungeonSettlersDelvers.Frieren.csproj"))) return candidate;
    }
    return null;
}
