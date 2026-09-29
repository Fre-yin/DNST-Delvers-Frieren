using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace FrierenPortrait;

// Keep the game's inventory action, cast, and single-item consumption. The
// Booklover reward runs only after that native consumption has succeeded.
internal static class BookloverItemUse
{
    internal const string BookKey = FrierenBookloverRules.BookKey;
    internal const string NativeReadSkill = "SKILL_DevelopersLetter";
    // Authorization is created only by the native successful consumption apply.
    // Manual drop/store events use the same later removal event, so they must
    // never be able to create this token themselves.
    private const float ConsumptionAuthorizationLifetimeSeconds = 10f;
    // The native reading success yields its own RemoveCarryItemApplyData instance.
    // Manual drops create a different instance of the same type (also amount 1 for a
    // single book), so only that exact instance may authorize a reward. Holding the
    // wrapper keeps the native object alive, so its address cannot be reused meanwhile.
    private const float ReadApplyMarkLifetimeSeconds = 120f;
    private static readonly Dictionary<IntPtr, (RemoveCarryItemApplyData Data, float Expires)>
        MarkedReadApplies = new();
    private static readonly Dictionary<string, (string ItemGuid, int BooksBefore, float Expires)>
        AuthorizedConsumptions = new();
    private static MainFlow cachedMainFlow;

    internal static void EnableInventoryUse(ItemSheet sheet)
    {
        var book = sheet?.GetData(BookKey);
        if (book == null) return;
        book.IsUsable = true;
        book.UseSkill = NativeReadSkill;
    }

    internal static void MarkReadApply(IExecutionData current, string usedItemKey)
    {
        // Every item a Booklover uses (food, potions, books) reaches this iterator.
        if (!FrierenBookloverRules.IsPossibleBookRead(usedItemKey)) return;
        var data = current?.TryCast<CustomExecutionData>()?.Data?.TryCast<RemoveCarryItemApplyData>();
        if (data?.Owner == null || data.Amount != 1 || !Booklover.HasTrait(data.Owner)) return;
        PruneExpiredReadMarks();
        MarkedReadApplies[data.Pointer] = (data, Time.realtimeSinceStartup + ReadApplyMarkLifetimeSeconds);
        DelversHost.Info("BOOKLOVER_READ_APPLY_MARKED item=" + (usedItemKey ?? "unknown"));
    }

    internal static void OnNativeApply(IApplyData data)
    {
        // Hot path: every native apply passes here.
        if (MarkedReadApplies.Count == 0 || data == null) return;
        if (!MarkedReadApplies.Remove(data.Pointer, out var marked))
        {
            PruneExpiredReadMarks();
            return;
        }
        if (Time.realtimeSinceStartup > marked.Expires) return;
        if (AuthorizeConsumption(marked.Data))
            DelversHost.Info("BOOKLOVER_CONSUMPTION_AUTHORIZED");
        else
            DelversHost.Warning("BOOKLOVER_CONSUMPTION_NOT_AUTHORIZED carried stack is not a technique book");
    }

    private static void PruneExpiredReadMarks()
    {
        if (MarkedReadApplies.Count == 0) return;
        var now = Time.realtimeSinceStartup;
        foreach (var key in MarkedReadApplies.Where(entry => now > entry.Value.Expires)
                     .Select(entry => entry.Key).ToArray())
            MarkedReadApplies.Remove(key);
    }

    private static bool AuthorizeConsumption(RemoveCarryItemApplyData data)
    {
        var owner = data?.Owner;
        if (owner == null || data.Amount != 1 || !Booklover.HasTrait(owner)) return false;
        var carried = EntityComponent._instance?.GetComponentOf<InventoryComponent>(owner)?.GetTempSlot();
        if (carried == null || carried.IsEmpty() || carried.Key != BookKey
            || carried.Amount < 1) return false;
        AuthorizedConsumptions[owner.Guid.ToString()] =
            (carried.Guid.ToString(), carried.Amount,
                Time.realtimeSinceStartup + ConsumptionAuthorizationLifetimeSeconds);
        return true;
    }

    internal static bool TryConsumeAuthorization(IEntity owner, ItemHolder carried)
    {
        if (owner == null || carried == null) return false;
        var id = owner.Guid.ToString();
        if (!AuthorizedConsumptions.TryGetValue(id, out var pending)) return false;
        if (Time.realtimeSinceStartup > pending.Expires)
        {
            AuthorizedConsumptions.Remove(id);
            DelversHost.Info("BOOKLOVER_CONSUMPTION_AUTH_EXPIRED");
            return false;
        }
        if (pending.ItemGuid != carried.Guid.ToString()
            || pending.BooksBefore != carried.Amount) return false;
        AuthorizedConsumptions.Remove(id);
        return true;
    }

