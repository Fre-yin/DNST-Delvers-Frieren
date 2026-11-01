using System.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
#if BEPINEX
using VfxViewType = global::Refactor.FX.VFXView;
#else
using VfxViewType = Il2CppRefactor.FX.VFXView;
#endif

namespace FrierenPortrait;

// Zoltraak beam (ZOLTRAAK-RECHERCHE.md, ZOLTRAAK-UMSETZUNG.md Abschnitte 11 und 14), drawn at the
// game's scale of 16 pixels per tile. The author's magic circle stands in front of Frieren; its
// drawing turns while both rims stand still. When the beam fires, a light impulse runs out of the
// circle, the beam leaves its centre pointed, tiled up to the first enemy or obstacle, fades
// out over a few pixels into a light point at its tip, and rings fray the tip. Every target hit
// and the obstacle that stops the beam get an impact; at an enemy, parts of the beam splinter
// off in a few directions. Driven by the
// channel; it follows the game speed, freezes while paused and fades out when the ticks stop
// (end, stun or a cancelled skill).
internal static class ZoltraakBeam
{
    private const float PixelsPerUnit = 16f;
    private const string SortingLayer = "FX";        // layer of the damage numbers (VFX catalog)
    private const float HeightOffset = 0.35f;         // from the unit's world position up to hand height
    private const float CircleDistance = 1.1f;        // circle in front of Frieren, not on her (test)
    // The segment's light wave moves 4 px per frame over 8 frames (once round
    // without a jump), 100 px/s as before; the flicker keeps its own, slower step.
    private const string SegmentFile = "Zoltraak_Strahlsegment_32x12_8Bilder.png";
    private const int SegmentFrames = 8;
    private const int SegmentWidth = 32, SegmentHeight = 12;
    // The tip: a light point of four frames instead of the hard end cap, and before
    // it the band fades out over FadePixels, cut from the segment where the tiled body stops, so
    // the light wave runs on into the fade without a seam.
    private const string TipFile = "Zoltraak_Lichtpunkt_17x17_4Bilder.png";
    private const int TipSize = 17, TipFrameCount = 4;
    private const int FadePixels = 6;
    private const float TipFrame = 0.08f;
    private const float FadeLength = FadePixels / PixelsPerUnit;
    private const float CircleFrame = 0.12f, BodyFrame = 0.04f, FlickerFrame = 0.08f, ImpactFrame = 0.07f;
    // Degrees per second of game time, clockwise. The drawing is four-fold symmetric, so any
    // speed loops without a jump.
    private const float CircleSpin = 90f;
    // Cast impulse (template): three frames of preparation, the shot from frame 4.
    private static readonly float[] ImpulseFrames = { 0.08f, 0.06f, 0.04f, 0.04f, 0.07f, 0.09f, 0.12f, 0.10f };
    private const int ImpulseShotFrame = 3;
    // The pointed start, 16 px out of the circle centre. It is cut from the beam segment itself
    // (TaperFrames), so colours and light wave run on into the band without a seam (no seam, and no extra sparks beside
    // the start, the anime has none).
    private const float WedgeLength = 1f;
    private const int WedgePixels = 16;
    private const int RingCount = 5;
    private const float RingSpeed = 1.2f;             // passes per second from mid beam to the tip
    // (sketch): where the beam strikes an enemy, parts of it splinter off in a
    // few directions. Placeholder from the beam's own segment: short rays fanning forward, renewed
    // with every tick, growing out and fading.
    private static readonly float[] SplinterAngles = { -55f, -22f, 22f, 55f };
    private const float SplinterLife = 0.2f, SplinterLength = 1.2f, SplinterWidth = 0.5f;
    private const float TickTimeout = 0.6f, StartTimeout = 1.5f, EndLinger = 0.25f;
    // Motion in code, the pixels stay the beam shoots out, flickers and collapses.
    private const float GrowTime = 0.15f, MinAlpha = 0.82f;

