using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace FrierenPortrait;

internal static class FrierenMagicSkills
{
    internal static readonly string[] Granted =
        { "SKILL_FireMissile", "SKILL_SummonBubbling", "SKILL_StrangeSeed" };

    private sealed class PendingRetry
    {
        internal IEntity Entity;
        internal PlayerUnitDataContainer Container;
        internal bool TraitAlreadyScheduled;
        internal float Deadline;
        internal object Coroutine;
    }

    private static readonly Dictionary<string, PendingRetry> PendingPersistence =
        new(StringComparer.Ordinal);
    private static PlayerUnitDataContainer playerUnits;

    internal static void Remember(PlayerUnitDataContainer container)
    {
        if (container != null) playerUnits = container;
    }

    // A failed runtime reconcile leaves the saved extra-skill list unchanged,
    // so it must not block saving: a unit that is not ready yet gets the
    // bounded retry, and the next spawn/equipment/reset hook reconciles again.
    internal static void ReconcileLoadedCampaign(CampaignDataContainer campaign)
    {
        if (campaign == null) throw new ArgumentNullException(nameof(campaign));

        var container = campaign.PlayerUnitDataContainer;
        Remember(container);
        var entities = campaign.EntityDataContainer?.GetAllEntitiesOnCampaign();
        if (entities == null) return;

        var reconciled = 0;
        var incomplete = 0;
        foreach (var key in entities.Keys)
        {
            var unit = entities[key]?.TryCast<UnitEntity>();
            if (unit == null || unit.Faction != FactionType.Player
                || unit.ProfileKey != FrierenIds.ProfileKey) continue;

            try
            {
                if (Reconcile(unit.Cast<IEntity>(), container)) reconciled++;
                else incomplete++;
            }
            catch (Exception ex)
            {
                incomplete++;
                DelversHost.Error("FRIEREN_MAGIC_SKILL_RECONCILE_FAILED context=campaign-load: " + ex);
            }
        }

        if (reconciled > 0 || incomplete > 0)
            DelversHost.Info("FRIEREN_MAGIC_SKILLS_CAMPAIGN_RECONCILED count=" + reconciled
                + " incomplete=" + incomplete);
    }

    internal static void TryReconcile(IEntity entity, PlayerUnitDataContainer container = null,
        bool traitAlreadyScheduled = false, string context = "unknown")
    {
        try { Reconcile(entity, container, traitAlreadyScheduled); }
        catch (Exception ex)
        {
            // Lifecycle hooks run after native recruitment, loading, equipment or
            // reset work. A mod failure must never turn that completed native
            // operation into a game failure.
            DelversHost.Error(
                $"FRIEREN_MAGIC_SKILL_RECONCILE_FAILED context={context}: {ex}");
        }
    }

    internal static bool Reconcile(IEntity entity, PlayerUnitDataContainer container = null,
        bool traitAlreadyScheduled = false, bool allowRetry = true)
    {
        if (entity == null || (!traitAlreadyScheduled && !FrierenGameplay.HasArchmageTrait(entity))) return true;
        container ??= playerUnits;
        Remember(container);

        var skills = EntityComponent._instance?.GetComponentOf<SkillComponent>(entity);
        var tree = container?.SkillViewer?.GetData(entity.Guid);
        var fullyRestored = skills != null && tree != null;

        foreach (var key in Granted)
        {
            if (tree != null)
            {
                try
                {
                    if (!TryContains(tree.GetExtraSkills(), key, out var isSaved))
                        fullyRestored = false;
                    else if (!isSaved)
                    {
                        container.OnAddExtraSkill(entity.Guid, key);
                        tree = container.SkillViewer?.GetData(entity.Guid);
                        if (tree == null || !TryContains(tree.GetExtraSkills(), key, out var wasSaved) || !wasSaved)
                            fullyRestored = false;
                    }
                }
                catch (Exception ex)
                {
                    fullyRestored = false;
                    DelversHost.Error(
                        $"FRIEREN_MAGIC_SKILL_SAVE_FAILED skill={key}: {ex}");
                }
            }

            if (skills != null)
            {
                try
                {
                    if (!skills.HasSkill(key)) skills.AddSkill(key);
                }
                catch (Exception ex)
                {
                    fullyRestored = false;
                    DelversHost.Error(
                        $"FRIEREN_MAGIC_SKILL_RUNTIME_FAILED skill={key}: {ex}");
                }
            }
        }

        var id = entity.Guid.ToString();
        if (fullyRestored)
        {
            if (allowRetry) CancelPending(id);
            else PendingPersistence.Remove(id);
        }
        else if (allowRetry) QueuePersistenceRetry(entity, container, traitAlreadyScheduled);
        return fullyRestored;
    }

    private static void CancelPending(string id)
    {
        if (!PendingPersistence.TryGetValue(id, out var pending)) return;
        PendingPersistence.Remove(id);
        if (pending.Coroutine == null) return;
        try { DelversHost.StopCoroutine(pending.Coroutine); }
        catch (Exception ex)
        {
            DelversHost.Warning("FRIEREN_MAGIC_SKILL_RETRY_STOP_FAILED: " + ex.Message);
        }
    }

    internal static void ResetLifecycle()
    {
        var pending = PendingPersistence.Values.ToArray();
        PendingPersistence.Clear();
        playerUnits = null;
        foreach (var retry in pending)
        {
            if (retry?.Coroutine == null) continue;
            try { DelversHost.StopCoroutine(retry.Coroutine); }
            catch (Exception ex)
            {
                DelversHost.Warning("FRIEREN_MAGIC_SKILL_RETRY_STOP_FAILED: " + ex.Message);
            }
        }
    }

