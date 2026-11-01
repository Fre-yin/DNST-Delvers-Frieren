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

// Frieren's name in the game's language order (en, ko, fr, de, ru, zh-Hans, zh-Hant,
// ja, es, pt-BR), in each language's own script as used by the licensed releases:
// Korean Haksan edition, Chinese Bilibili and Taiwan releases, the Japanese original,
// and the established Russian press form.
internal static class FrierenNameTexts
{
    internal const string English = "Frieren";
    internal static readonly IReadOnlyList<string> ByLanguage = Array.AsReadOnly(new[]
    {
        English, "프리렌", English, English, "Фрирен", "芙莉莲", "芙莉蓮", "フリーレン", English, English
    });
}

// New-campaign start group: RecruitHelper.CreateEstablishCandidate applies the
// player's locked traits and stats before Frieren's postfix runs, and its profile pick heeds only race, gender and
// profile restriction and free portraits, not the
// other locks (same line). While anything is locked,
// Frieren is excluded unless the slot is restricted to her own profile.
internal static class FrierenEstablishRules
{
    internal static bool ExcludeFrieren(bool majorStatsLocked, bool mainTraitsLocked, bool subTraitsLocked,
        bool useProfileRestriction, string restrictedProfileKey)
        => (majorStatsLocked || mainTraitsLocked || subTraitsLocked)
            && !(useProfileRestriction
                && string.Equals(restrictedProfileKey, FrierenUniqueCandidateRules.ProfileKey, StringComparison.Ordinal));
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

// Tiles hit by Zoltraak: on the ray from the caster towards the target, one per step along the
// major axis, radius steps long and without the caster's own tile, like the game's own line but
// at the real angle instead of 8 directions (GridHelper.FindPositionWithDirection). Null when there is no direction or
// no range; the skill then keeps the native line.
internal static class ZoltraakLineRules
{
    internal static IReadOnlyList<(int X, int Y)> Tiles(int fromX, int fromY, int toX, int toY, int radius)
    {
        var dx = toX - fromX;
        var dy = toY - fromY;
        var major = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (major == 0 || radius <= 0) return null;
        var tiles = new (int X, int Y)[radius];
        for (var step = 1; step <= radius; step++)
            tiles[step - 1] = (
                fromX + (int)Math.Round((double)dx * step / major, MidpointRounding.AwayFromZero),
                fromY + (int)Math.Round((double)dy * step / major, MidpointRounding.AwayFromZero));
        return tiles;
    }

    // The beam stops at the first blocked tile, like the native line of
    // SKILL_ClingingVines (CombatUtility.GetTilesInDirection breaks on inMapData.IsBlocked).
    // It also ends at the first enemy instead of passing through. Open are the tiles it reaches, the enemy's tile
    // included; Blocked is the blocked tile that stopped it, Target the enemy's tile, both null
    // when the line runs its full range. Null without a direction or range.
    // A blocked tile that passes (the caster's own or an allied summon) lets
    // the beam through; it counts as open and is listed in Passed.
    internal static ZoltraakTrace Trace(int fromX, int fromY, int toX, int toY, int radius, Func<int, int, bool> isBlocked,
        Func<int, int, bool> holdsTarget = null, Func<int, int, bool> passes = null)
    {
        var tiles = Tiles(fromX, fromY, toX, toY, radius);
        if (tiles == null) return null;
        var open = new List<(int X, int Y)>(tiles.Count);
        var passed = new List<(int X, int Y)>();
        foreach (var tile in tiles)
        {
            if (isBlocked(tile.X, tile.Y))
            {
                if (passes == null || !passes(tile.X, tile.Y)) return new ZoltraakTrace(open, tile, null, passed);
                passed.Add(tile);
            }
            open.Add(tile);
            if (holdsTarget != null && holdsTarget(tile.X, tile.Y)) return new ZoltraakTrace(open, null, tile, passed);
        }
        return new ZoltraakTrace(open, null, null, passed);
    }

    // Why the beam ends, also the stop key of the measurement log: target (the first enemy),
    // range (nothing in the way), rock (blocked without a building: terrain, map edge, unexplored
    // ground), wall (a building that cannot be destroyed) or building (it takes the hit and holds
    // the beam until it is gone, whoever owns it, like the game's Explosion).
    internal static string StopReason(bool blocked, bool hasBuilding, bool indestructible)
        => !blocked ? "range" : !hasBuilding ? "rock" : indestructible ? "wall" : "building";

    internal const string TargetStop = "target";

