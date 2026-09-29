using Il2CppInterop.Runtime;

namespace FrierenPortrait;

// One patch owner per native deserialization boundary keeps migration order
// explicit and makes each optional compatibility step fail independently.
internal static class FrierenSaveMigrations
{
    // Core restores Frieren's fixed traits (race, archmage, Booklover) as the
    // first prefix of the same method; see FrierenFixedTraitRules.
    internal static void BeforeComponentsDeserialize(SavedComponents saved, IEntity entity)
    {
        try
        {
            var removed = FrierenLegacyTraitMigration.RemoveLegacyMage(saved);
            if (removed > 0)
                DelversHost.Info("FRIEREN_LEGACY_TRAIT_MIGRATED"
                    + " entity=" + DescribeEntity(entity)
                    + " profile=" + FrierenIds.ProfileKey
                    + " removedMage=" + removed
                    + " background=" + FrierenGameplay.TraitKey);
        }
        catch (Exception ex)
        {
            DelversHost.Error("FRIEREN_LEGACY_TRAIT_MIGRATION_FAILED"
                + " entity=" + DescribeEntity(entity)
                + " saveWillBeBlocked=true error=" + ex);
            throw;
        }
    }

    internal static void BeforeUnitProfileDeserialize(ComponentSaveBaseData data, IEntity entity)
    {
        try
        {
            var saved = data?.TryCast<UnitProfileComponentSaveData>();
            if (FrierenVisualMigration.Normalize(saved))
                DelversHost.Info(
                    $"FRIEREN_VISUAL_MIGRATED profile={FrierenIds.ProfileKey} eyes=Green hair=lightenedElf09");
            if (FrierenBiographyMigration.Normalize(saved))
                DelversHost.Info(
                    $"FRIEREN_BIOGRAPHY_MIGRATED profile={FrierenIds.ProfileKey} to={FrierenLocalization.BiographyKey}");
        }
        catch (Exception ex)
        {
            DelversHost.Error("FRIEREN_PROFILE_MIGRATION_FAILED"
                + " entity=" + DescribeEntity(entity) + " error=" + ex);
            throw;
        }
    }

    internal static void AfterUnitProfileDeserialize(UnitProfileComponent profile)
    {
        try
        {
            if (!FrierenNameMigration.Normalize(profile, out _)) return;
            // The old key is never logged: players share logs, and it carries a personal name.
            DelversHost.Info(
                $"FRIEREN_NAME_MIGRATED profile={FrierenIds.ProfileKey} from=legacy-name-key to={FrierenGameplay.NameKey}");
        }
        catch (Exception ex)
        {
            DelversHost.Error("FRIEREN_NAME_MIGRATION_FAILED unit: " + ex);
            throw;
        }
    }

    internal static void AfterClanDeserialize(ClanDataContainer clan)
    {
        try
        {
            var changed = BookloverMigration.EnsureCandidates(clan?.AvailableRecruitCandidates);
            if (changed > 0)
                DelversHost.Info("BOOKLOVER_SAVED_CANDIDATES_MIGRATED count=" + changed);
        }
        catch (Exception ex)
        {
            DelversHost.Error("BOOKLOVER_SAVED_CANDIDATES_MIGRATION_FAILED: " + ex);
            throw;
        }

        try
        {
            var changed = FrierenNameMigration.Normalize(clan?.AvailableRecruitCandidates);
            if (changed > 0)
                DelversHost.Info(
                    $"FRIEREN_RECRUIT_NAMES_MIGRATED count={changed} to={FrierenGameplay.NameKey}");
        }
        catch (Exception ex)
        {
            DelversHost.Error("FRIEREN_NAME_MIGRATION_FAILED recruits: " + ex);
            throw;
        }

        try
        {
            var changed = FrierenBiographyMigration.Normalize(clan?.AvailableRecruitCandidates);
            if (changed > 0)
                DelversHost.Info(
                    $"FRIEREN_RECRUIT_BIOGRAPHIES_MIGRATED count={changed} to={FrierenLocalization.BiographyKey}");
        }
        catch (Exception ex)
        {
            DelversHost.Error("FRIEREN_BIOGRAPHY_MIGRATION_FAILED recruits: " + ex);
            throw;
        }
    }

    // Only the clan migrations rewrite saved data, so only their failure is
    // reported to Core (which then blocks saving). Magic skills and campaign
    // history are runtime fix-ups that retry or log on their own; each step
    // runs even when an earlier one failed.
    internal static void AfterCampaignLoaded(CampaignDataContainer campaign)
    {
        if (campaign == null) throw new ArgumentNullException(nameof(campaign));

        Exception migrationFailure = null;
        try { AfterClanDeserialize(campaign.ClanDataContainer); }
        catch (Exception ex) { migrationFailure = ex; }

        FrierenMagicSkills.ReconcileLoadedCampaign(campaign);

        try { FrierenCampaignHistory.ObservePlayerUnits(campaign); }
        catch (Exception ex) { DelversHost.Warning("FRIEREN_CAMPAIGN_HISTORY_FAILED: " + ex.Message); }

        if (migrationFailure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(migrationFailure).Throw();
    }

    private static string DescribeEntity(IEntity entity)
    {
        if (entity == null) return "unknown";
        try { return entity.Guid.ToString(); }
        catch { return "unavailable"; }
    }
}
