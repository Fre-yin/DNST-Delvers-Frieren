using global::BepInEx.Unity.IL2CPP;
using DungeonSettlersDelvers.Core;
using FrierenPortrait;

namespace DungeonSettlersDelvers.Frieren.BepInEx;

internal static class FrierenBepInExCompatibilityDiagnostics
{
    private const string ExtendedHotbarPluginId = "fre-yin.dnst.extended-hotbar";

    internal static void Report(global::BepInEx.Logging.ManualLogSource log)
    {
        try
        {
            var plugin = IL2CPPChainloader.Instance?.Plugins?.Values
                .FirstOrDefault(item => string.Equals(item.Metadata.GUID, ExtendedHotbarPluginId,
                    StringComparison.OrdinalIgnoreCase));
            if (plugin == null)
            {
                log.LogWarning("FRIEREN_HOTBAR_DIAGNOSTIC status=not-loaded expectedVersion="
                    + CompatibilityModIdentity.SupportedExtendedHotbarVersion
                    + " requiredInPractice=true hardReference=false patchesHotbar=false");
                return;
            }
            var version = plugin.Metadata.Version.ToString();
            var status = CompatibilityModIdentity.ClassifyExtendedHotbar(
                CompatibilityModIdentity.ExtendedHotbarAssembly, CompatibilityModIdentity.ExtendedHotbarName, version);
            var message = "FRIEREN_HOTBAR_DIAGNOSTIC name=" + plugin.Metadata.Name
                + " guid=" + plugin.Metadata.GUID + " version=" + version
                + " expectedVersion=" + CompatibilityModIdentity.SupportedExtendedHotbarVersion
                + " requiredInPractice=true hardReference=false patchesHotbar=false";
            if (status == ExtendedHotbarCompatibilityStatus.Compatible) log.LogInfo(message + " status=compatible");
            else log.LogWarning(message + " status=" + (status == ExtendedHotbarCompatibilityStatus.UnsupportedVersion
                ? "unsupported-version" : "unknown-version"));
        }
        catch (Exception ex)
        {
            log.LogWarning("FRIEREN_HOTBAR_DIAGNOSTIC status=inspection-unavailable: " + ex.Message);
        }
    }
}