    private static void QueuePersistenceRetry(IEntity entity, PlayerUnitDataContainer container,
        bool traitAlreadyScheduled)
    {
        var id = entity.Guid.ToString();
        if (PendingPersistence.TryGetValue(id, out var pending))
        {
            // A scene/campaign transition can replace the Il2Cpp wrapper while
            // keeping the unit Guid. Let the existing coroutine follow the new
            // entity and data container instead of blocking its recovery.
            pending.Entity = entity;
            if (container != null) pending.Container = container;
            pending.TraitAlreadyScheduled |= traitAlreadyScheduled;
            pending.Deadline = Time.realtimeSinceStartup + 5f;
            return;
        }
        pending = new PendingRetry
        {
            Entity = entity,
            Container = container,
            TraitAlreadyScheduled = traitAlreadyScheduled,
            Deadline = Time.realtimeSinceStartup + 5f
        };
        PendingPersistence.Add(id, pending);
        try { pending.Coroutine = DelversHost.StartCoroutine(RetryPersistence(id, pending)); }
        catch
        {
            PendingPersistence.Remove(id);
            throw;
        }
    }

    private static System.Collections.IEnumerator RetryPersistence(string id, PendingRetry pending)
    {
        var restored = false;
        try
        {
            while (Time.realtimeSinceStartup < pending.Deadline)
            {
                yield return null;
                if (!PendingPersistence.TryGetValue(id, out var current)
                    || !ReferenceEquals(current, pending)) break;
                try
                {
                    if (pending.Entity == null) break;
                    restored = Reconcile(pending.Entity, pending.Container ?? playerUnits,
                        pending.TraitAlreadyScheduled, false);
                    if (restored) break;
                }
                catch
                {
                    // Scene changes can invalidate an Il2Cpp wrapper between
                    // frames. Keep this retry bounded and let the next regular
                    // load/spawn hook restore the skills on the replacement.
                }
            }
        }
        finally
        {
            var ownsPending = PendingPersistence.TryGetValue(id, out var current)
                && ReferenceEquals(current, pending);
            if (ownsPending) PendingPersistence.Remove(id);
            if (!restored && ownsPending)
                DelversHost.Warning(
                    $"FRIEREN_MAGIC_SKILL_SAVE_RETRY_EXPIRED unit={id}; retrying on the next spawn/load/equipment event.");
        }
    }

    private static bool TryContains(Il2CppSystem.Collections.Generic.IReadOnlyList<string> values,
        string key, out bool contains)
    {
        contains = false;
        if (values == null) return false;
        Il2CppSystem.Collections.Generic.IReadOnlyCollection<string> collection;
        try { collection = values.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<string>>(); }
        catch { return false; }
        if (collection == null) return false;
        int count;
        try { count = Math.Max(0, collection.Count); }
        catch { return false; }
        for (var i = 0; i < count; i++)
        {
            try
            {
                if (values[i] != key) continue;
                contains = true;
                return true;
            }
            catch { return false; }
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerUnitDataContainer), nameof(PlayerUnitDataContainer.OnPlayerUnitSpawned))]
internal static class FrierenRememberSpawnedPlayerDataPatch
{
    private static void Postfix(PlayerUnitDataContainer __instance)
        => FrierenMagicSkills.Remember(__instance);
}

[HarmonyPatch(typeof(UnitSpawner), nameof(UnitSpawner.SpawnPlayableUnit),
    new[] { typeof(MapType), typeof(RecruitCandidateData), typeof(Vector2Int), typeof(FactionType) })]
internal static class FrierenSpawnMagicSkillsPatch
{
    private static void Postfix(UnitSpawner __instance, RecruitCandidateData __1, UnitEntity __result)
    {
        var isFrieren = __1?.UnitProfileKey == FrierenIds.ProfileKey
            && __1.BackgroundTrait == FrierenGameplay.TraitKey;
        if (isFrieren && __result != null)
            FrierenMagicSkills.TryReconcile(__result.Cast<IEntity>(), __instance._playerUnitDataContainer,
                true, "spawn");
    }
}

// Required after the native equipment skill rebuild: Frieren's three granted
// spells are extra skills rather than item defaults. Reconcile is filtered by
// the Elfische Erzmagierin affecter and touches only this unit's runtime skills
// plus saved extra-skill list; it does not alter items, equipment, or Hotbar state.
[HarmonyPatch(typeof(ItemControlProcessor), "ProcessUnequip",
    new[] { typeof(MapType), typeof(UnitEntity), typeof(int), typeof(string),
        typeof(Il2CppSystem.Collections.Generic.Dictionary<StatType, float>) })]
internal static class FrierenUnequipMagicSkillsPatch
{
    private static void Postfix(ItemControlProcessor __instance, UnitEntity __1)
    {
        if (__1 != null)
            FrierenMagicSkills.TryReconcile(__1.Cast<IEntity>(), __instance.playerUnitDataContainer,
                context: "unequip");
    }
}

[HarmonyPatch(typeof(EntityEventHandler), nameof(EntityEventHandler.OnEvent),
    new[] { typeof(SkillResetRequested) })]
internal static class FrierenResetMagicSkillsPatch
{
    private static void Postfix(EntityEventHandler __instance, SkillResetRequested __0)
    {
        // This is the end of the native reset transaction: learned/runtime
        // skills were reset and equipped defaults were restored. Extra-skill
        // registrations survive, so restoring here makes their active skills
        // available again without duplicating the saved entries.
        if (__0?.Unit != null)
            FrierenMagicSkills.TryReconcile(__0.Unit, __instance.playerUnitDataContainer,
                context: "skill-reset");
    }
}