    private sealed class Beam
    {
        internal GameObject Root;
        internal SpriteRenderer CircleRim, CircleInner, Impulse, Wedge, Body, Fade, Tip;
        internal readonly SpriteRenderer[] Rings = new SpriteRenderer[RingCount];
        internal readonly SpriteRenderer[] Splinters = new SpriteRenderer[SplinterAngles.Length];
        internal readonly Dictionary<string, (SpriteRenderer Renderer, float Age)> Impacts = new(StringComparer.Ordinal);
        internal float Clock, ShotClock, SinceTick, EndTimer, FullLength, Spin, ImpulseAge, Angle;
        internal float SplinterAge = -1f, SplinterTurn;
        internal int BodyFrame;                       // the fade follows the body's light wave
        internal Vector2 Start, Direction, HitPoint;
        internal bool Firing, Ending;
    }

    private static readonly Dictionary<string, Beam> Active = new(StringComparer.Ordinal);
    private static Sprite[] circleRims, circleInners, impulseFrames, bodyFrames, wedgeFrames, ringFrames, impactFrames;
    private static Sprite[] tipFrames;
    private static Texture2D fadeTexture;
    // One fade sprite per segment frame and column, made when first needed.
    private static readonly Sprite[] FadeSprites = new Sprite[SegmentFrames * SegmentWidth];
    private static Material material;
    private static bool loadTried, loaded;
    private static object animator;
    private static float animatorBeat;

    // The cast begins: the circle forms and the impulse gathers before the first tick.
    internal static void OnStart(ChannelTick tick)
    {
        if (!Load() || tick?.User == null || tick.InMapData == null) return;
        var userId = tick.User.Guid.ToString();
        Aim(tick, out var start, out var direction, out _, out _);
        if (!Active.TryGetValue(userId, out var beam) || !beam.Root)
        {
            beam = Create();
            Active[userId] = beam;
        }
        Place(beam, start, start + direction, direction);
        beam.SinceTick = 0f;
        EnsureAnimator();
    }

    internal static void OnTick(ChannelTick tick, int ticks)
    {
        if (!Load() || tick?.User == null || tick.InMapData == null) return;
        var inMap = tick.InMapData;
        var userId = tick.User.Guid.ToString();
        Aim(tick, out var start, out var direction, out var end, out var endTile);

        if (!Active.TryGetValue(userId, out var beam) || !beam.Root)
        {
            beam = Create();
            Active[userId] = beam;
        }
        if (!beam.Firing)
        {
            // The shot: the beam grows out and the impulse jumps to its flash.
            beam.Firing = true;
            beam.ShotClock = 0f;
            beam.ImpulseAge = Math.Max(beam.ImpulseAge, ImpulseStart(ImpulseShotFrame));
        }
        Place(beam, start, end, direction);
        foreach (var target in tick.Targets)
            Impact(beam, target.Guid.ToString(), inMap.GetWorldPosition(target.Guid) + Vector2.up * HeightOffset);
        // Rock or an indestructible wall takes no hit, but the beam still bursts against it.
        var stop = tick.Area?.StopReason;
        if (stop != null && stop != "range" && stop != ZoltraakLineRules.TargetStop && tick.Area.StopTarget == null)
            Impact(beam, "stop", end);
        if (stop == ZoltraakLineRules.TargetStop)
        {
            beam.HitPoint = end;
            beam.SplinterAge = 0f;
            beam.SplinterTurn = (tick.Index % 3 - 1) * 8f;   // a little different every tick
        }
        beam.SinceTick = 0f;
        if (tick.Index + 1 >= ticks)
        {
            beam.Ending = true;
            beam.EndTimer = EndLinger;
        }
        EnsureAnimator();
    }

