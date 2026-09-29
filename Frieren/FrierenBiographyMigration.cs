
namespace FrierenPortrait;

// Development versions left Frieren's biography empty. Fill only that empty
// value so player-authored or future custom biographies remain untouched.
internal static class FrierenBiographyMigration
{
    internal static bool Normalize(UnitProfileComponentSaveData saved)
    {
        if (saved == null || !FrierenBiographyMigrationRules.ShouldMigrate(saved.ProfileKey,
            saved.UnitBioTextKey, FrierenIds.ProfileKey)) return false;
        saved.UnitBioTextKey = FrierenLocalization.BiographyKey;
        return true;
    }

    internal static bool Normalize(RecruitCandidateData candidate)
    {
        if (candidate == null || !FrierenBiographyMigrationRules.ShouldMigrate(candidate.UnitProfileKey,
            candidate.BioTextKey, FrierenIds.ProfileKey)) return false;
        candidate.BioTextKey = FrierenLocalization.BiographyKey;
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