    internal static void ClearConsumptionAuthorizations()
    {
        MarkedReadApplies.Clear();
        AuthorizedConsumptions.Clear();
        cachedMainFlow = null;
    }

    internal static void HideUnavailableInventoryUse(IEntity owner, ItemTableData item, InventoryActions actions)
    {
        if (item?.Key != BookKey || actions == null || Booklover.HasTrait(owner)) return;
        for (var i = actions.Count - 1; i >= 0; i--)
            if (actions[i].InteractionInputType == UIInputType.InteractionOption_UseItem)
                actions.RemoveAt(i);
    }

    internal static void Award(IEntity owner, MapType map)
    {
        var status = EntityComponent._instance?.GetComponentOf<StatusComponent>(owner);
        if (!cachedMainFlow) cachedMainFlow = UnityEngine.Object.FindObjectOfType<MainFlow>();
        var flow = cachedMainFlow;
        var handler = flow?._tickFlowHandlers != null && flow._tickFlowHandlers.ContainsKey(map)
            ? flow._tickFlowHandlers[map]?.TryCast<TickFlowHandler>() : null;
        if (status == null || handler?._forceQueue == null
            || DataSheetManager.Instance?._affecter?.GetData(Booklover.MoodKey) == null)
            throw new InvalidOperationException("Booklover-Effekt konnte nach Buchverbrauch nicht vorbereitet werden.");

        // AddProgressionValue updates both the live stat and its save record.
        // Applying progression again would double-count existing bonuses.
        status.AddProgressionValue(StatType.MainSkillPoint, 2);
        status.AddProgressionValue(StatType.SubSkillPoint, 2);
        var mood = new AffecterApplyData(owner, Booklover.MoodKey, 1, Booklover.MoodDurationSeconds, false, owner);
        var request = new DataApplyRequested(mood.Cast<IApplyData>()).Cast<IEventData>();
        handler.EnqueueEvent(request, true);
        DelversHost.Info("BOOKLOVER_READ_SUCCESS book=ITEM_TechBook points=2+2 mood=4 duration=1200");
    }
}

[HarmonyPatch(typeof(ItemSheet), nameof(ItemSheet.Parse))]
internal static class BookloverBookTablePatch
{
    private static void Postfix(ItemSheet __instance) => BookloverItemUse.EnableInventoryUse(__instance);
}

[HarmonyPatch(typeof(ItemUseCondition), "CheckCondition")]
internal static class BookloverReadingGatePatch
{
    private static bool Prefix(SkillExecutionContext context, ref ExecutionCheckResult __result)
    {
        if (context?.UseItem?.Key != BookloverItemUse.BookKey || Booklover.HasTrait(context.User)) return true;
        __result = ExecutionCheckResult.Failed_NotHasSkill;
        return false;
    }
}

[HarmonyPatch(typeof(ActiveSkill_Base), nameof(ActiveSkill_Base.CheckForUseItem),
    new[] { typeof(IInMapDataSupplier), typeof(SkillExecutionContext), typeof(bool) })]
internal static class BookloverEarlyReadingGatePatch
{
    private static bool Prefix(SkillExecutionContext context, ref ExecutionCheckResult __result)
    {
        if (context?.UseItem?.Key != BookloverItemUse.BookKey || Booklover.HasTrait(context.User)) return true;
        __result = ExecutionCheckResult.Failed_NotHasSkill;
        return false;
    }
}

[HarmonyPatch(typeof(InteractionFactory), nameof(InteractionFactory.CreateInventoryInteraction),
    new[] { typeof(IEntity), typeof(ItemTableData), typeof(int), typeof(int) })]
internal static class BookloverInventoryUseBySelectionPatch
{
    private static void Postfix(IEntity ownerEntity, ItemTableData tableData, InventoryActions __result)
        => BookloverItemUse.HideUnavailableInventoryUse(ownerEntity, tableData, __result);
}

