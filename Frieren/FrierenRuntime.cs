using HarmonyLib;
#if BEPINEX
using global::Refactor;
#else
using Il2CppRefactor;
#endif
#if BEPINEX
using global::Refactor.Util;
#else
using Il2CppRefactor.Util;
#endif
#if BEPINEX
using global::Util;
#else
using Il2CppUtil;
#endif
#if BEPINEX
using global::Util.Sheet;
#else
using Il2CppUtil.Sheet;
#endif
using UnityEngine;

namespace FrierenPortrait;

// Loader-neutral runtime state. A loader adapter supplies lifecycle and Harmony;
// shared logging and coroutine services come from Core.
public sealed class FrierenRuntime
{
    internal static FrierenRuntime Instance { get; private set; }
    internal readonly PortraitAssets Assets = new();
    internal UnitProfileTableData Profile;
    internal UnitProfileTableData Template;
    internal UnitProfileTableData VisualSource;
    private UnitProfileSheet registeredSheet;
    private UnitVisualDatabase registeredDatabase;
    private TraitSheet registeredTraitSheet;
    private AffecterSheet registeredAffecterSheet;
    private bool busy, failed, enabled;
    private IDisposable coreIntegration;
    private IDisposable loadIntegration;
    private IDisposable uniqueCandidateRegistration;
    private IDisposable fixedTraitRegistration;

    public void Initialize(HarmonyLib.Harmony harmony)
    {
        if (!DelversCoreRuntime.IsReady
            || !DelversCoreRuntime.SupportsApi(DelversCoreRuntime.MinimumApiVersionForLoadIntegrations))
            throw new InvalidOperationException("Frieren requires Dungeon Settlers Delvers Core API 1.3.0 or later in API 1.x.");
        if (Instance != null)
            throw new InvalidOperationException("A Frieren runtime instance is already active; the second loader entry was rejected before registration.");
        LegacyFrierenConflict.ThrowIfPresent();
        Instance = this;
        try
        {
            coreIntegration = DelversCoreRuntime.RegisterRecruitmentIntegration("frieren",
                FrierenCampaignHistory.ObservePlayerUnits,
                () => false);
            loadIntegration = DelversCoreRuntime.RegisterLoadIntegration("frieren",
                FrierenSaveMigrations.BeforeComponentsDeserialize,
                FrierenSaveMigrations.BeforeUnitProfileDeserialize,
                FrierenSaveMigrations.AfterUnitProfileDeserialize,
                FrierenSaveMigrations.AfterCampaignLoaded);
            uniqueCandidateRegistration = DelversCoreRuntime.RegisterUniqueCandidatePredicate(
                "frieren", IsUniqueCandidate);
            fixedTraitRegistration = DelversCoreRuntime.RegisterFixedTraits(
                "frieren", FrierenIds.ProfileKey, FrierenFixedTraitRules.OrderedFixedTraits);
            FrierenBootstrap.Initialize(harmony);
            enabled = true;
            DelversHost.Info($"Frieren portrait hooks ready; {FrierenBootstrap.SupportedBuilds} verified.");
        }
        catch (Exception ex)
        {
            Fail(ex);
            try { harmony.UnpatchSelf(); } catch { }
            fixedTraitRegistration?.Dispose();
            fixedTraitRegistration = null;
            loadIntegration?.Dispose();
            loadIntegration = null;
            uniqueCandidateRegistration?.Dispose();
            uniqueCandidateRegistration = null;
            coreIntegration?.Dispose();
            coreIntegration = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
            throw;
        }
    }