    // The one animation loop (ZoltraakLoopRules): a known loop without a heartbeat is gone and a new one starts, logged.
    private static void EnsureAnimator()
    {
        if (ZoltraakLoopRules.Stale(animator != null, Time.unscaledTime, animatorBeat))
        {
            DelversHost.Warning("FRIEREN_ZOLTRAAK_BEAM_ANIMATOR_RESTART stale=" + (Time.unscaledTime - animatorBeat).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s");
            animator = null;
        }
        if (animator != null) return;
        animatorBeat = Time.unscaledTime;
        animator = DelversHost.StartCoroutine(Animate());
    }

    // After a campaign load (FrierenMagicSkills): whether the loaded pictures and the material still exist; anything destroyed
    // makes Load run again at the next cast. The state line shows what a menu round trip left.
    internal static string CheckAfterLoad()
    {
        var alive = material && circleRims != null && circleRims.Length > 0 && circleRims[0] && bodyFrames != null && bodyFrames.Length > 0 && bodyFrames[0]
            && impactFrames != null && impactFrames.Length > 0 && impactFrames[0];
        var state = "loaded=" + loaded + " alive=" + alive + " animator=" + (animator != null ? "known" : "none");
        if (loadTried && !alive)
        {
            loadTried = false;
            loaded = false;
            state += " reload=next-cast";
        }
        return state;
    }

    // The beam ends where the hits end: the far edge of the last open tile, which is the near
    // edge of a wall that stopped the line. It points at the aimed tile.
    private static void Aim(ChannelTick tick, out Vector2 start, out Vector2 direction, out Vector2 end, out Vector2Int endTile)
    {
        var inMap = tick.InMapData;
        var userTile = inMap.GetLogicalPosition(tick.User.Guid);
        var userWorld = inMap.GetWorldPosition(tick.User.Guid);
        var offset = userWorld - new Vector2(userTile.x, userTile.y);
        var tiles = tick.Area?.Tiles;
        endTile = tiles == null ? FarthestTile(tick.Skill.GetEffectArea(userTile, tick.TargetPosition), userTile)
            : tiles.Count > 0 ? tiles[tiles.Count - 1] : userTile;
        start = userWorld + Vector2.up * HeightOffset;
        var aim = new Vector2(tick.TargetPosition.x - userTile.x, tick.TargetPosition.y - userTile.y);
        direction = aim.sqrMagnitude < 0.01f ? Vector2.right : aim.normalized;
        end = new Vector2(endTile.x, endTile.y) + offset + Vector2.up * HeightOffset + direction * 0.5f;
        // At an enemy the beam ends on the enemy itself, not behind it.
        if (tick.Area?.StopReason == ZoltraakLineRules.TargetStop && tick.Targets != null && tick.Targets.Count > 0)
            end = inMap.GetWorldPosition(tick.Targets[0].Guid) + Vector2.up * HeightOffset;
    }

    private static Vector2Int FarthestTile(Il2CppSystem.Collections.Generic.List<Vector2Int> area, Vector2Int from)
    {
        var best = from;
        var bestDistance = -1;
        if (area == null) return best;
        foreach (var tile in area)
        {
            var distance = (tile - from).sqrMagnitude;
            if (distance > bestDistance) { best = tile; bestDistance = distance; }
        }
        return best;
    }

    private static void Impact(Beam beam, string key, Vector2 position)
    {
        if (!beam.Impacts.TryGetValue(key, out var impact) || !impact.Renderer)
            impact = (Renderer(beam.Root, "Impact", impactFrames[0], -3), 0f);
        impact.Renderer.transform.position = position;
        impact.Renderer.enabled = true;
        beam.Impacts[key] = (impact.Renderer, 0f);
    }

    private static Beam Create()
    {
        var root = new GameObject("Delvers_Zoltraak_Beam");
        var beam = new Beam
        {
            Root = root,
            CircleRim = Renderer(root, "CircleRim", circleRims[0], -7),
            CircleInner = Renderer(root, "Circle", circleInners[0], -6),
            Body = Renderer(root, "Body", bodyFrames[0], -5),
            Wedge = Renderer(root, "StartWedge", wedgeFrames[0], -5),
            Fade = Renderer(root, "EndFade", FadeSprite(0, 0), -5),
            Tip = Renderer(root, "Tip", tipFrames[0], -3),
            Impulse = Renderer(root, "CastImpulse", impulseFrames[0], -3)
        };
        for (var i = 0; i < RingCount; i++) beam.Rings[i] = Renderer(root, "Ring", ringFrames[0], -4);
        for (var i = 0; i < beam.Splinters.Length; i++)
        {
            beam.Splinters[i] = Renderer(root, "Splinter", bodyFrames[0], -4);
            beam.Splinters[i].drawMode = SpriteDrawMode.Tiled;
            beam.Splinters[i].enabled = false;
        }
        beam.Body.drawMode = SpriteDrawMode.Tiled;
        Shape(beam);
        return beam;
    }

    // Pivots come with the sprites (see Load): body, wedge and fade start at their left edge, the
    // light point sits on its centre.
    private static SpriteRenderer Renderer(GameObject root, string name, Sprite sprite, int order)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = order;
        if (material) renderer.sharedMaterial = material;
        return renderer;
    }

