using System.Buffers.Binary;
using UnityEngine;
using SpriteList = Il2CppSystem.Collections.Generic.List<UnityEngine.Sprite>;

namespace FrierenPortrait;

internal sealed class PortraitAssets : IDisposable
{
    private static readonly string[] RuntimeFiles =
    {
        "Frieren_Normal.png", "Frieren_Stress.png", "Elfische_Erzmagierin.png", "Booklover.png"
    };

    internal static string PackagedAssetFolder => Path.Combine(
        DelversHost.PackageDirectory ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Mods", "DungeonSettlersDelvers"),
        "Frieren");

    internal static string LegacyAssetFolder => Path.Combine(
        Path.GetDirectoryName(Application.dataPath), "Mods", "FrierenPortraitAssets");

    internal static string AssetFolder => HasRuntimeFiles(PackagedAssetFolder) ? PackagedAssetFolder
        : HasRuntimeFiles(LegacyAssetFolder) ? LegacyAssetFolder : PackagedAssetFolder;

    internal static void ValidateFiles()
    {
        var folder = AssetFolder;
        var normal = Read(Path.Combine(folder, RuntimeFiles[0]));
        var stress = Read(Path.Combine(folder, RuntimeFiles[1]));
        var trait = Read(Path.Combine(folder, RuntimeFiles[2]), 24);
        var booklover = Read(Path.Combine(folder, RuntimeFiles[3]), 24);
        if (normal.Length == 0 || stress.Length == 0 || trait.Length == 0 || booklover.Length == 0)
            throw new InvalidDataException("Die vier Frieren-Laufzeitgrafiken müssen gemeinsam verfügbar sein: " + folder);
    }

    private static bool HasRuntimeFiles(string folder)
        => RuntimeFiles.All(file => File.Exists(Path.Combine(folder, file)));

    internal Sprite Normal { get; private set; }
    internal Sprite Stress { get; private set; }
    internal Sprite TraitIcon { get; private set; }
    internal Sprite BookloverIcon { get; private set; }
    internal SpriteList WorldHair { get; private set; }
    internal bool PortraitReady => Normal && Normal.texture && Stress && Stress.texture
        && TraitIcon && TraitIcon.texture && BookloverIcon && BookloverIcon.texture;
    internal bool WorldHairReady => WorldHair != null && WorldHair.Count == 3
        && WorldHair[0] && WorldHair[0].texture && WorldHair[1] && WorldHair[1].texture
        && WorldHair[2] && WorldHair[2].texture;
    internal bool Alive => PortraitReady && WorldHairReady;

    internal void Load(string folder = null)
    {
        if (PortraitReady) return;
        Dispose();
        try
        {
            // Validate both files before creating or registering any Unity object.
            folder ??= AssetFolder;
            var normal = Read(Path.Combine(folder, "Frieren_Normal.png"));
            var stress = Read(Path.Combine(folder, "Frieren_Stress.png"));
            var trait = Read(Path.Combine(folder, "Elfische_Erzmagierin.png"), 24);
            var booklover = Read(Path.Combine(folder, "Booklover.png"), 24);
            Normal = Create(normal, "Frieren_Normal");
            Stress = Create(stress, "Frieren_Stress");
            TraitIcon = Create(trait, "Elfische_Erzmagierin", 24);
            BookloverIcon = Create(booklover, "Booklover", 24);
        }
        catch { Dispose(); throw; }
    }

    internal void LoadWorldHair(SpriteList template)
    {
        if (WorldHairReady) return;
        if (template == null || template.Count != 3)
            throw new InvalidOperationException("Die originale dreiteilige Elfenhaar-Grafik fehlt.");
        for (var i = 0; i < 3; i++)
        {
            if (!template[i] || template[i].rect.width != 64 || template[i].rect.height != 40
                || template[i].pixelsPerUnit != 16 || template[i].pivot != new Vector2(32, 20)
                || !template[i].texture || !template[i].texture.isReadable
                || template[i].texture.width != 64 || template[i].texture.height != 40)
                throw new InvalidOperationException("Die originale Elfenhaar-Grafik hat unerwartete Abmessungen.");
        }
        var created = new SpriteList();
        try
        {
            for (var i = 0; i < 3; i++)
                created.Add(CloneNativeHairWithColorSwap(template[i], $"FrierenHair_{i}"));
            WorldHair = created;
        }
        catch
        {
            for (var i = 0; i < created.Count; i++) Destroy(created[i]);
            throw;
        }
    }

