using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace FrierenPortrait;

// The native candidate generator excludes profiles only while a matching unit
// still exists. A campaign marker also remembers Frieren after her death.
// Core decides whether uniqueness applies: only a test campaign with free
// recruitment relaxes it (API 1.6.0).
internal static class FrierenCampaignHistory
{
    // The one rule for keeping Frieren out of a candidate pool: she was already
    // recruited in this campaign and Core enforces uniqueness here.
    internal static bool Excluded(Il2CppSystem.Guid guid)
    {
        var relaxed = !DelversCoreRuntime.UniquenessEnforced(guid.ToString());
        return !relaxed && Seen(guid);
    }

    internal static bool Seen(Il2CppSystem.Guid guid)
        => CampaignPresence.Seen(guid.ToString(), "frieren",
            CampaignPresence.LegacyMarkerName(guid.ToString()));

    internal static void Mark(Il2CppSystem.Guid guid)
    {
        CampaignPresence.Mark(guid.ToString(), "frieren", CampaignPresence.LegacyMarkerName(guid.ToString()),
            "Frieren war in dieser Kampagne bereits rekrutiert.\n");
    }

    internal static void ObservePlayerUnits(RecruitHelper helper)
    {
        // The first refresh of a session also notes which other mods patch the recruitment's methods.
        try { FrierenRecruitPatchAudit.RunOnce(); }
        catch (Exception ex) { DelversHost.Warning("FRIEREN_RECRUIT_PATCH_AUDIT_FAILED: " + ex.Message); }
        var entities = helper?._entityContainer?.GetAllEntitiesOnCampaign();
        var clan = helper?._clanDataContainer;
        if (entities == null || clan == null) return;
        foreach (var key in entities.Keys)
        {
            var unit = entities[key]?.TryCast<UnitEntity>();
            if (unit != null && unit.Faction == FactionType.Player && ProfileKeyOf(unit) == FrierenIds.ProfileKey)
            {
                Mark(clan.CampaignGuid);
                break;
            }
        }
        if (Excluded(clan.CampaignGuid)) helper._existProfiles?.Add(FrierenIds.ProfileKey);
    }

    internal static void ObservePlayerUnits(EntityEventHandler handler)
    {
        var entities = handler?._entityContainer?.GetAllEntitiesOnCampaign();
        var clan = handler?._clanDataContainer;
        if (clan != null) ObservePlayerUnits(entities, clan.CampaignGuid);
    }

    internal static void ObservePlayerUnits(CampaignDataContainer campaign)
    {
        var entities = campaign?.EntityDataContainer?.GetAllEntitiesOnCampaign();
        var clan = campaign?.ClanDataContainer;
        if (clan != null) ObservePlayerUnits(entities, clan.CampaignGuid);
    }

    // A player unit without a profile (the Toolkit's dummy from SpawnUnitRequested) throws a
    // NullReferenceException inside the game's UnitEntity.ProfileKey (UnitProfileReader.GetProfileKey). It reaches managed code
    // as an Il2CppException (catching NullReferenceException alone missed it); such a unit is no Frieren.
    private static string ProfileKeyOf(UnitEntity unit)
    {
        try { return unit.ProfileKey; }
        catch (Exception ex) when (ex is Il2CppInterop.Runtime.Il2CppException || ex is NullReferenceException) { return null; }
    }

    private static void ObservePlayerUnits(
        Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, IEntity> entities,
        Il2CppSystem.Guid campaignGuid)
    {
        if (entities == null) return;
        foreach (var key in entities.Keys)
        {
            var unit = entities[key]?.TryCast<UnitEntity>();
            if (unit != null && unit.Faction == FactionType.Player && ProfileKeyOf(unit) == FrierenIds.ProfileKey)
            {
                Mark(campaignGuid);
                break;
            }
        }
    }
}

[HarmonyPatch(typeof(RecruitHelper), nameof(RecruitHelper.CreateRecruitCandidate),
    new[] { typeof(ClanRankTableData), typeof(Il2CppSystem.Collections.Generic.HashSet<string>),
        typeof(Il2CppSystem.Collections.Generic.HashSet<string>) })]
internal static class FrierenHistoryCandidatePatch
{
    private static void Prefix(RecruitHelper __instance,
        Il2CppSystem.Collections.Generic.HashSet<string> alreadyExistProfiles)
    {
        if (alreadyExistProfiles != null && __instance?._clanDataContainer != null
            && FrierenCampaignHistory.Excluded(__instance._clanDataContainer.CampaignGuid))
            alreadyExistProfiles.Add(FrierenIds.ProfileKey);
    }
}

[HarmonyPatch(typeof(EntityEventHandler), nameof(EntityEventHandler.OnEvent),
    new[] { typeof(SpawnUnitRequested) })]
internal static class FrierenHistorySpawnPatch
{
    private static void Postfix(EntityEventHandler __instance, SpawnUnitRequested msg)
    {
        if (msg?.Faction == FactionType.Player) FrierenCampaignHistory.ObservePlayerUnits(__instance);
    }
}