    public void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        FrierenLifecycle.ResetSceneState();
        if (enabled) { EnsureGameplay(); Ensure(DataSheetManager.Instance?._unitProfile); }
    }

    internal void EnsureGameplay(bool force = false)
    {
        if (!enabled || failed) return;
        var manager = DataSheetManager.Instance;
        if (manager?.IsReady == true)
        {
            try { FrierenLocalization.EnsureCurrent(); }
            catch (Exception ex) { Fail(ex); return; }
        }
        var traits = manager?._trait;
        var affecters = manager?._affecter;
        if (manager?.IsReady != true || traits?._traitTable == null || affecters?._affecterTable == null
            || traits.GetData("AFFECTER_Mage") == null || affecters.GetData("AFFECTER_Mage") == null
            || traits.GetData("AFFECTER_SlowLearner") == null || affecters.GetData("AFFECTER_SlowLearner") == null
            || affecters.GetData("AFFECTER_PlayedCard") == null) return;
        if (!force && registeredTraitSheet?.Pointer == traits.Pointer
            && registeredAffecterSheet?.Pointer == affecters.Pointer
            && traits.GetData(FrierenGameplay.TraitKey) != null
            && affecters.GetData(FrierenGameplay.TraitKey) != null
            && traits.GetData(Booklover.TraitKey) != null
            && affecters.GetData(Booklover.TraitKey) != null
            && affecters.GetData(Booklover.MoodKey) != null) return;
        try
        {
            FrierenGameplay.Register(traits);
            FrierenGameplay.Register(affecters);
            Booklover.Register(traits);
            Booklover.Register(affecters);
            if (traits.GetData(FrierenGameplay.TraitKey) == null || affecters.GetData(FrierenGameplay.TraitKey) == null
                || traits.GetData(Booklover.TraitKey) == null || affecters.GetData(Booklover.TraitKey) == null
                || affecters.GetData(Booklover.MoodKey) == null)
                throw new InvalidOperationException("Die Frieren-Traits konnten nicht vollständig registriert werden.");
            registeredTraitSheet = traits;
            registeredAffecterSheet = affecters;
            DelversHost.Info("FRIEREN_TRAIT_REGISTERED key=" + FrierenGameplay.TraitKey
                + " type=Background rarity=Legendary physicalDamage=-40% maxEnergy=+50 cooldownReduction=+20 magicBypass=Fire,Water,Nature persistent=true");
            DelversHost.Info("BOOKLOVER_REGISTERED key=" + Booklover.TraitKey
                + " type=Individual rarity=Common mood=+4 duration=1200s allRacePools=true");
        }
        catch (Exception ex) { Fail(ex); }
    }

    internal void Ensure(UnitProfileSheet sheet, bool force = false)
    {
        if (!enabled || failed || busy || sheet?._profileTable == null || sheet._raceToProfiles == null) return;
        if (!force && registeredSheet?.Pointer == sheet.Pointer && Assets.Alive) return;
        busy = true;
        try
        {
            // The game's serialized database is owned by this scene manager. The
            // ScriptableObject singleton is not initialized by the player build.
            var database = UnitVisualDataManager.Instance?._database;
            if (!database) return;
            if (!sheet._raceToProfiles.TryGetValue(RaceType.Elf, out var elves) || elves == null || elves.Count == 0) return;
            // Fixed native template and ID: another mod adding an earlier sort key
            // must never change the identity written to existing saved characters.
            if (!sheet._profileTable.TryGetValue(FrierenIds.TemplateKey, out var template) || template == null
                || template.GenderType != GenderType.Female || template.AppearWeight <= 0
                || TextKeyExtensions.GetRaceType(template.Key) != RaceType.Elf)
                throw new InvalidOperationException("Das geprüfte weibliche Elfenprofil fehlt oder wurde inkompatibel verändert.");
            if (!sheet._profileTable.TryGetValue(FrierenIds.VisualSourceKey, out var visualSource)
                || visualSource == null || visualSource.GenderType != GenderType.Female
                || visualSource.HeadType != template.HeadType
                || visualSource.BodyType != template.BodyType
                || visualSource.SkinColor != template.SkinColor
                || TextKeyExtensions.GetRaceType(visualSource.Key) != RaceType.Elf)
                throw new InvalidOperationException("Das geprüfte Teemu-Frieren-Basisprofil fehlt oder wurde inkompatibel verändert.");
            Template = template;
            VisualSource = visualSource;
            var profile = Clone(Template);
            // Keep the native prefix: race and visual type are inferred from this key.
            profile.Key = FrierenIds.ProfileKey;
            profile.Portrait = FrierenIds.PortraitKey;
            profile.HairType = FrierenIds.CustomHairType;
            profile.EyesType = VisualSource.EyesType;
            profile.DevComment1 = "Frieren, weibliche Elfe";
            var originalVisual = new UnitProfileData(Template);
            var customVisual = new UnitProfileData(profile);
            if (TextKeyExtensions.GetRaceType(profile.Key) != RaceType.Elf
                || customVisual.UnitVisualType != originalVisual.UnitVisualType)
                throw new InvalidOperationException("Profilkennung wird vom Spiel nicht als Elfenprofil erkannt.");

            if (sheet._profileTable.TryGetValue(profile.Key, out var existing)
                && existing?.Pointer != Profile?.Pointer)
                throw new InvalidOperationException("Die Frieren-Profilkennung ist bereits durch einen anderen Eintrag belegt.");
            for (var i = 0; i < elves.Count; i++)
                if (elves[i]?.Portrait == FrierenIds.PortraitKey && elves[i]?.Pointer != Profile?.Pointer)
                    throw new InvalidOperationException("Die Frieren-Porträtkennung ist bereits belegt.");

            Assets.Load();
            RegisterVisuals(database);
            // Publish the selectable profile only after both states resolve successfully.
            var addedKey = false;
            try
            {
                if (existing != null) profile = existing;
                else { sheet._profileTable.Add(profile.Key, profile); addedKey = true; }
                var count = 0;
                for (var i = 0; i < elves.Count; i++) if (elves[i]?.Key == profile.Key) count++;
                if (count > 1) throw new InvalidOperationException("Doppeltes Frieren-Profil gefunden.");
                if (count == 0) elves.Add(profile);
            }
            catch
            {
                if (addedKey) sheet._profileTable.Remove(profile.Key);
                throw;
            }
            Profile = profile;
            registeredSheet = sheet;
            DelversHost.Info($"FRIEREN_REGISTERED key={Profile.Key} portrait={FrierenIds.PortraitKey} template={Template.Key} visualSource={VisualSource.Key} race=Elf gender=Female weight={Profile.AppearWeight} states=2");
        }
        catch (Exception ex) { Fail(ex); }
        finally { busy = false; }
    }

    internal void RegisterVisuals(UnitVisualDatabase database)
    {
        if (!database) throw new InvalidOperationException("Porträt-Datenbank ist nicht verfügbar.");
        if (!Assets.PortraitReady) Assets.Load();
        var nativeHair = database.GetHeadTypeHairSpriteData(VisualSource.HeadType, VisualSource.HairType);
        Assets.LoadWorldHair(nativeHair);
        var normals = database._portraitSpriteDictionary;
        var stresses = database._portraitStressSpriteDictionary;
        if (normals == null || stresses == null) throw new InvalidOperationException("Porträt-Datenbank ist nicht initialisiert.");
        if (registeredDatabase?.Pointer != database.Pointer)
        {
            if (normals.ContainsKey(FrierenIds.PortraitKey) || stresses.ContainsKey(FrierenIds.PortraitKey))
                throw new InvalidOperationException("Die Frieren-Grafikkennung ist bereits belegt.");
        }
        database.PlacePortraitSpriteData(FrierenIds.PortraitKey, Assets.Normal);
        try { database.PlaceStressPortraitSpriteData(FrierenIds.PortraitKey, Assets.Stress); }
        catch { normals.Remove(FrierenIds.PortraitKey); throw; }
        database.PlaceHeadTypeHairSpriteData(VisualSource.HeadType, FrierenIds.CustomHairType, Assets.WorldHair);
        registeredDatabase = database;
    }

    internal void EnsureVisuals(UnitVisualDatabase database)
    {
        if (!enabled || failed || busy || !database || VisualSource == null
            || database._portraitSpriteDictionary == null || database._portraitStressSpriteDictionary == null) return;
        try
        {
            var hair = database.GetHeadTypeHairSpriteData(VisualSource.HeadType, FrierenIds.CustomHairType);
            if (Assets.Alive && registeredDatabase?.Pointer == database.Pointer
                && database._portraitSpriteDictionary.TryGetValue(FrierenIds.PortraitKey, out var normal) && normal == Assets.Normal
                && database._portraitStressSpriteDictionary.TryGetValue(FrierenIds.PortraitKey, out var stress) && stress == Assets.Stress
                && hair != null && hair.Count == 3 && hair[0] == Assets.WorldHair[0]) return;
            RegisterVisuals(database);
        }
        catch (Exception ex) { Fail(ex); }
    }

    internal static UnitProfileTableData Clone(UnitProfileTableData source) => new()
    {
        Key = source.Key, DevComment1 = source.DevComment1, AppearWeight = source.AppearWeight,
        GenderType = source.GenderType, Portrait = source.Portrait, PersonalityType = source.PersonalityType,
        SkinColor = source.SkinColor, HairType = source.HairType, FacialDecoType = source.FacialDecoType,
        EyesType = source.EyesType, HeadType = source.HeadType, BodyType = source.BodyType
    };

    private void Fail(Exception ex)
    {
        if (!failed) DelversHost.Error("FRIEREN_DISABLED: " + ex);
        failed = true;
    }

    public void Update()
    {
        if (enabled && !failed)
        {
            EnsureGameplay();
        }
        if (enabled && !failed && registeredSheet == null)
            Ensure(DataSheetManager.Instance?._unitProfile);
    }

    public void Shutdown()
    {
        fixedTraitRegistration?.Dispose();
        fixedTraitRegistration = null;
        loadIntegration?.Dispose();
        loadIntegration = null;
        uniqueCandidateRegistration?.Dispose();
        uniqueCandidateRegistration = null;
        coreIntegration?.Dispose();
        coreIntegration = null;
        FrierenLifecycle.ResetSceneState();
        FrierenLocalization.ResetLifecycle();
        // Runtime unloading would leave saved custom IDs unresolved; only free on game exit.
        Assets.Dispose();
        if (ReferenceEquals(Instance, this)) Instance = null;
    }

    private static bool IsUniqueCandidate(RecruitCandidateData candidate)
        => candidate != null && FrierenUniqueCandidateRules.IsUniqueCandidate(
            candidate.UnitProfileKey, candidate.BackgroundTrait);
}

