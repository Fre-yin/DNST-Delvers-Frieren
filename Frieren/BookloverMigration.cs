using Il2CppInterop.Runtime;
using CandidateTraits = Il2CppSystem.Collections.Generic.List<string>;

namespace FrierenPortrait;

// Development saves predate Booklover. Saved Frieren units regain it through
// Core's fixed-trait restore (FrierenFixedTraitRules); saved recruit
// candidates with Frieren's distinct profile ID are upgraded here.
internal static class BookloverMigration
{
    internal static int EnsureCandidates(Il2CppSystem.Collections.Generic.List<RecruitCandidateData> candidates)
    {
        if (candidates == null) return 0;
        var changed = 0;
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (candidate?.UnitProfileKey != FrierenIds.ProfileKey) continue;
            candidate.IndividualTraits ??= new CandidateTraits();
            var found = false;
            for (var j = 0; j < candidate.IndividualTraits.Count; j++)
                if (candidate.IndividualTraits[j] == Booklover.TraitKey) { found = true; break; }
            if (found) continue;
            candidate.IndividualTraits.Add(Booklover.TraitKey);
            changed++;
        }
        return changed;
    }

}
