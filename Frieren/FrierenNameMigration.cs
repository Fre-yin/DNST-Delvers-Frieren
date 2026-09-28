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

namespace FrierenPortrait;

// Candidate names may be either localization keys or literal player-facing text.
// Frieren uses the native literal-name path. These narrow load hooks repair saves
// created by development builds that persisted the old technical name key.
internal static class FrierenNameMigration
{
    internal static bool Normalize(UnitProfileComponent profile, out string previousName)
    {
        previousName = null;
        if (profile == null || profile.GetProfileKey() != FrierenIds.ProfileKey) return false;
        previousName = profile.GetNameTextKey();
        if (!FrierenNameMigrationRules.ShouldMigrate(profile.GetProfileKey(), previousName,
            FrierenIds.ProfileKey, FrierenGameplay.LegacyNameKey, FrierenGameplay.NameKey)) return false;
        profile.SetNameTextKey(FrierenGameplay.NameKey);
        return true;
    }

    internal static bool Normalize(RecruitCandidateData candidate)
    {
        if (candidate == null || !FrierenNameMigrationRules.ShouldMigrate(candidate.UnitProfileKey,
            candidate.NameTextKey, FrierenIds.ProfileKey, FrierenGameplay.LegacyNameKey,
            FrierenGameplay.NameKey)) return false;
        candidate.NameTextKey = FrierenGameplay.NameKey;
        return true;
    }

    internal static int Normalize(
        Il2CppSystem.Collections.Generic.List<RecruitCandidateData> candidates)
    {
        if (candidates == null) return 0;
        var changed = 0;
        for (var i = 0; i < candidates.Count; i++)
            if (Normalize(candidates[i])) changed++;
        return changed;
    }
}
