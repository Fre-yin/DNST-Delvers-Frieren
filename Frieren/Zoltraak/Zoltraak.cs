using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace FrierenPortrait;

// Zoltraak, Frieren's own spell, first version (see ZOLTRAAK-RECHERCHE.md). A channeled magic
// line spell that borrows the class of the native SKILL_ClingingVines: the game maps every skill
// key to the class of exactly that name, so Skill_Base.Create is redirected for our key. The
// channel itself (ChannelSkills) replaces the template's behaviour. Frieren keeps the spell as a
// saved extra skill (FrierenMagicSkills.Reconcile), so a save holding it needs Frieren 0.5.0 or
// later; without the key the game cannot load the skill and Core blocks saving.
internal static partial class Zoltraak
{
    internal const string SkillKey = "SKILL_DelversZoltraak";
    internal const string TemplateKey = "SKILL_ClingingVines";
    internal const string IconFile = "Zoltraak_Icon.png";
    internal const string BreakIconFile = "Zoltraak_Bruch_Symbol_16x16.png";
    // The game reads TEXTKEY_<key>_NAME and _DESC from rows in FrierenLocalization, so no
    // hook on the hot text lookups is needed. Name and BreakName only fill the sheet's
    // developer comments.
    internal const string Name = "Zoltraak";
    internal const string NameTextKey = "TEXTKEY_" + SkillKey + "_NAME";
    internal const string DescriptionTextKey = "TEXTKEY_" + SkillKey + "_DESC";
    internal const string BreakNameTextKey = "TEXTKEY_" + BreakKey + "_NAME";
    internal const string BreakDescriptionTextKey = "TEXTKEY_" + BreakKey + "_DESC";

    // The eight images of the spell, next to Frieren's portraits (ZoltraakBeam, icons). The pointed
    // start and the fading end are cut from the beam segment, so they have no image of their own.
    internal static readonly string[] AssetFiles =
    {
        IconFile, BreakIconFile, "Zoltraak_Zauberkreis_32x32_5Bilder.png", "Zoltraak_Castimpuls_48x48_8Bilder.png",
        "Zoltraak_Strahlsegment_32x12_8Bilder.png", "Zoltraak_Endring_8x20_4Bilder.png",
        "Zoltraak_Lichtpunkt_17x17_4Bilder.png", "Zoltraak_Einschlag_64x64_4Bilder.png"
    };

    // 15 hits over 3 s, each hit one break stack of -1 physical and magic
    // resistance for 12 s, at most 30 stacks (two full channels).
    internal const int Ticks = 15;
    internal const float TickInterval = 0.2f;
    // Share of the caster's final magic attack power per hit, in percent (25 after the Toolkit's standard measurement, then 40). Fixed in a
    // release; a Core test campaign may change it for measuring (SetHitPercentForTest).
    internal const float DefaultHitPercent = 40f;
    // Share of each hit that goes surely to the head (20 %, final word
    // pending); the rest lands like any normal hit, which the game also sends to the head about a
    // third of the time.
    internal const float BodyShare = 0.8f;
    internal const float HeadShare = 0.2f;
    internal const string IndestructibleBuildingKey = "AFFECTER_IndestructibleBuilding";

    internal const string BreakKey = "AFFECTER_DelversZoltraakBreak";
    internal const string BreakTemplateKey = "AFFECTER_Frozen";   // data-only, negative, timed, stacking
    internal const string BreakName = "Zoltraak Break";
    internal const int BreakMaxStack = 30;
    internal const float BreakDuration = 12f;
    // Resistance points one break stack takes, physical and magic alike.
    internal const float BreakPerStack = 1f;
    // The skill row's FloatValues, read by the tooltip only (ZoltraakTextRules.TooltipValues).
    internal static readonly float[] TooltipValues = ZoltraakTextRules.TooltipValues(DefaultHitPercent, Ticks, TickInterval, BreakPerStack, BreakDuration, BreakMaxStack);
    internal const string StunKey = "AFFECTER_Stun";

    private static bool unavailableLogged;
    private static float? testHitPercent;
    private static string testHitPercentCampaign;

