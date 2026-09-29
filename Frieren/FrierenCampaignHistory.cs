using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace FrierenPortrait;

// The native candidate generator excludes profiles only while a matching unit
// still exists. A campaign marker also remembers Frieren after her death.
internal static class FrierenCampaignHistory
{
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
        var entities = helper?._entityContainer?.GetAllEntitiesOnCampaign();
        var clan = helper?._clanDataContainer;
        if (entities == null || clan == null) return;
        foreach (var key in entities.Keys)
        {
            var unit = entities[key]?.TryCast<UnitEntity>();
            if (unit != null && unit.Faction == FactionType.Player && unit.ProfileKey == FrierenIds.ProfileKey)
            {
                Mark(clan.CampaignGuid);
                break;
            }
        }
        if (Seen(clan.CampaignGuid))
            helper._existProfiles?.Add(FrierenIds.ProfileKey);
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

    private static void ObservePlayerUnits(
        Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, IEntity> entities,
        Il2CppSystem.Guid campaignGuid)
    {
        if (entities == null) return;
        foreach (var key in entities.Keys)
        {
            var unit = entities[key]?.TryCast<UnitEntity>();
            if (unit != null && unit.Faction == FactionType.Player && unit.ProfileKey == FrierenIds.ProfileKey)
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
        Il2CppSystem.Collections.Generic.HashSet<string> __1)
    {
        if (__1 != null && __instance?._clanDataContainer != null
            && FrierenCampaignHistory.Seen(__instance._clanDataContainer.CampaignGuid))
            __1.Add(FrierenIds.ProfileKey);
    }
}

[HarmonyPatch(typeof(EntityEventHandler), nameof(EntityEventHandler.OnEvent),
    new[] { typeof(SpawnUnitRequested) })]
internal static class FrierenHistorySpawnPatch
{
    private static void Postfix(EntityEventHandler __instance, SpawnUnitRequested __0)
    {
        if (__0?.Faction == FactionType.Player) FrierenCampaignHistory.ObservePlayerUnits(__instance);
    }
}
