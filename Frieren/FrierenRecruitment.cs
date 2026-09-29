using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace FrierenPortrait;

internal static class FrierenRecruitment
{
    internal const string BookKey = "ITEM_TechBook";
    internal static int BookCost(int gold) => FrierenRecruitmentRules.BookCost(gold);

    private sealed class OriginalPriceIcon
    {
        internal Image Image;
        internal Sprite Sprite;
    }

    private static readonly Dictionary<IntPtr, OriginalPriceIcon> OriginalPriceIcons = new();
    private static Sprite nativeBookIcon;
    private static bool loggedMissingIcon;

    internal static void ResetLifecycle()
    {
        foreach (var original in OriginalPriceIcons.Values)
        {
            if (!original?.Image || !original.Sprite) continue;
            try { original.Image.sprite = original.Sprite; }
            catch { }
        }
        OriginalPriceIcons.Clear();
        nativeBookIcon = null;
        loggedMissingIcon = false;
    }

    internal static void ShowPrice(TMP_Text priceText, RecruitCandidateData candidate, bool compact)
    {
        if (!priceText) return;
        if (candidate?.UnitProfileKey != FrierenIds.ProfileKey)
        {
            RestorePriceIcon(priceText);
            return;
        }
        var amount = BookCost(candidate.RecruitPrice);
        // The icon already identifies the resource in compact cards. The
        // detail panel uses the game's own localized item name, avoiding
        // language-specific plural rules.
        var bookName = TextKeyExtensions.GetName(BookKey);
        priceText.text = compact ? $"× {amount}" : $"{bookName} × {amount}";
        var icon = FindPriceIcon(priceText);
        if (!icon)
        {
            if (!loggedMissingIcon)
            {
                loggedMissingIcon = true;
                DelversHost.Warning("FRIEREN_BOOK_ICON_NOT_FOUND: Preistext zeigt Technikbücher, aber das native Goldsymbol konnte nicht zugeordnet werden.");
            }
            return;
        }
        // Il2Cpp pointers of destroyed UI objects can be reused; only trust an
        // entry that still refers to this live Image.
        if (!OriginalPriceIcons.TryGetValue(icon.Pointer, out var known) || !known?.Image || known.Image != icon)
        {
            if (OriginalPriceIcons.Count >= 64) PruneDeadPriceIcons();
            OriginalPriceIcons[icon.Pointer] = new OriginalPriceIcon { Image = icon, Sprite = icon.sprite };
        }
        var book = nativeBookIcon;
        if (!book)
        {
            book = new ResourceLoader().LoadItemIcon(BookKey);
            if (book) nativeBookIcon = book;
        }
        if (book) icon.sprite = book;
    }

    private static void PruneDeadPriceIcons()
    {
        var dead = new List<IntPtr>();
        foreach (var pair in OriginalPriceIcons)
            if (!pair.Value?.Image) dead.Add(pair.Key);
        foreach (var key in dead) OriginalPriceIcons.Remove(key);
    }