    // Per caster for the running channel: the share per hit and whether this is a test
    // campaign, decided once at its first tick.
    private sealed class ChannelState
    {
        internal float HitPercent;
        internal bool Measuring;
    }

    private static readonly Dictionary<string, ChannelState> Channels = new(StringComparer.Ordinal);

    static Zoltraak()
    {
        ChannelSkills.Register(new ChannelSkillDefinition
        {
            SkillKey = SkillKey,
            Ticks = Ticks,
            Interval = TickInterval,
            Tick = BuildTick,
            InterruptReason = InterruptReason,
            Area = LineArea,
            Started = CastStarted,
            Finished = end =>
            {
                if (end.User == null) return;
                var id = end.User.Guid.ToString();
                ZoltraakSound.StopLoop(id);
                if (Channels.Remove(id, out var state) && state.Measuring) ZoltraakMeasurement.End(end);
            },
        });
    }

    internal static void EnsureChannelRegistered() { }   // runs the static constructor

    // Only a registered skill may be saved: a key the game cannot create would block saving.
    internal static bool Available
    {
        get
        {
            if (DataSheetManager.Instance?.GetSkillData(SkillKey) != null) return true;
            if (!unavailableLogged) DelversHost.Warning("FRIEREN_ZOLTRAAK_UNAVAILABLE: the skill is not registered, Frieren keeps her other spells.");
            unavailableLogged = true;
            return false;
        }
    }


    // Same gate as the game's own skill check ValidSelfCondition.CheckCondition: a dead, unconscious, stunned or
    // silenced user cannot cast. Sleeping is added on
    // top. Freezing ends the channel exactly when the game gives it one of these categories.
    private static readonly (AffecterCategory Category, string Reason)[] Interrupts =
    {
        (AffecterCategory.Unconscious, "unconscious"),
        (AffecterCategory.Stun, "stun"),
        (AffecterCategory.Silence, "silence"),
        (AffecterCategory.Sleeping, "sleeping")
    };

    private static string InterruptReason(IEntity user)
    {
        if (user == null) return "missing";
        if (user.TryCast<UnitEntity>()?.IsDead == true) return "dead";
        var affecters = EntityComponent._instance?.GetComponentOf<AffecterComponent>(user);
        if (affecters == null) return null;
        if (affecters.Has(StunKey)) return "stun";   // the key check the channel always had
        foreach (var (category, reason) in Interrupts)
            if (affecters.HasCategory(category)) return reason;
        return null;
    }


    // The game builds the skill's routine: the circle forms and the cast sound starts. Graphics
    // and sound must never stop the spell.
    private static void CastStarted(ChannelTick tick)
    {
        try { ZoltraakBeam.OnStart(tick); }
        catch (Exception ex) { DelversHost.Warning("FRIEREN_ZOLTRAAK_BEAM_FAILED: " + ex.Message); }
        try { ZoltraakSound.Cast(tick.InMapData.GetWorldPosition(tick.User.Guid)); }
        catch (Exception ex) { DelversHost.Warning("FRIEREN_ZOLTRAAK_SOUND_FAILED: " + ex.Message); }
    }

    // The first tick is the shot: its sound, then the beam sound until the channel ends.
    private static void BeamStarted(ChannelTick tick)
    {
        var position = tick.InMapData.GetWorldPosition(tick.User.Guid);
        ZoltraakSound.Shot(position);
        ZoltraakSound.StartLoop(tick.User.Guid.ToString(), position);
    }

