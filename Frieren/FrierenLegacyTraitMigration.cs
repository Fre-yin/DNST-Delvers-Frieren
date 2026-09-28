using Il2CppInterop.Runtime;
#if BEPINEX
using global::Refactor;
#else
using Il2CppRefactor;
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
using ComponentSaveList = Il2CppSystem.Collections.Generic.List<global::Refactor.ComponentSaveData>;
#else
using ComponentSaveList = Il2CppSystem.Collections.Generic.List<Il2CppRefactor.ComponentSaveData>;
#endif
#if BEPINEX
using HolderList = Il2CppSystem.Collections.Generic.List<global::Refactor.Component.AffecterHolder>;
#else
using HolderList = Il2CppSystem.Collections.Generic.List<Il2CppRefactor.Component.AffecterHolder>;
#endif

namespace FrierenPortrait;

// The first gameplay test build saved the archmage key as an individual trait
// alongside the native Mage background. Affecter save entries do not store a
// trait type, so the current Background registration reclassifies the custom
// key automatically. Only the now-redundant Mage holder needs migration.
internal static class FrierenLegacyTraitMigration
{
    internal const string LegacyBackgroundKey = "AFFECTER_Mage";

    internal static int RemoveLegacyMage(ComponentSaveList savedDataList)
    {
        if (savedDataList == null) return 0;

        UnitProfileComponentSaveData profile = null;
        AffecterComponentSaveData affecters = null;
        for (var i = 0; i < savedDataList.Count; i++)
        {
            var component = savedDataList[i];
            if (component?.Data == null) continue;
            if (component.Type == ComponentType.UnitProfile)
            {
                if (profile != null) throw new InvalidDataException("Mehrere Profilkomponenten in derselben Einheit.");
                profile = component.Data.TryCast<UnitProfileComponentSaveData>()
                    ?? throw new InvalidDataException("Profilkomponente besitzt unerwartete Speicherdaten.");
            }
            else if (component.Type == ComponentType.Affecter)
            {
                if (affecters != null) throw new InvalidDataException("Mehrere Eigenschaftskomponenten in derselben Einheit.");
                affecters = component.Data.TryCast<AffecterComponentSaveData>()
                    ?? throw new InvalidDataException("Eigenschaftskomponente besitzt unerwartete Speicherdaten.");
            }
        }

        // Both identity checks are required. This must never alter a native
        // mage or a character that merely uses Frieren's portrait/profile.
        if (profile?.ProfileKey != FrierenIds.ProfileKey || affecters?.AffecterHolders == null) return 0;
        var holders = affecters.AffecterHolders;
        var hasArchmage = false;
        var mageCount = 0;
        for (var i = 0; i < holders.Count; i++)
        {
            var key = holders[i]?.Key;
            if (key == FrierenGameplay.TraitKey) hasArchmage = true;
            else if (key == LegacyBackgroundKey) mageCount++;
        }
        if (!hasArchmage || mageCount == 0) return 0;

        // Build first and publish once, so an allocation/read failure cannot
        // leave the load data half-migrated.
        var migrated = new HolderList();
        for (var i = 0; i < holders.Count; i++)
            if (holders[i]?.Key != LegacyBackgroundKey) migrated.Add(holders[i]);
        affecters.AffecterHolders = migrated;
        return mageCount;
    }

}
