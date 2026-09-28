using System.Collections;
using System.Reflection;
using MelonLoader;
using DungeonSettlersDelvers.Core;

namespace FrierenPortrait;

// Optional loader diagnostic: inspect metadata only, without resolving or
// patching another mod's implementation.
internal static class FrierenMelonCompatibilityDiagnostics
{
    private static bool reported;

    internal static void Report()
    {
        if (reported) return;
        reported = true;
        var registeredProperty = typeof(MelonBase).GetProperty("RegisteredMelons",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (registeredProperty?.GetValue(null) is not IEnumerable registered)
        {
            DelversHost.Warning("FRIEREN_HOTBAR_DIAGNOSTIC status=inspection-unavailable"
                + " expectedAssembly=" + CompatibilityModIdentity.ExtendedHotbarAssembly
                + " expectedVersion=" + CompatibilityModIdentity.SupportedExtendedHotbarVersion
                + " requiredInPractice=true hardReference=false");
            return;
        }

        foreach (var melon in registered)
        {
            if (melon == null) continue;
            var assemblyName = melon.GetType().Assembly.GetName().Name;
            var info = typeof(MelonBase).GetProperty("Info", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(melon);
            var infoType = info?.GetType();
            var name = infoType?.GetProperty("Name")?.GetValue(info)?.ToString();
            if (!CompatibilityModIdentity.IsExtendedHotbar(assemblyName, name)) continue;
            var version = infoType?.GetProperty("Version")?.GetValue(info)?.ToString();
            var status = CompatibilityModIdentity.ClassifyExtendedHotbar(assemblyName, name, version);
            var message = "FRIEREN_HOTBAR_DIAGNOSTIC"
                + " name=" + (name ?? "unknown")
                + " assembly=" + (assemblyName ?? "unknown")
                + " version=" + (version ?? "unknown")
                + " expectedVersion=" + CompatibilityModIdentity.SupportedExtendedHotbarVersion
                + " requiredInPractice=true hardReference=false patchesHotbar=false";
            if (status == ExtendedHotbarCompatibilityStatus.Compatible)
                DelversHost.Info(message + " status=compatible");
            else
            {
                var statusLabel = status == ExtendedHotbarCompatibilityStatus.UnsupportedVersion
                    ? "unsupported-version" : "unknown-version";
                DelversHost.Warning(message + " status=" + statusLabel);
            }
            return;
        }

        DelversHost.Warning("FRIEREN_HOTBAR_DIAGNOSTIC status=not-loaded"
            + " expectedAssembly=" + CompatibilityModIdentity.ExtendedHotbarAssembly
            + " expectedVersion=" + CompatibilityModIdentity.SupportedExtendedHotbarVersion
            + " requiredInPractice=true hardReference=false");
    }
}