    private static void Place(Beam beam, Vector2 start, Vector2 end, Vector2 direction)
    {
        beam.Angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var rotation = Quaternion.Euler(0f, 0f, beam.Angle);
        beam.Start = start + direction * CircleDistance;
        beam.Direction = direction;
        beam.FullLength = Mathf.Max(0.5f, Vector2.Distance(beam.Start, end));
        beam.CircleRim.transform.position = beam.Start;
        beam.CircleInner.transform.position = beam.Start;
        beam.Impulse.transform.position = beam.Start;
        beam.Wedge.transform.SetPositionAndRotation(beam.Start, rotation);
        beam.Body.transform.SetPositionAndRotation(beam.Start + direction * WedgeLength, rotation);
        beam.Fade.transform.rotation = rotation;
        foreach (var ring in beam.Rings) ring.transform.rotation = rotation;
        Shape(beam);
    }

    // The circle forms first; after the shot the length grows in over GrowTime. Width and circle
    // collapse while the beam ends.
    private static void Shape(Beam beam)
    {
        var collapse = beam.Ending ? Mathf.Clamp01(beam.EndTimer / EndLinger) : 1f;
        var circle = Mathf.Min(Mathf.Clamp01(beam.Clock / GrowTime), collapse);
        var circleScale = new Vector3(circle, circle, 1f);
        beam.CircleRim.transform.localScale = circleScale;
        beam.CircleInner.transform.localScale = circleScale;
        beam.CircleInner.transform.localRotation = Quaternion.Euler(0f, 0f, -beam.Spin);

        var length = beam.Firing ? Mathf.Max(0.05f, beam.FullLength * Mathf.Clamp01(beam.ShotClock / GrowTime)) : 0f;
        var width = new Vector3(1f, collapse, 1f);
        // Wedge, body, fade, light point; a beam too short for the fade ends in the body.
        var fade = beam.Firing && length > WedgeLength + FadeLength;
        var bodyLength = length - WedgeLength - (fade ? FadeLength : 0f);
        beam.Wedge.enabled = beam.Firing;
        beam.Tip.enabled = beam.Firing;
        beam.Body.enabled = beam.Firing && bodyLength > 0f;
        beam.Fade.enabled = fade;
        beam.Wedge.transform.localScale = new Vector3(Mathf.Min(1f, length / WedgeLength), collapse, 1f);
        beam.Body.size = new Vector2(Mathf.Max(0.01f, bodyLength), SegmentHeight / PixelsPerUnit);
        beam.Body.transform.localScale = width;
        if (fade)
        {
            // The fade starts at the segment column where the tiled body stops.
            var column = (int)(bodyLength * PixelsPerUnit) % SegmentWidth;
            beam.Fade.sprite = FadeSprite(beam.BodyFrame % SegmentFrames, column);
            beam.Fade.transform.position = beam.Start + beam.Direction * (WedgeLength + bodyLength);
            beam.Fade.transform.localScale = width;
        }
        beam.Tip.transform.position = beam.Start + beam.Direction * length;
        beam.Tip.transform.localScale = new Vector3(collapse, collapse, 1f);

        // The rings run from the middle of the beam to its tip and grow, then fade there.
        var ringStart = WedgeLength + (length - WedgeLength) * 0.5f;
        var showRings = beam.Firing && length > WedgeLength + 0.5f;
        for (var i = 0; i < RingCount; i++)
        {
            var ring = beam.Rings[i];
            ring.enabled = showRings;
            if (!showRings) continue;
            var phase = Mathf.Repeat(beam.Clock * RingSpeed + i / (float)RingCount, 1f);
            ring.transform.position = beam.Start + beam.Direction * Mathf.Lerp(ringStart, length, phase);
            ring.transform.localScale = width;
            ring.sprite = ringFrames[Math.Min(ringFrames.Length - 1, (int)(phase * ringFrames.Length))];
        }
    }

