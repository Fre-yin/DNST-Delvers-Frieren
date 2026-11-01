using HarmonyLib;

namespace FrierenPortrait;

internal static class FrierenBootstrap
{
    internal static string SupportedBuilds => DelversCoreRuntime.SupportedBuilds;

    internal static void Initialize(HarmonyLib.Harmony harmony)
    {
        if (!DelversCoreRuntime.IsReady || !DelversCoreRuntime.SupportsApi(FrierenRuntime.RequiredCoreApi))
            throw new InvalidOperationException($"Dungeon Settlers Delvers Core API {FrierenRuntime.RequiredCoreApi} muss vor Frieren geladen sein.");
        LegacyFrierenConflict.ThrowIfPresent();
        PortraitAssets.ValidateFiles();
        PatchAllClasses(harmony);
    }

    // Like Core: PatchAll stops at the first broken class and may only say "IL Compile Error
    // (unknown location)". Patching class by class names every broken class and its target in
    // one start. Any failure still leaves Frieren off: the adapter unpatches everything.
    private static void PatchAllClasses(HarmonyLib.Harmony harmony)
    {
        var failed = new List<string>();
        foreach (var type in AccessTools.GetTypesFromAssembly(typeof(FrierenBootstrap).Assembly))
        {
            try { harmony.CreateClassProcessor(type).Patch(); }
            catch (Exception ex)
            {
                var target = DescribePatchTarget(type);
                failed.Add(type.Name + " -> " + target);
                DelversHost.Error("FRIEREN_PATCH_CLASS_FAILED class=" + type.FullName + " target=" + target
                    + " error=" + DescribeInnermost(ex));
            }
        }
        if (failed.Count > 0)
            throw new InvalidOperationException(failed.Count + " Harmony patch class(es) failed: " + string.Join("; ", failed));
    }

    private static string DescribePatchTarget(Type patchClass)
    {
        try
        {
            var info = HarmonyMethod.Merge(HarmonyMethodExtensions.GetFromType(patchClass));
            var name = (info.declaringType?.Name ?? "?") + "." + (info.methodName ?? "TargetMethod()");
            if (info.argumentTypes == null) return name;
            var arguments = new List<string>();
            foreach (var argument in info.argumentTypes) arguments.Add(argument?.Name ?? "?");
            return name + "(" + string.Join(", ", arguments) + ")";
        }
        catch (Exception) { return "unknown"; }
    }

    private static string DescribeInnermost(Exception error)
    {
        var inner = error;
        while (inner.InnerException != null) inner = inner.InnerException;
        var text = inner == error ? error.Message : error.Message + " / " + inner.GetType().Name + ": " + inner.Message;
        return text.Replace('\n', ' ').Replace('\r', ' ');
    }

}