    private static Image FindPriceIcon(TMP_Text priceText)
    {
        var parent = priceText.transform.parent;
        for (var depth = 0; depth < 2 && parent; depth++, parent = parent.parent)
        {
            var images = parent.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (!image || !image.sprite) continue;
                var name = (image.name + " " + image.sprite.name).ToLowerInvariant();
                if (name.Contains("gold") || name.Contains("coin") || name.Contains("money")
                    || name.Contains("currency") || name.Contains("priceicon")
                    || image.sprite.name == "ITEM_TechBook") return image;
            }
        }
        return null;
    }

    private static void RestorePriceIcon(TMP_Text priceText)
    {
        var icon = FindPriceIcon(priceText);
        if (icon && OriginalPriceIcons.TryGetValue(icon.Pointer, out var original) && original?.Sprite
            && original.Image == icon)
            icon.sprite = original.Sprite;
    }

    internal sealed class Transaction
    {
        internal RecruitCandidateData Candidate;
        internal int OriginalGoldPrice;
        internal int BookCount;
        internal int PopulationBefore;
        internal int CandidatesBefore;
        internal MapType Map;
    }

    internal static bool Begin(EntityEventHandler handler, CandidateRecruitRequested request, out Transaction state)
    {
        state = null;
        var clan = handler?._clanDataContainer;
        var candidates = clan?.AvailableRecruitCandidates;
        if (request == null || candidates == null || request.Index < 0 || request.Index >= candidates.Count) return true;
        var candidate = candidates[request.Index];
        if (candidate?.UnitProfileKey != FrierenIds.ProfileKey) return true;
        var books = BookCost(candidate.RecruitPrice);
        if (!handler.HasResearchUnlockItem(BookKey, books, request.Map))
        {
            handler.NoticeNotEnoughItem();
            DelversHost.Info($"FRIEREN_RECRUIT_BLOCKED booksNeeded={books}");
            return false;
        }
        state = new Transaction
        {
            Candidate = candidate,
            OriginalGoldPrice = candidate.RecruitPrice,
            BookCount = books,
            PopulationBefore = clan.CurrentPopulation,
            CandidatesBefore = candidates.Count,
            Map = request.Map
        };
        // The native flow checks and deducts this field as gold. Restore it in
        // Finalizer; the book cost is charged only after native recruitment succeeds.
        candidate.RecruitPrice = 0;
        return true;
    }

    internal static void Complete(EntityEventHandler handler, Transaction state)
    {
        if (state == null) return;
        try { CompleteCore(handler, state); }
        catch (Exception ex)
        {
            // Native recruitment has already finished at this point. A mod
            // failure must not be rethrown into the native event dispatcher.
            DelversHost.Error("FRIEREN_RECRUIT_COMPLETE_FAILED: " + ex);
        }
    }

    private static void CompleteCore(EntityEventHandler handler, Transaction state)
    {
        var clan = handler?._clanDataContainer;
        var list = clan?.AvailableRecruitCandidates;
        var stillListed = false;
        if (list != null)
            for (var i = 0; i < list.Count; i++)
                if (list[i]?.Pointer == state.Candidate.Pointer) { stillListed = true; break; }
        var recruited = clan != null && (clan.CurrentPopulation > state.PopulationBefore
            || (list != null && list.Count < state.CandidatesBefore && !stillListed));
        if (!recruited) return;
        // Mark first: Frieren is recruited either way and must stay unique.
        FrierenCampaignHistory.Mark(clan.CampaignGuid);
        if (!handler.HasResearchUnlockItem(BookKey, state.BookCount, state.Map))
        {
            // Books changed between the check in Begin and native success.
            // Do not throw into the native dispatcher; report the unpaid cost.
            DelversHost.Error($"FRIEREN_RECRUIT_BOOKS_MISSING booksNeeded={state.BookCount} "
                + "Frieren wurde rekrutiert, die Technikbücher konnten aber nicht abgezogen werden.");
            return;
        }
        handler.OnEvent(new RemoveFromPlayerOwnedItemRequested(state.Map, BookKey, state.BookCount));
        DelversHost.Info($"FRIEREN_RECRUITED gold=0 books={state.BookCount} originalGoldPrice={state.OriginalGoldPrice}");
    }

    internal static void Restore(Transaction state)
    {
        if (state?.Candidate != null) state.Candidate.RecruitPrice = state.OriginalGoldPrice;
    }
}

[HarmonyPatch(typeof(SubUI_CandidateCard), nameof(SubUI_CandidateCard.SetCard))]
internal static class FrierenCardPricePatch
{
    private static void Postfix(SubUI_CandidateCard __instance, RecruitCandidateData __0)
        => FrierenRecruitment.ShowPrice(__instance._recruitPrice, __0, true);
}

[HarmonyPatch(typeof(SubUI_CandidateDetailPanel), nameof(SubUI_CandidateDetailPanel.SetSelectedCandidate))]
internal static class FrierenDetailPricePatch
{
    private static void Postfix(SubUI_CandidateDetailPanel __instance, int __0, RecruitCandidateData __1)
        => FrierenRecruitment.ShowPrice(__instance._recruitPrice, __1, false);
}

[HarmonyPatch(typeof(EntityEventHandler), nameof(EntityEventHandler.OnEvent),
    new[] { typeof(CandidateRecruitRequested) })]
internal static class FrierenRecruitTransactionPatch
{
    private static bool Prefix(EntityEventHandler __instance, CandidateRecruitRequested __0,
        out FrierenRecruitment.Transaction __state)
        => FrierenRecruitment.Begin(__instance, __0, out __state);

    private static void Postfix(EntityEventHandler __instance, FrierenRecruitment.Transaction __state)
        => FrierenRecruitment.Complete(__instance, __state);

    private static Exception Finalizer(Exception __exception, FrierenRecruitment.Transaction __state)
    {
        FrierenRecruitment.Restore(__state);
        if (__exception != null)
            DelversHost.Error("FRIEREN_RECRUIT_ERROR: " + __exception);
        return __exception;
    }
}