    // The log's mark for a line that passed through a summon of the caster's side (pass=summon).
    internal const string SummonPass = "summon";
}

internal sealed class ZoltraakTrace
{
    internal ZoltraakTrace(IReadOnlyList<(int X, int Y)> open, (int X, int Y)? blocked, (int X, int Y)? target,
        IReadOnlyList<(int X, int Y)> passed = null)
    {
        Open = open;
        Blocked = blocked;
        Target = target;
        Passed = passed ?? Array.Empty<(int X, int Y)>();
    }

    internal IReadOnlyList<(int X, int Y)> Open { get; }
    internal (int X, int Y)? Blocked { get; }
    internal (int X, int Y)? Target { get; }
    // Blocked tiles the beam passed through, in Open as well.
    internal IReadOnlyList<(int X, int Y)> Passed { get; }
}

// The magic circle turns in the game. Both round rims stand still, only the
// drawing inside turns (as in the rotation test of the template, rim from radius 10.5 of 32).
// A pixel belongs to the rim when its centre lies at or beyond the radius from the image centre.
internal static class ZoltraakCircleRules
{
    internal const float RimRadius = 10.5f;

    internal static bool IsRim(int x, int y, int size, float rimRadius = RimRadius)
    {
        var dx = x + 0.5 - size / 2.0;
        var dy = y + 0.5 - size / 2.0;
        return Math.Sqrt(dx * dx + dy * dy) >= rimRadius;
    }
}

// The beam's pointed start: a pixel of the width x height strip is inside when it lies within the
// half height that grows from a two-pixel tip at x = 0 to the full beam at the last column.
internal static class ZoltraakTaperRules
{
    internal static bool Inside(int x, int y, int width, int height)
    {
        var half = 0.6 + (height / 2.0 - 0.6) * Math.Pow(x / (double)(width - 1), 0.8);
        return Math.Abs(y - (height - 1) / 2.0) <= half;
    }
}

// Display rules of the spell's numbers: "25 %" with a non-breaking space where the language
// writes one (German, French, Spanish), "25%" elsewhere, never a line break inside.
internal static class ZoltraakTextRules
{
    internal static string Percent(float value, bool spaced)
        => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + (spaced ? " %" : "%");

    // Tooltip step 1: the description names its numbers as the game's own skills do. The game's
    // tooltip fills {n} from the skill row's FloatValues and colours the [STATUS_...] tag (Tooltip_Base.ApplyTooltipFormatText,
    // StringTools.FormatText). {0} percent of the magic attack per hit, {1} hits, {2} seconds of the channel, {3} resistance
    // points per break stack, {4} seconds of the break, {5} break stacks at most. Only the tooltip reads them; the channel is
    // Frieren's own (ChannelSkills replaces the vines' routine).
    internal static float[] TooltipValues(float hitPercent, int ticks, float tickInterval, float perStack, float breakSeconds, int breakStacks)
        => new[] { hitPercent, ticks, (float)Math.Round(ticks * tickInterval, 2), perStack, breakSeconds, breakStacks };