    // The rays at the hit point grow out to SplinterLength and fade within SplinterLife.
    private static void Splinter(Beam beam, float step, int bodyFrame)
    {
        if (beam.SplinterAge < 0f) return;
        beam.SplinterAge += step;
        var t = beam.SplinterAge / SplinterLife;
        var show = t < 1f && beam.Firing;
        var collapse = beam.Ending ? Mathf.Clamp01(beam.EndTimer / EndLinger) : 1f;
        for (var i = 0; i < beam.Splinters.Length; i++)
        {
            var ray = beam.Splinters[i];
            ray.enabled = show;
            if (!show) continue;
            ray.transform.SetPositionAndRotation(beam.HitPoint,
                Quaternion.Euler(0f, 0f, beam.Angle + SplinterAngles[i] + beam.SplinterTurn));
            ray.size = new Vector2(Mathf.Lerp(0.3f, SplinterLength, t), bodyFrames[0].rect.height / PixelsPerUnit);
            ray.transform.localScale = new Vector3(1f, SplinterWidth * (1f - 0.6f * t) * collapse, 1f);
            ray.sprite = bodyFrames[(bodyFrame + i) % bodyFrames.Length];
            ray.color = new Color(1f, 1f, 1f, 1f - t);
        }
        if (!show) beam.SplinterAge = -1f;
    }

    private static float ImpulseStart(int frame)
    {
        var time = 0f;
        for (var i = 0; i < frame; i++) time += ImpulseFrames[i];
        return time;
    }

    private static IEnumerator Animate()
    {
        try
        {
            while (Active.Count > 0)
            {
                animatorBeat = Time.unscaledTime;
                var step = Time.deltaTime * ZoltraakGameSpeedPatch.Multiplier;
                foreach (var key in Active.Keys.ToArray())
                {
                    var beam = Active[key];
                    if (!beam.Root) { Active.Remove(key); continue; }
                    beam.Clock += step;
                    beam.Spin = Mathf.Repeat(beam.Spin + step * CircleSpin, 360f);
                    beam.SinceTick += step;
                    if (beam.Firing) beam.ShotClock += step;
                    if (beam.Ending) beam.EndTimer -= step;
                    if ((beam.Ending && beam.EndTimer <= 0f) || beam.SinceTick > (beam.Firing ? TickTimeout : StartTimeout))
                    {
                        UnityEngine.Object.Destroy(beam.Root);
                        Active.Remove(key);
                        continue;
                    }
                    var circleFrame = (int)(beam.Clock / CircleFrame) % circleInners.Length;
                    beam.CircleRim.sprite = circleRims[circleFrame];
                    beam.CircleInner.sprite = circleInners[circleFrame];
                    // Without a shot the impulse holds its last preparation frame.
                    beam.ImpulseAge += step;
                    if (!beam.Firing) beam.ImpulseAge = Math.Min(beam.ImpulseAge, ImpulseStart(ImpulseShotFrame) - 0.001f);
                    var impulseFrame = 0;
                    while (impulseFrame < ImpulseFrames.Length && beam.ImpulseAge >= ImpulseStart(impulseFrame + 1)) impulseFrame++;
                    beam.Impulse.enabled = impulseFrame < impulseFrames.Length;
                    if (beam.Impulse.enabled) beam.Impulse.sprite = impulseFrames[impulseFrame];

                    var bodyFrame = beam.BodyFrame = (int)(beam.Clock / BodyFrame);
                    beam.Body.sprite = bodyFrames[bodyFrame % bodyFrames.Length];
                    beam.Wedge.sprite = wedgeFrames[bodyFrame % wedgeFrames.Length];
                    beam.Tip.sprite = tipFrames[(int)(beam.Clock / TipFrame) % tipFrames.Length];
                    Shape(beam);
                    // Flicker in whole frames, so the pixels stay crisp.
                    var flicker = (int)(beam.Clock / FlickerFrame);
                    var tint = new Color(1f, 1f, 1f, Mathf.Lerp(MinAlpha, 1f, Mathf.Abs(Mathf.Sin(flicker * 2.399f))));
                    beam.Body.color = tint;
                    beam.Wedge.color = tint;
                    beam.Fade.color = tint;
                    Splinter(beam, step, bodyFrame);
                    foreach (var impactKey in beam.Impacts.Keys.ToArray())
                    {
                        var (renderer, age) = beam.Impacts[impactKey];
                        if (!renderer) { beam.Impacts.Remove(impactKey); continue; }
                        age += step;
                        var frame = (int)(age / ImpactFrame);
                        renderer.enabled = frame < impactFrames.Length;
                        if (renderer.enabled) renderer.sprite = impactFrames[frame];
                        beam.Impacts[impactKey] = (renderer, age);
                    }
                }
                yield return null;
            }
        }
        finally
        {
            animator = null;
        }
    }

