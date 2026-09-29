using global::BepInEx;
using global::BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using DungeonSettlersDelvers.Core;
using FrierenPortrait;

namespace DungeonSettlersDelvers.Frieren.BepInEx;

[BepInPlugin(Id, "Dungeon Settlers Delvers: Frieren", "0.4.0")]
[BepInProcess("DungeonSettlers.exe")]
[BepInDependency(DelversCoreRuntime.BepInExPluginId, "0.4.0")]
public sealed class FrierenPlugin : BasePlugin
{
    internal const string Id = "fre-yin.dungeonsettlers.delvers.frieren";
    private FrierenRuntime runtime;
    private Harmony harmony;
    private FrierenDriver driver;
    private bool ownsRuntime;

    public override void Load()
    {
        if (ownsRuntime) return;
        if (!DelversCoreRuntime.IsReady || DelversCoreRuntime.LoaderProfile != "BepInEx"
            || !DelversCoreRuntime.SupportsApi(DelversCoreRuntime.MinimumApiVersionForNativeValueLists))
        {
            Log.LogError("FRIEREN_DISABLED: Core API 1.4.0 or later in API 1.x must initialize first. No Frieren patches were applied.");
            return;
        }

        try
        {
            harmony = new Harmony(Id);
            runtime = new FrierenRuntime();
            runtime.Initialize(harmony);
            ownsRuntime = true;
            driver = AddComponent<FrierenDriver>();
            driver.Attach(runtime, harmony);
            FrierenBepInExCompatibilityDiagnostics.Report(Log);
        }
        catch (Exception ex)
        {
            if (ownsRuntime)
            {
                harmony?.UnpatchSelf();
                runtime?.Shutdown();
            }
            if (driver) UnityEngine.Object.Destroy(driver);
            driver = null;
            runtime = null;
            ownsRuntime = false;
            Log.LogError("FRIEREN_DISABLED before registration completed: " + ex);
            throw;
        }
    }

    // Persisted custom IDs make hot-unload unsafe during a running campaign.
    public override bool Unload() => false;
}

public sealed class FrierenDriver : MonoBehaviour
{
    private FrierenRuntime runtime;
    private Harmony harmony;
    private UnityAction<Scene, LoadSceneMode> sceneLoaded;
    private bool detached;

    public FrierenDriver(IntPtr pointer) : base(pointer) { }

    [HideFromIl2Cpp]
    internal void Attach(FrierenRuntime value, Harmony owner)
    {
        runtime = value;
        harmony = owner;
        sceneLoaded = (Action<Scene, LoadSceneMode>)OnSceneLoaded;
        SceneManager.sceneLoaded += sceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        => runtime?.OnSceneWasInitialized(scene.buildIndex, scene.name);

    public void Update() => runtime?.Update();
    public void OnApplicationQuit() => Detach();
    public void OnDestroy() => Detach();

    [HideFromIl2Cpp]
    private void Detach()
    {
        if (detached) return;
        detached = true;
        if (sceneLoaded != null) SceneManager.sceneLoaded -= sceneLoaded;
        sceneLoaded = null;
        harmony?.UnpatchSelf();
        harmony = null;
        runtime?.Shutdown();
        runtime = null;
    }
}