internal static class LegacyFrierenConflict
{
    private const string LegacyAssemblyName = "DungeonSettlersFrierenPortrait";
    private const string LegacyFileName = LegacyAssemblyName + ".dll";

    internal static void ThrowIfPresent()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name,
                LegacyAssemblyName, StringComparison.OrdinalIgnoreCase));
        if (loaded != null)
            throw new InvalidOperationException("The legacy Frieren DLL is already loaded. Remove "
                + LegacyFileName + " from Mods or BepInEx/plugins and restart the game.");

        var gameRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
        var pluginRoots = new[]
        {
            Path.Combine(gameRoot, "Mods"),
            Path.Combine(gameRoot, "BepInEx", "plugins")
        };
        string file = null;
        foreach (var root in pluginRoots)
        {
            if (!Directory.Exists(root)) continue;
            try { file = Directory.EnumerateFiles(root, LegacyFileName, SearchOption.AllDirectories).FirstOrDefault(); }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            if (file != null) break;
        }
        if (file != null)
            throw new InvalidOperationException("Remove the legacy Frieren DLL at " + file
                + " and restart the game before loading Dungeon Settlers Delvers: Frieren.");
    }
}

[HarmonyPatch(typeof(UnitProfileSheet), nameof(UnitProfileSheet.Parse))]
internal static class ParsedProfilesPatch
{
    private static void Postfix(UnitProfileSheet __instance) => FrierenRuntime.Instance?.Ensure(__instance, true);
}

[HarmonyPatch(typeof(UnitProfileSheet), nameof(UnitProfileSheet.GetProfile))]
internal static class ProfilePoolPatch
{
    private static void Prefix(UnitProfileSheet __instance) => FrierenRuntime.Instance?.Ensure(__instance);
}

[HarmonyPatch(typeof(UnitProfileSheet), nameof(UnitProfileSheet.GetData))]
internal static class ProfileLookupPatch
{
    private static void Prefix(UnitProfileSheet __instance) => FrierenRuntime.Instance?.Ensure(__instance);
}

[HarmonyPatch(typeof(UnitVisualDatabase), nameof(UnitVisualDatabase.GetPortraitSprite))]
internal static class PortraitLookupPatch
{
    private static void Prefix(UnitVisualDatabase __instance, string key)
    {
        if (key == FrierenIds.PortraitKey) FrierenRuntime.Instance?.EnsureVisuals(__instance);
    }
}
