using System.Reflection;
using HarmonyLib;

namespace FrierenPortrait;

// Once per session, at the first refresh of the recruitment (by then every mod has patched): which other mods patch the methods
// Frieren's recruitment goes through, one line per method (FrierenRecruitPatchAuditRules). It only reads Harmony's patch info,
// so a crash report shows whether another mod sat on the same path.
internal static class FrierenRecruitPatchAudit
{
    private static bool done;

    internal static void RunOnce()
    {
        if (done) return;
        done = true;
        foreach (var (name, method) in Targets())
        {
            var owners = method == null ? null : Harmony.GetPatchInfo(method)?.Owners;
            DelversHost.Info(FrierenRecruitPatchAuditRules.Line(name, method != null, FrierenRecruitPatchAuditRules.Foreign(owners)));
        }
    }

    private static IEnumerable<(string Name, MethodBase Method)> Targets()
    {
        yield return ("RecruitHelper.CreateRecruitCandidate", AccessTools.Method(typeof(RecruitHelper), nameof(RecruitHelper.CreateRecruitCandidate),
            new[] { typeof(ClanRankTableData), typeof(Il2CppSystem.Collections.Generic.HashSet<string>), typeof(Il2CppSystem.Collections.Generic.HashSet<string>) }));
        yield return ("RecruitHelper.RefreshExist", AccessTools.Method(typeof(RecruitHelper), nameof(RecruitHelper.RefreshExist)));
        yield return ("EntityEventHandler.OnEvent(CandidateRecruitRequested)",
            AccessTools.Method(typeof(EntityEventHandler), nameof(EntityEventHandler.OnEvent), new[] { typeof(CandidateRecruitRequested) }));
        yield return ("EntityEventHandler.OnEvent(SpawnUnitRequested)",
            AccessTools.Method(typeof(EntityEventHandler), nameof(EntityEventHandler.OnEvent), new[] { typeof(SpawnUnitRequested) }));
    }
}