    // In the game's language order: English, Korean, French, German, Russian, Simplified and Traditional Chinese, Japanese,
    // Spanish, Portuguese. Each language follows the phrasing of the game's own skills in it (TextKeyTable, e.g.
    // SKILL_StrangeSeed, SKILL_CheeringHowl). The break sentence.
    internal static readonly string[] Descriptions =
    {
        "A beam with {1} hits over {2} seconds that ends at the first enemy. An obstacle in the way stops it and takes the damage if it can be destroyed. Each hit deals {0}% of [STATUS_MagicAttackPower] as magic damage. Each hit applies [AFFECTER_DelversZoltraakBreak] for {4} seconds: per stack, [STATUS_PhysicalResistance] and [STATUS_MagicResistance] fall by {3}, up to {5} stacks.",
        "{2}초 동안 {1}회 타격하며 첫 번째 적에서 멈추는 광선. 앞을 막는 장애물은 광선을 막고, 파괴할 수 있으면 피해를 받습니다. 타격마다 [STATUS_MagicAttackPower]의 {0}%의 마법 피해를 입힙니다. 타격마다 {4}초 동안 [AFFECTER_DelversZoltraakBreak]을 부여합니다: 중첩당 [STATUS_PhysicalResistance]와 [STATUS_MagicResistance]가 {3} 감소하며, 최대 {5}중첩.",
        "Un rayon de {1} coups en {2} secondes qui s'arrête au premier ennemi. Un obstacle sur le chemin l'arrête et subit les dégâts s'il peut être détruit. Chaque coup inflige des dégâts magiques équivalents à {0}% de [STATUS_MagicAttackPower]. Chaque coup applique [AFFECTER_DelversZoltraakBreak] pendant {4} secondes : par cumul, [STATUS_PhysicalResistance] et [STATUS_MagicResistance] baissent de {3}, jusqu'à {5} cumuls.",
        "Ein Strahl mit {1} Treffern in {2} Sekunden, der am ersten Gegner endet. Ein Hindernis davor fängt ihn ab und nimmt Schaden, wenn es zerstörbar ist. Jeder Treffer verursacht magischen Schaden in Höhe von {0}% × [STATUS_MagicAttackPower]. Jeder Treffer belegt das Ziel {4} Sekunden lang mit [AFFECTER_DelversZoltraakBreak]: Je Stapel sinken [STATUS_PhysicalResistance] und [STATUS_MagicResistance] um {3}, bis zu {5} Stapel.",
        "Луч из {1} ударов за {2} сек., который заканчивается на первом враге. Препятствие на пути останавливает его и получает урон, если его можно разрушить. Каждый удар наносит магический урон в размере {0}% от значения «[STATUS_MagicAttackPower]». Каждый удар накладывает [AFFECTER_DelversZoltraakBreak] на {4} сек.: за каждое наложение [STATUS_PhysicalResistance] и [STATUS_MagicResistance] снижаются на {3}, не более {5} наложений.",
        "持续{2}秒、命中{1}次的光束，止于第一个敌人。途中的障碍物会挡住光束，若可被摧毁则会受到伤害。每次命中造成[STATUS_MagicAttackPower] {0}% 的魔法伤害。每次命中施加[AFFECTER_DelversZoltraakBreak]，持续{4}秒：每层使[STATUS_PhysicalResistance]和[STATUS_MagicResistance]降低{3}，最多{5}层。",
        "持續{2}秒、命中{1}次的光束，止於第一個敵人。途中的障礙物會擋住光束，若可被摧毀則會受到傷害。每次命中造成相當於 [STATUS_MagicAttackPower] {0}% 的魔法傷害。每次命中施加[AFFECTER_DelversZoltraakBreak]，持續{4}秒：每層使[STATUS_PhysicalResistance]和[STATUS_MagicResistance]降低{3}，最多{5}層。",
        "{2}秒間に{1}回ヒットし、最初の敵で止まるビーム。途中の障害物はビームを遮り、破壊可能ならダメージを受ける。1ヒットごとに[STATUS_MagicAttackPower]の{0}%の魔法ダメージを与える。1ヒットごとに{4}秒間[AFFECTER_DelversZoltraakBreak]を付与する：スタックごとに[STATUS_PhysicalResistance]と[STATUS_MagicResistance]が{3}低下し、最大{5}スタック。",
        "Un rayo de {1} golpes en {2} segundos que termina en el primer enemigo. Un obstáculo en el camino lo detiene y recibe el daño si se puede destruir. Cada golpe inflige un daño mágico equivalente al {0}% de [STATUS_MagicAttackPower]. Cada golpe aplica [AFFECTER_DelversZoltraakBreak] durante {4} segundos: por acumulación, [STATUS_PhysicalResistance] y [STATUS_MagicResistance] bajan {3}, hasta {5} acumulaciones.",
        "Um raio de {1} golpes em {2} segundos que termina no primeiro inimigo. Um obstáculo no caminho o detém e recebe o dano se puder ser destruído. Cada golpe causa {0}% de [STATUS_MagicAttackPower] como dano mágico. Cada golpe aplica [AFFECTER_DelversZoltraakBreak] por {4} segundos: por acúmulo, [STATUS_PhysicalResistance] e [STATUS_MagicResistance] caem {3}, até {5} acúmulos."
    };

    internal static string DescriptionEnglish => Descriptions[0];

    internal static string DescriptionGerman => Descriptions[3];
    // The highest {n} a text asks for; -1 without one. The row must hold more values than that ("[No_Value]" otherwise).
    internal static int HighestPlaceholder(string text)
    {
        var highest = -1;
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text ?? "", @"(?<!\{)\{(\d+)\}(?!\})"))
            highest = Math.Max(highest, int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        return highest;
    }
}