[HarmonyPatch]
internal static class BookloverInventoryUseByOwnerPatch
{
    private static System.Reflection.MethodBase TargetMethod()
        => typeof(InteractionFactory).GetMethods(System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .SingleOrDefault(method => method.Name == nameof(InteractionFactory.CreateInventoryInteraction)
                && method.GetParameters().Length == 4
                && method.GetParameters()[2].ParameterType.Name == "Guid");

    private static void Postfix(IEntity ownerEntity, ItemTableData tableData, InventoryActions __result)
        => BookloverItemUse.HideUnavailableInventoryUse(ownerEntity, tableData, __result);
}

// The native reading success (ItemUseCondition.OnSuccessCondition, first yield)
// wraps a fresh RemoveCarryItemApplyData(user, 1) in a CustomExecutionData.
// The iterator may run before the reading finishes, so this only marks the instance.
[HarmonyPatch(typeof(ItemUseCondition._OnSuccessCondition_d__2),
    nameof(ItemUseCondition._OnSuccessCondition_d__2.MoveNext))]
internal static class BookloverReadApplyMarkPatch
{
    private static void Postfix(ItemUseCondition._OnSuccessCondition_d__2 __instance, bool __result)
    {
        if (!__result) return;
        try
        {
            BookloverItemUse.MarkReadApply(__instance.__2__current, __instance.context?.UseItem?.Key);
        }
        catch (Exception ex)
        {
            DelversHost.Error("BOOKLOVER_READ_MARK_FAILED: " + ex);
        }
    }
}

// The private Apply(RemoveCarryItemApplyData) overload is inlined into this virtual
// dispatcher and never called natively (Cpp2IL CallerCount 0), so hook the dispatcher.
// Only a marked reading instance reaching it authorizes the following removal event.
[HarmonyPatch(typeof(MainEventApplier), nameof(MainEventApplier.Apply),
    new[] { typeof(IApplyData), typeof(Il2CppSystem.Collections.Generic.List<IApplyData>) })]
internal static class BookloverNativeConsumptionPatch
{
    private static void Prefix(IApplyData data)
    {
        try
        {
            BookloverItemUse.OnNativeApply(data);
        }
        catch (Exception ex)
        {
            DelversHost.Error("BOOKLOVER_CONSUMPTION_AUTH_FAILED: " + ex);
        }
    }
}

[HarmonyPatch(typeof(ItemEventHandler), nameof(ItemEventHandler.OnEvent),
    new[] { typeof(RemoveCarryItemRequested) })]
internal static class BookloverReadCompletedPatch
{
    private sealed class ReadState
    {
        internal IEntity Owner;
        internal MapType Map;
        internal InventoryComponent Inventory;
        internal int BooksBefore;
    }

    private static void Prefix(RemoveCarryItemRequested msg, out ReadState __state)
    {
        __state = null;
        try
        {
            if (msg?.Owner == null) return;
            var inventory = EntityComponent._instance?.GetComponentOf<InventoryComponent>(msg.Owner);
            if (inventory == null) return;
            var carried = inventory.GetTempSlot();
            // The native event's nullable Amount is reported as zero by the IL2CPP
            // wrapper on this build. Verify the actual one-book decrease instead.
            if (carried == null || !Booklover.HasTrait(msg.Owner)
                || carried.IsEmpty() || carried.Key != BookloverItemUse.BookKey || carried.Amount < 1
                || !BookloverItemUse.TryConsumeAuthorization(msg.Owner, carried)) return;
            __state = new ReadState
            {
                Owner = msg.Owner, Map = msg.Map, Inventory = inventory, BooksBefore = carried.Amount
            };
        }
        catch (Exception ex)
        {
            // Never let the reward bookkeeping break native item removal.
            __state = null;
            DelversHost.Error("BOOKLOVER_READ_PREPARE_FAILED: " + ex);
        }
    }

    private static void Postfix(ReadState __state)
    {
        if (__state == null) return;
        try
        {
            var carried = __state.Inventory.GetTempSlot();
            var remaining = carried == null || carried.IsEmpty() ? 0
                : carried.Key == BookloverItemUse.BookKey ? carried.Amount : -1;
            if (__state.BooksBefore - remaining != 1)
            {
                DelversHost.Warning($"BOOKLOVER_READ_NOT_CONSUMED before={__state.BooksBefore} after={remaining}");
                return;
            }
            BookloverItemUse.Award(__state.Owner, __state.Map);
        }
        catch (Exception ex)
        {
            DelversHost.Error("BOOKLOVER_READ_REWARD_FAILED: " + ex);
        }
    }
}
