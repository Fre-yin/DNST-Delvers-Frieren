using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace FrierenPortrait;

// Zoltraak, the registration: the skill row and the break affecter row added to the game's sheets,
// the skill's creation through the template's class and the two icons (loaded once, shown through
// the game's skill icon lookup and FrierenIconRegistry).
internal static partial class Zoltraak
{
    private static Sprite icon, breakIcon;
    private static bool iconFailed, breakIconFailed;

    internal static void RegisterBreak(AffecterSheet sheet)
    {
        if (sheet?._affecterTable == null || sheet.GetData(BreakKey) != null) return;
        var template = sheet.GetData(BreakTemplateKey);
        if (template == null) { DelversHost.Warning("FRIEREN_ZOLTRAAK_TEMPLATE_MISSING key=" + BreakTemplateKey); return; }
        var row = new AffecterTableData
        {
            Key = BreakKey,
            DevComment1 = BreakName,
            DevComment2 = "PhysicalResistance -1 and MagicResistance -1 per stack",
            Type = template.Type,
            MaxStack = BreakMaxStack,
            IsPositive = PositiveType.Negative,
            HasDuration = true,
            IsAbsolute = template.IsAbsolute,
            IsExposedToPlayer = true,
            BuildingAttachable = false,
            Categories = template.Categories,
            AdjustStatsData = new Il2CppStringArray(0),
            AdjustStatsPerStackData = new Il2CppStringArray(new[] { "PhysicalResistance_-" + BreakPerStack.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), "MagicResistance_-" + BreakPerStack.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) }),
            FloatValues = template.FloatValues,
            BindingValue = template.BindingValue ?? string.Empty,
            AdjustStats = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>(),
            AdjustStatsPerStack = new Il2CppSystem.Collections.Generic.Dictionary<StatType, float>()
        };
        row.AdjustStatsPerStack.Add(StatType.PhysicalResistance, -BreakPerStack);
        row.AdjustStatsPerStack.Add(StatType.MagicResistance, -BreakPerStack);
        sheet._affecterTable.Add(BreakKey, row);
        DelversHost.Info($"FRIEREN_ZOLTRAAK_BREAK_REGISTERED key={BreakKey} template={BreakTemplateKey} maxStack={BreakMaxStack} perStack=PhysicalResistance_-1;MagicResistance_-1");
    }

    internal static void Register(SkillSheet sheet)
    {
        if (sheet?._skillTable == null || sheet._skillTable.ContainsKey(SkillKey)) return;
        var t = sheet.GetData(TemplateKey);
        if (t == null) { DelversHost.Warning("FRIEREN_ZOLTRAAK_TEMPLATE_MISSING key=" + TemplateKey); return; }
        var row = new SkillTableData
        {
            Key = SkillKey,
            DevComment_SkillName = Name,
            DevComment_SkillFloatValues = "{0} % magic attack per hit, {1} hits, {2} s, {3} resistance per break stack, {4} s break, {5} break stacks",
            SkillType = t.SkillType,
            IsBasicAttackSkill = false,
            // Tooltip step 1: Zoltraak's own numbers for the game's tooltip ({0} to {5}, ZoltraakTextRules).
            FloatValues = new Il2CppStructArray<float>(TooltipValues),
            DamageType = DamageType.Magic,
            HitType = t.HitType,
            TargetType = t.TargetType,
            AffectTarget = t.AffectTarget,
            Range = t.Range,
            AreaType = t.AreaType,
            AreaRadius = t.AreaRadius,
            AllowTargetOutside = t.AllowTargetOutside,
            AllowOvercomeWall = t.AllowOvercomeWall,
            IsDelayAdjustedByAttackSpeed = t.IsDelayAdjustedByAttackSpeed,
            StartDelay = t.StartDelay,
            EndDelay = t.EndDelay,
            // The cooldown starts after the channel (test), so it must stay below the
            // 12 s break duration for a second channel to reach 30 stacks.
            Cooldown = 10f,
            Duration = t.Duration,
            EnergyCost = t.EnergyCost,
            DurabilityCost = t.DurabilityCost,
            ProperDurabilityEquip = t.ProperDurabilityEquip,
            RequiredEquipment = t.RequiredEquipment,
            AmmoType = t.AmmoType,
            AmmoAmount = t.AmmoAmount,
            CreatureKey = t.CreatureKey,
            CreatureAmount = t.CreatureAmount,
            RequiredSkillPoint = t.RequiredSkillPoint,
            RequiredLevel = t.RequiredLevel,
            PrerequisiteSkills = t.PrerequisiteSkills,
            Categories = t.Categories
        };
        sheet._skillTable.Add(SkillKey, row);
        DelversHost.Info($"FRIEREN_ZOLTRAAK_REGISTERED key={SkillKey} class={TemplateKey} area={row.AreaType} range={row.Range} cooldown={row.Cooldown}");
    }

    // The class comes from the template, key and data are ours.
    internal static Skill_Base Create()
    {
        var skill = Skill_Base.Create(TemplateKey);
        var data = DataSheetManager.Instance?.GetSkillData(SkillKey);
        if (skill == null || data == null) return null;
        skill._Key_k__BackingField = SkillKey;
        skill._Data_k__BackingField = data;
        UseStraightLine(skill.TryCast<ActiveSkill_Base>());
        return skill;
    }

    internal static Sprite Icon()
    {
        if (!icon && !iconFailed) icon = LoadIcon(IconFile, 32, "Zoltraak_Icon", out iconFailed);
        return icon;
    }

    // Own drawing (no game pixels), shown through FrierenIconRegistry.
    internal static Sprite BreakIcon()
    {
        if (!breakIcon && !breakIconFailed) breakIcon = LoadIcon(BreakIconFile, 16, "Zoltraak_Break_Icon", out breakIconFailed);
        return breakIcon;
    }

    private static Sprite LoadIcon(string file, int size, string name, out bool failed)
    {
        failed = false;
        try
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(PortraitAssets.AssetFolder, file)), false)
                || texture.width != size || texture.height != size)
                throw new InvalidDataException($"{file} ist kein {size}x{size}-PNG");
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
        catch (Exception ex)
        {
            failed = true;
            DelversHost.Warning("FRIEREN_ZOLTRAAK_ICON_FALLBACK: " + ex.Message);
            return null;
        }
    }
}