// Zoltraak's own sounds are plain PCM WAV files (16 or 24 bit, any channel count and rate).
// Samples come back interleaved as floats from -1 to 1, ready for AudioClip.SetData.
internal static class ZoltraakWavRules
{
    internal static ZoltraakWav Read(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 12 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE")
            throw new InvalidDataException("keine WAV-Datei");
        int pos = 12, format = 0, channels = 0, rate = 0, bits = 0, dataPos = -1, dataLength = 0;
        while (pos + 8 <= bytes.Length)
        {
            var id = Encoding.ASCII.GetString(bytes, pos, 4);
            var length = BitConverter.ToInt32(bytes, pos + 4);
            if (length < 0) break;
            if (id == "fmt " && pos + 24 <= bytes.Length)
            {
                format = BitConverter.ToInt16(bytes, pos + 8);
                channels = BitConverter.ToInt16(bytes, pos + 10);
                rate = BitConverter.ToInt32(bytes, pos + 12);
                bits = BitConverter.ToInt16(bytes, pos + 22);
            }
            else if (id == "data")
            {
                dataPos = pos + 8;
                dataLength = Math.Min(length, bytes.Length - dataPos);
                break;
            }
            pos += 8 + length + (length & 1);
        }
        if (format != 1 || channels < 1 || rate < 1 || (bits != 16 && bits != 24))
            throw new InvalidDataException($"nur PCM mit 16 oder 24 Bit (Format {format}, {bits} Bit)");
        if (dataPos < 0) throw new InvalidDataException("WAV ohne Daten");
        var size = bits / 8;
        var frames = dataLength / (size * channels);
        if (frames < 1) throw new InvalidDataException("WAV ohne Daten");
        var samples = new float[frames * channels];
        for (var i = 0; i < samples.Length; i++)
        {
            var p = dataPos + i * size;
            samples[i] = bits == 16 ? BitConverter.ToInt16(bytes, p) / 32768f
                : (bytes[p] | (bytes[p + 1] << 8) | ((sbyte)bytes[p + 2] << 16)) / 8388608f;
        }
        return new ZoltraakWav(channels, rate, frames, samples);
    }
}

internal sealed class ZoltraakWav
{
    internal ZoltraakWav(int channels, int rate, int frames, float[] samples)
    {
        Channels = channels;
        Rate = rate;
        Frames = frames;
        Samples = samples;
    }

    internal int Channels { get; }
    internal int Rate { get; }
    internal int Frames { get; }
    internal float[] Samples { get; }
}

// (after a wipe, back to the menu and the save loaded again, Zoltraak looked wrong): the beam's
// animation and the sound each run one loop, started only while none is known (animator, runner) and forgotten in its finally. A
// loop stopped from outside never reaches that finally and would block every later start. Each loop notes a heartbeat; a known
// loop without one for StaleSeconds counts as gone and a new one starts.
internal static class ZoltraakLoopRules
{
    internal const float StaleSeconds = 2f;

    internal static bool Stale(bool known, float now, float beat) => known && now - beat > StaleSeconds;

    // (menue-pause, FAIL MN4): paused in game, to the title, loaded and played on at "Normal", the beam froze and
    // the sounds stayed mute: the speed cache still held the pause's 0, the load sends no speed change. After every load the cache
    // takes the campaign's own multiplier of its current speed; 1 when the campaign has none to give.
    internal static float AfterLoad(float? campaign) => campaign is float value && !float.IsNaN(value) ? Math.Max(0f, value) : 1f;
}

// Which other mods patch the methods Frieren's recruitment goes through (FrierenRecruitPatchAudit): every Harmony owner that
// is not one of the Delvers packs. Rules without the game, so the offline tests cover them.
internal static class FrierenRecruitPatchAuditRules
{
    internal const string OwnPrefix = "fre-yin.dungeonsettlers.delvers";

    internal static List<string> Foreign(IEnumerable<string> owners)
    {
        var foreign = new List<string>();
        if (owners == null) return foreign;
        foreach (var owner in owners)
            if (!string.IsNullOrEmpty(owner) && !owner.StartsWith(OwnPrefix, StringComparison.Ordinal) && !foreign.Contains(owner))
                foreign.Add(owner);
        foreign.Sort(StringComparer.Ordinal);
        return foreign;
    }

    internal static string Line(string target, bool found, IReadOnlyCollection<string> foreign)
        => "FRIEREN_RECRUIT_PATCH_AUDIT target=" + target + " found=" + (found ? 1 : 0)
            + " foreign=" + (foreign == null || foreign.Count == 0 ? "-" : string.Join(",", foreign));
}