    private static IEnumerable<IExecutionData> BuildTick(ChannelTick tick)
    {
        var id = tick.User.Guid.ToString();
        if (tick.Index == 0 || !Channels.ContainsKey(id)) Channels[id] = StartChannel(tick.User);
        var state = Channels[id];
        var stop = tick.Area?.StopReason ?? "range";
        if (state.Measuring) ZoltraakMeasurement.Tick(tick.User, tick.Index, stop, tick.Area?.Pass);
        // Graphics and sound must never stop the damage.
        try { ZoltraakBeam.OnTick(tick, Ticks); }
        catch (Exception ex) { DelversHost.Warning("FRIEREN_ZOLTRAAK_BEAM_FAILED: " + ex.Message); }
        if (tick.Index == 0)
        {
            try { BeamStarted(tick); }
            catch (Exception ex) { DelversHost.Warning("FRIEREN_ZOLTRAAK_SOUND_FAILED: " + ex.Message); }
        }
        var share = state.HitPercent / 100f;
        var magic = new Il2CppSystem.Nullable<DamageType>(DamageType.Magic);
        var ranged = new Il2CppSystem.Nullable<AttackType>(AttackType.Ranged);
        foreach (var target in tick.Targets)
        {
            // A building takes the whole hit on its durability; head and break are for units.
            if (target.TryCast<BuildingEntity>() != null)
            {
                var wall = Execution.Damage(target, DataValue.SelfCoef(share, StatType.MagicAttackPower),
                    magic, ranged, DamageProperty.AlwaysHit);
                if (state.Measuring) ZoltraakMeasurement.Record(tick.User, tick.Index, wall, target, "building", stop);
                yield return wall;
                continue;
            }
            // DamageProperty is a flag set. The game has no "body only": a normal hit lands wholly on
            // head or body, picked by weight (DamageSimulator.CalculateTargetStat; about 35 % went to
            // the head in the Toolkit's standard). The head share comes on top.
            // AlwaysHit: a focused beam must not be dodged ("Miss" in the test video).
            var body = Execution.Damage(target, DataValue.SelfCoef(share * BodyShare, StatType.MagicAttackPower),
                magic, ranged, DamageProperty.AlwaysHit);
            var head = Execution.Damage(target, DataValue.SelfCoef(share * HeadShare, StatType.MagicAttackPower),
                magic, ranged, DamageProperty.OnlyHead | DamageProperty.AlwaysHit);
            if (state.Measuring)
            {
                ZoltraakMeasurement.Record(tick.User, tick.Index, body, target, "body", stop);
                ZoltraakMeasurement.Record(tick.User, tick.Index, head, target, "head", stop);
            }
            yield return body;
            yield return head;
            yield return Execution.AddAffecter(target, BreakKey, DataValue.FixedValue(1), DataValue.FixedValue(BreakDuration));
        }
    }

    // Decided once per channel: only a Core test campaign measures and may use another share.
    private static ChannelState StartChannel(IEntity user)
    {
        var campaign = CurrentCampaign();
        var measuring = campaign != null && DelversCoreRuntime.IsTestCampaign(campaign);
        var state = new ChannelState { HitPercent = measuring ? TestHitPercent(campaign) : DefaultHitPercent, Measuring = measuring };
        if (measuring) ZoltraakMeasurement.Begin(user, state.HitPercent);
        return state;
    }

    private static string CurrentCampaign()
    {
        try { return UnityEngine.Object.FindObjectOfType<MainFlow>()?._campaignData?.ClanDataContainer?.CampaignGuid.ToString(); }
        catch (Exception) { return null; }
    }

    private static float TestHitPercent(string campaign)
    {
        if (testHitPercent.HasValue && testHitPercentCampaign == campaign) return testHitPercent.Value;
        return DefaultHitPercent;
    }

    // Toolkit and self-test (FrierenRuntime.SetZoltraakHitPercentForTest): the share per hit for
    // the running test campaign. Refused outside a Core test campaign; returns the active value.
    internal static float SetHitPercentForTest(float percent)
    {
        var campaign = CurrentCampaign();
        if (campaign == null || !DelversCoreRuntime.IsTestCampaign(campaign))
        {
            DelversHost.Warning("FRIEREN_ZOLTRAAK_TEST_PERCENT_REFUSED reason=not-a-test-campaign");
            return DefaultHitPercent;
        }
        if (!(percent > 0f && percent <= 100f))
        {
            DelversHost.Warning("FRIEREN_ZOLTRAAK_TEST_PERCENT_REFUSED reason=out-of-range percent="
                + percent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return TestHitPercent(campaign);
        }
        testHitPercent = percent;
        testHitPercentCampaign = campaign;
        DelversHost.Info("FRIEREN_ZOLTRAAK_TEST_PERCENT percent=" + percent.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " campaign=" + campaign);
        return percent;
    }
}