    private static bool Load()
    {
        if (loadTried) return loaded;
        loadTried = true;
        try
        {
            (circleRims, circleInners) = SplitCircle("Zoltraak_Zauberkreis_32x32_5Bilder.png", 32, 5);
            impulseFrames = Strip("Zoltraak_Castimpuls_48x48_8Bilder.png", 48, 48, 8, new Vector2(.5f, .5f));
            var segment = LoadTexture(SegmentFile, SegmentWidth * SegmentFrames, SegmentHeight);
            bodyFrames = Frames(segment, SegmentWidth, SegmentHeight, SegmentFrames, new Vector2(0f, .5f));
            wedgeFrames = TaperFrames(segment, SegmentWidth, SegmentHeight, SegmentFrames);
            fadeTexture = FadeTexture(segment);
            tipFrames = Strip(TipFile, TipSize, TipSize, TipFrameCount, new Vector2(.5f, .5f));
            ringFrames = Strip("Zoltraak_Endring_8x20_4Bilder.png", 8, 20, 4, new Vector2(.5f, .5f));
            impactFrames = Strip("Zoltraak_Einschlag_64x64_4Bilder.png", 64, 64, 4, new Vector2(.5f, .5f));
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader) material = new Material(shader) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            loaded = true;
            DelversHost.Info("FRIEREN_ZOLTRAAK_BEAM_READY unlit=" + (material != null) + " circle=turning");
        }
        catch (Exception ex)
        {
            DelversHost.Warning("FRIEREN_ZOLTRAAK_BEAM_DISABLED: " + ex.Message);
        }
        return loaded;
    }

    private static Texture2D LoadTexture(string file, int width, int height)
    {
        var path = Path.Combine(PortraitAssets.AssetFolder, file);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false) || texture.width != width || texture.height != height)
            throw new InvalidDataException($"{file}: erwartet {width}x{height}");
        return Prepare(texture);
    }

    private static Texture2D Prepare(Texture2D texture)
    {
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return texture;
    }

    // Horizontal PNG strip -> frames with point filter at the game's 16 pixels per unit.
    private static Sprite[] Strip(string file, int width, int height, int count, Vector2 pivot)
        => Frames(LoadTexture(file, width * count, height), width, height, count, pivot);

    private static Sprite[] Frames(Texture2D texture, int width, int height, int count, Vector2 pivot)
    {
        var frames = new Sprite[count];
        for (var i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(texture, new Rect(i * width, 0, width, height), pivot, PixelsPerUnit, 0,
                SpriteMeshType.FullRect);
            frames[i].hideFlags = HideFlags.DontUnloadUnusedAsset;
        }
        return frames;
    }

    // The pointed start, per frame the last WedgePixels columns of the segment (the band runs on
    // from there into the tiled body, which starts at column 0) cut to a point at the circle centre.
    private static Sprite[] TaperFrames(Texture2D segment, int width, int height, int count)
    {
        var pixels = segment.GetPixels32();
        var taper = new Il2CppStructArray<Color32>(WedgePixels * count * height);
        for (var frame = 0; frame < count; frame++)
            for (var x = 0; x < WedgePixels; x++)
                for (var y = 0; y < height; y++)
                {
                    if (!ZoltraakTaperRules.Inside(x, y, WedgePixels, height)) continue;
                    taper[y * WedgePixels * count + frame * WedgePixels + x] = pixels[y * width * count + frame * width + width - WedgePixels + x];
                }
        var texture = Prepare(new Texture2D(WedgePixels * count, height, TextureFormat.RGBA32, false));
        texture.SetPixels32(taper);
        texture.Apply();
        return Frames(texture, WedgePixels, height, count, new Vector2(0f, .5f));
    }

    // The fading end: for every segment frame and every column the body can stop at, the next
    // FadePixels columns of the segment (wrapping round), each a step more transparent.
    private static Texture2D FadeTexture(Texture2D segment)
    {
        var pixels = segment.GetPixels32();
        var width = SegmentWidth * FadePixels;
        var fade = new Il2CppStructArray<Color32>(width * SegmentHeight * SegmentFrames);
        for (var frame = 0; frame < SegmentFrames; frame++)
            for (var column = 0; column < SegmentWidth; column++)
                for (var x = 0; x < FadePixels; x++)
                {
                    var source = (column + x) % SegmentWidth;
                    var alpha = 1f - (x + 1) / (float)(FadePixels + 1);
                    for (var y = 0; y < SegmentHeight; y++)
                    {
                        var pixel = pixels[y * SegmentWidth * SegmentFrames + frame * SegmentWidth + source];
                        pixel.a = (byte)(pixel.a * alpha);
                        fade[(frame * SegmentHeight + y) * width + column * FadePixels + x] = pixel;
                    }
                }
        var texture = Prepare(new Texture2D(width, SegmentHeight * SegmentFrames, TextureFormat.RGBA32, false));
        texture.SetPixels32(fade);
        texture.Apply();
        return texture;
    }

    private static Sprite FadeSprite(int frame, int column)
    {
        var index = frame * SegmentWidth + column;
        var sprite = FadeSprites[index];
        if (sprite) return sprite;
        sprite = Sprite.Create(fadeTexture, new Rect(column * FadePixels, frame * SegmentHeight, FadePixels, SegmentHeight),
            new Vector2(0f, .5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return FadeSprites[index] = sprite;
    }

    // The circle strip split by ZoltraakCircleRules into rims that stand and a drawing that turns.
    // Color32 is a blittable struct, so the pixel arrays copy correctly in both loaders.
    private static (Sprite[] Rims, Sprite[] Inners) SplitCircle(string file, int size, int count)
    {
        var source = LoadTexture(file, size * count, size);
        var pixels = source.GetPixels32();
        var rim = new Il2CppStructArray<Color32>(pixels.Length);
        var inner = new Il2CppStructArray<Color32>(pixels.Length);
        var width = size * count;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                if (ZoltraakCircleRules.IsRim(x % size, y, size)) rim[index] = pixels[index];
                else inner[index] = pixels[index];
            }
        var rimTexture = Prepare(new Texture2D(width, size, TextureFormat.RGBA32, false));
        rimTexture.SetPixels32(rim);
        rimTexture.Apply();
        var innerTexture = Prepare(new Texture2D(width, size, TextureFormat.RGBA32, false));
        innerTexture.SetPixels32(inner);
        innerTexture.Apply();
        var center = new Vector2(.5f, .5f);
        return (Frames(rimTexture, size, size, count, center), Frames(innerTexture, size, size, count, center));
    }
}

