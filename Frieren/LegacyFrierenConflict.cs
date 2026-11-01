namespace FrierenPortrait;

// LegacyFrierenConflict, the old DLL check: Frieren does not start next to the legacy
// DungeonSettlersFrierenPortrait DLL, whether it is already loaded or still lies in a Mods or
// BepInEx/plugins folder. FrierenRuntime.Initialize and FrierenBootstrap.Initialize call it.
internal static class LegacyFrierenConflict
{
    private const string LegacyAssemblyName = "DungeonSettlersFrierenPortrait";
    private const string LegacyFileName = LegacyAssemblyName + ".dll";

    internal static void ThrowIfPresent()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name,
                LegacyAssemblyName, StringComparison.OrdinalIgnoreCase));
        if (loaded != null)
            throw new InvalidOperationException("The legacy Frieren DLL is already loaded. Remove "
                + LegacyFileName + " from Mods or BepInEx/plugins and restart the game.");

        var gameRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
        var pluginRoots = new[]
        {
            Path.Combine(gameRoot, "Mods"),
            Path.Combine(gameRoot, "BepInEx", "plugins")
        };
        string file = null;
        foreach (var root in pluginRoots)
        {
            if (!Directory.Exists(root)) continue;
            try { file = Directory.EnumerateFiles(root, LegacyFileName, SearchOption.AllDirectories).FirstOrDefault(); }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            if (file != null) break;
        }
        if (file != null)
            throw new InvalidOperationException("Remove the legacy Frieren DLL at " + file
                + " and restart the game before loading Dungeon Settlers Delvers: Frieren.");
    }
}
