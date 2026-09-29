using HarmonyLib;
using MelonLoader;
using DungeonSettlersDelvers.Core;

[assembly: MelonInfo(typeof(FrierenPortrait.FrierenMod), "Dungeon Settlers Delvers: Frieren", "0.4.0", "Fre-yin")]
[assembly: MelonGame(null, "DungeonSettlers")]
[assembly: MelonAdditionalDependencies("DungeonSettlersDelvers.Core.MelonLoader")]
[assembly: HarmonyDontPatchAll]

namespace FrierenPortrait;

// MelonLoader entry point. Gameplay state and patches live in FrierenRuntime.
public sealed class FrierenMod : MelonMod
{
    private FrierenRuntime runtime;
    private bool ownsRuntime;

    public override void OnInitializeMelon()
    {
        if (ownsRuntime) return;
        if (!DelversCoreRuntime.IsReady || DelversCoreRuntime.LoaderProfile != "Melon"
            || !DelversCoreRuntime.SupportsApi(DelversCoreRuntime.MinimumApiVersionForNativeValueLists))
        {
            LoggerInstance.Error("FRIEREN_DISABLED: Core API 1.4.0 or later in API 1.x must initialize first. No Frieren patches were applied.");
            return;
        }
        runtime = new FrierenRuntime();
        try
        {
            runtime.Initialize(HarmonyInstance);
            ownsRuntime = true;
            try { FrierenMelonCompatibilityDiagnostics.Report(); }
            catch (Exception ex) { DelversHost.Warning("FRIEREN_HOTBAR_DIAGNOSTIC_FAILED: " + ex.Message); }
        }
        catch (Exception ex)
        {
            if (ownsRuntime)
            {
                HarmonyInstance.UnpatchSelf();
                runtime?.Shutdown();
            }
            runtime = null;
            ownsRuntime = false;
            LoggerInstance.Error("FRIEREN_DISABLED before registration completed: " + ex);
        }
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        => runtime?.OnSceneWasInitialized(buildIndex, sceneName);

    public override void OnUpdate() => runtime?.Update();

    public override void OnDeinitializeMelon()
    {
        if (ownsRuntime)
        {
            HarmonyInstance.UnpatchSelf();
            runtime?.Shutdown();
        }
        runtime = null;
        ownsRuntime = false;
    }
}