// Game speed for the beam and the sounds: 0 while paused, 1 at normal speed. The VFX view hears
// every speed change of the campaign (VFXView.ChangeGameSpeed), so the beam
// needs no per-frame hook. A load sends a change when the effective speed changes, and always with the consecration notice
// (same line; whether other pause reasons apply while loading is open there), so every load sets it anew (SyncAfterLoad).
// Both read the campaign's speed, so they should agree whichever comes last (own estimate). Before the first change or load
// it runs at 1.
[HarmonyPatch(typeof(VfxViewType), nameof(VfxViewType.ChangeGameSpeed))]
internal static class ZoltraakGameSpeedPatch
{
    internal static float Multiplier { get; private set; } = 1f;

    private static void Postfix(float mult) => Multiplier = Math.Max(0f, mult);

    // After every campaign load (FrierenSaveMigrations.AfterCampaignLoaded, its own step): the campaign's multiplier of its
    // current speed (ZoltraakLoopRules.AfterLoad). A load whose speed stays sends no ChangeGameSpeed,
    // so the pause's 0 from before a trip through the title would stay. Returns the values for FRIEREN_ZOLTRAAK_SPEED.
    internal static string SyncAfterLoad()
    {
        float? campaign = null;
        var speed = "-";
        try
        {
            var setting = UnityEngine.Object.FindObjectOfType<MainFlow>()?._campaignSetting?.GameSpeedSetting;
            if (setting != null)
            {
                var current = setting.GetSpeed();
                speed = current.ToString();
                campaign = setting.GetMultiplier(current);
            }
        }
        catch (Exception ex) { speed = "error-" + ex.GetType().Name; }
        var before = Multiplier;
        Multiplier = ZoltraakLoopRules.AfterLoad(campaign);
        return FormattableString.Invariant($"multiplier={Multiplier:0.00} before={before:0.00} speed={speed}");
    }
}