    // Copy the *actual loaded sprite* of Teemu's Frieren. This preserves the
    // game's frame order, texture coordinates, silhouette, alpha, pivot and
    // pixels; only the three opaque Elf09_Blue hair tones are changed.
    private static Sprite CloneNativeHairWithColorSwap(Sprite source, string name)
    {
        var original = source.texture;
        var pixels = original.GetPixels32(); // Unity returns a detached array.
        var recolored = 0;
        for (var i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];
            if (pixel.a == 0) continue;
            if (pixel.r == 0x5D && pixel.g == 0x57 && pixel.b == 0x6A)
                { pixel.r = 0x9D; pixel.g = 0x99; pixel.b = 0xAC; }
            else if (pixel.r == 0x8A && pixel.g == 0x81 && pixel.b == 0x9D)
                { pixel.r = 0xC6; pixel.g = 0xC3; pixel.b = 0xD0; }
            else if (pixel.r == 0xA9 && pixel.g == 0xA0 && pixel.b == 0xAE)
                { pixel.r = 0xE0; pixel.g = 0xDE; pixel.b = 0xEA; }
            else if (pixel.r == 0x22 && pixel.g == 0x20 && pixel.b == 0x25)
                continue; // original contour
            else
                throw new InvalidDataException($"Unerwartete Farbe im originalen Teemu-Haarbild {name}.");
            pixels[i] = pixel;
            recolored++;
        }
        if (recolored == 0)
            throw new InvalidDataException($"Keine Haarfarbe im originalen Teemu-Haarbild {name} gefunden.");

        var texture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        Sprite copy = null;
        try
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            texture.name = name + "_Texture";
            texture.filterMode = original.filterMode;
            texture.wrapMode = original.wrapMode;
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var pivot = new Vector2(source.pivot.x / source.rect.width,
                source.pivot.y / source.rect.height);
            copy = Sprite.Create(texture, source.rect, pivot, source.pixelsPerUnit,
                0, SpriteMeshType.FullRect);
            if (!copy) throw new InvalidOperationException("Teemu-Haarbild konnte nicht kopiert werden: " + name);
            copy.name = name;
            copy.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return copy;
        }
        catch
        {
            if (copy) UnityEngine.Object.Destroy(copy);
            UnityEngine.Object.Destroy(texture);
            throw;
        }
    }

    internal static byte[] Read(string path, int size = 48) => Read(path, size, size);

    internal static byte[] Read(string path, int width, int height)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 33 || bytes.Length > 1024 * 1024
            || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)) != width
            || BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)) != height)
            throw new InvalidDataException($"Erwartet wird ein PNG mit {width} × {height} Pixeln: " + path);
        return bytes;
    }

    private static Sprite Create(byte[] bytes, string name, int size = 48) => Create(bytes, name, size, size);

    private static Sprite Create(byte[] bytes, string name, int width, int height)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Sprite sprite = null;
        try
        {
            if (!ImageConversion.LoadImage(texture, bytes, false) || texture.width != width || texture.height != height)
                throw new InvalidDataException("PNG konnte nicht geladen werden: " + name);
            texture.name = name + "_Texture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            if (!sprite) throw new InvalidOperationException("Sprite konnte nicht erzeugt werden: " + name);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
        catch
        {
            if (sprite) UnityEngine.Object.Destroy(sprite);
            UnityEngine.Object.Destroy(texture);
            throw;
        }
    }

    public void Dispose()
    {
        Destroy(Normal);
        Destroy(Stress);
        Destroy(TraitIcon);
        Destroy(BookloverIcon);
        if (WorldHair != null)
            for (var i = 0; i < WorldHair.Count; i++) Destroy(WorldHair[i]);
        Normal = Stress = TraitIcon = BookloverIcon = null;
        WorldHair = null;
    }

    private static void Destroy(Sprite sprite)
    {
        if (!sprite) return;
        var texture = sprite.texture;
        UnityEngine.Object.Destroy(sprite);
        if (texture) UnityEngine.Object.Destroy(texture);
    }
}
