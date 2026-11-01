extern alias UnityCore;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Audio;
using SpanWrapper = UnityCore::UnityEngine.Bindings.ManagedSpanWrapper;
#if BEPINEX
using GameSfx = global::Refactor.Sound.SFXEnum;
using GameSoundManager = global::Refactor.Sound.SoundManager;
#else
using GameSfx = Il2CppRefactor.Sound.SFXEnum;
using GameSoundManager = Il2CppRefactor.Sound.SoundManager;
#endif

namespace FrierenPortrait;

// Zoltraak's own sounds (ZOLTRAAK-UMSETZUNG.md, Abschnitt 13): WAV files next to the images,
// played through the game's sound effect mixer group, so the game's effect volume, master volume
// and background mute apply. The cast sound plays while Frieren gathers the spell, the shot when
// the beam starts, the beam sound loops until the channel ends. Every sound is built from scratch
// or from CC0 sounds (Frieren\Assets\LIZENZEN.md), nothing is cut from the anime. A missing or
// broken file only means silence.
internal static class ZoltraakSound
{
    internal const string Folder = "Sounds";

    // The sounds a release plays, picked by ear the candidates W1, A1 and D1 under
    // their release names (Frieren\Assets\Sounds). The QA build can swap them from its probe
    // buttons (ZoltraakSoundProbe). ShotGameSound instead plays one of the game's own sounds.
    internal const string ReleaseCastFile = "Zoltraak_Wirken.wav";
    internal const string ReleaseShotFile = "Zoltraak_Abschuss.wav";
    internal const string ReleaseLoopFile = "Zoltraak_Dauerton.wav";
    internal static string CastFile = ReleaseCastFile, ShotFile = ReleaseShotFile, LoopFile = ReleaseLoopFile;
    internal static int? ShotGameSound;

    private const float CastVolume = 0.8f, ShotVolume = 0.9f, LoopVolume = 0.6f;
    private const float LoopFadeIn = 0.2f, FadeOut = 0.08f;
    // The game mixes in 2D; far from the camera our sounds fade out like an off-screen fight.
    private const float NearDistance = 10f, FarDistance = 30f;

    private sealed class Voice
    {
        internal AudioSource Source;
        internal float Volume, FadeIn, Age, FadeLeft = -1f, StopAfter = -1f;
        internal Vector2 Position;
        internal bool Loop, Paused;
        // Only the QA probe buttons play through a pause.
        internal bool IgnorePause;
    }

    private static readonly Dictionary<string, AudioClip> Clips = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Broken = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Voice> Voices = new();
    private static readonly Dictionary<string, Voice> Loops = new(StringComparer.Ordinal);
    private static object runner;

    internal static void Cast(Vector2 position) => Play("cast", CastFile, position, CastVolume, false, 0f);

    internal static void Shot(Vector2 position)
    {
        if (ShotGameSound is { } sound)
        {
            try
            {
                GameSoundManager.Instance?.PlaySFXOneTime((GameSfx)sound, position);
            }
            catch (Exception ex) { DelversHost.Warning("FRIEREN_ZOLTRAAK_SOUND_GAME_FAILED: " + ex.Message); }
            return;
        }
        Play("shot", ShotFile, position, ShotVolume, false, 0f);
    }

    internal static void StartLoop(string id, Vector2 position)
    {
        if (id == null) return;
        StopLoop(id);
        var voice = Play("loop", LoopFile, position, LoopVolume, true, LoopFadeIn);
        if (voice != null) Loops[id] = voice;
    }

    internal static void StopLoop(string id)
    {
        if (id != null && Loops.Remove(id, out var voice) && voice.FadeLeft < 0f) voice.FadeLeft = FadeOut;
    }

    // True when the file loads; used by the self-test and the probe buttons.
    internal static bool Ready(string file) => Clip(file) != null;

    private static string PathOf(string file)
    {
        var inFolder = Path.Combine(PortraitAssets.AssetFolder, Folder, file);
        return File.Exists(inFolder) ? inFolder : Path.Combine(PortraitAssets.AssetFolder, file);
    }

    private static AudioClip Clip(string file)
    {
        if (string.IsNullOrEmpty(file) || Broken.Contains(file)) return null;
        if (Clips.TryGetValue(file, out var cached) && cached) return cached;
        try
        {
            var wav = ZoltraakWavRules.Read(File.ReadAllBytes(PathOf(file)));
            var clip = NewClip("Delvers_" + Path.GetFileNameWithoutExtension(file), wav.Frames, wav.Channels, wav.Rate);
            if (!Fill(clip, wav.Samples)) throw new InvalidOperationException("SetData abgelehnt");
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Clips[file] = clip;
            DelversHost.Info(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "FRIEREN_ZOLTRAAK_SOUND_READY file={0} seconds={1:0.00} channels={2} rate={3}", file, wav.Frames / (float)wav.Rate, wav.Channels, wav.Rate));
            return clip;
        }
        catch (Exception ex)
        {
            Broken.Add(file);
            DelversHost.Warning($"FRIEREN_ZOLTRAAK_SOUND_DISABLED file={file}: {ex.Message}");
            return null;
        }
    }

    // The interop's AudioClip.Create and its managed SetData for float arrays are Unity's own
    // bodies: Create's CreateUserSound turns the clip's name into a ReadOnlySpan<char>, SetData the
    // samples into a span, and both need ReadOnlySpan.GetPinnableReference, which the game's IL2CPP
    // build stripped ("Method not found", Lauf 13 and 14). So the clip is built the way
    // Create does it and name and samples go to the native calls behind it as pinned spans.
    private static AudioClip NewClip(string name, int frames, int channels, int rate)
    {
        var clip = AudioClip.Construct_Internal();
        if (!clip) throw new InvalidOperationException("Construct_Internal lieferte keinen Clip");
        Pinned(name.ToCharArray(), (begin, length) =>
        {
            var raw = new RawSpan { Begin = begin, Length = length };
            AudioClip.CreateUserSound_Injected(clip.m_CachedPtr,
                ref System.Runtime.CompilerServices.Unsafe.As<RawSpan, SpanWrapper>(ref raw), frames, channels, rate, false);
            return true;
        });
        return clip;
    }

    private static bool Fill(AudioClip clip, float[] samples)
        => Pinned(samples, (begin, length) =>
        {
            var raw = new RawSpan { Begin = begin, Length = length };
            return AudioClip.SetData_Injected(clip.m_CachedPtr,
                ref System.Runtime.CompilerServices.Unsafe.As<RawSpan, SpanWrapper>(ref raw), 0);
        });

    private static bool Pinned(Array data, Func<IntPtr, int, bool> use)
    {
        var pin = GCHandle.Alloc(data, GCHandleType.Pinned);
        try { return use(pin.AddrOfPinnedObject(), data.Length); }
        finally { pin.Free(); }
    }

    // The wrapper's fields are read-only and its constructor is a native call, so the span is
    // written into a struct of the same explicit layout (begin at 0, length at 8; Cecil, both
    // loaders' interop) and handed over as the wrapper.
    [StructLayout(LayoutKind.Explicit)]
    private struct RawSpan
    {
        [FieldOffset(0)] internal IntPtr Begin;
        [FieldOffset(8)] internal int Length;
    }

    private static AudioMixerGroup Mixer()
    {
        try { return GameSoundManager.Instance?._oneTimeSFXSoundHandler?._sfxAudioMixerGroup; }
        catch (Exception) { return null; }
    }

    // kind names the part (cast, shot, loop, probe-…) in the log lines.
    private static Voice Play(string kind, string file, Vector2 position, float volume, bool loop, float fadeIn, bool ignorePause = false)
    {
        var clip = Clip(file);
        if (!clip) return null;
        try
        {
            var holder = new GameObject("Delvers_Zoltraak_Sound");
            holder.transform.position = new Vector3(position.x, position.y, 0f);
            var source = holder.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var mixer = Mixer();
            if (mixer) source.outputAudioMixerGroup = mixer;
            var voice = new Voice { Source = source, Volume = volume, FadeIn = fadeIn, Position = position, Loop = loop, IgnorePause = ignorePause };
            source.volume = Level(voice);
            source.Play();
            Voices.Add(voice);
            EnsureRunner();
            return voice;
        }
        catch (Exception ex)
        {
            DelversHost.Warning($"FRIEREN_ZOLTRAAK_SOUND_FAILED kind={kind} file={file}: {ex.Message}");
            return null;
        }
    }


    private static float Level(Voice voice)
    {
        var fadeIn = voice.FadeIn > 0f ? Mathf.Clamp01(voice.Age / voice.FadeIn) : 1f;
        var fadeOut = voice.FadeLeft < 0f ? 1f : Mathf.Clamp01(voice.FadeLeft / FadeOut);
        return voice.Volume * fadeIn * fadeOut * (voice.IgnorePause ? 1f : Distance(voice.Position));
    }

    private static float Distance(Vector2 position)
    {
        var camera = Camera.main;
        if (!camera) return 1f;
        var eye = camera.transform.position;
        var distance = Vector2.Distance(new Vector2(eye.x, eye.y), position);
        return 1f - Mathf.Clamp01((distance - NearDistance) / (FarDistance - NearDistance));
    }

    private static float runnerBeat;

    // The one sound loop (ZoltraakLoopRules): a known loop without a heartbeat is gone and a new one starts, logged.
    private static void EnsureRunner()
    {
        if (ZoltraakLoopRules.Stale(runner != null, Time.unscaledTime, runnerBeat))
        {
            DelversHost.Warning("FRIEREN_ZOLTRAAK_SOUND_RUNNER_RESTART stale=" + (Time.unscaledTime - runnerBeat).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s");
            runner = null;
        }
        if (runner != null) return;
        runnerBeat = Time.unscaledTime;
        runner = DelversHost.StartCoroutine(Run());
    }

    // Holds every voice while the game is paused (game speed 0), fades and removes the ended ones.
    private static IEnumerator Run()
    {
        try
        {
            while (Voices.Count > 0)
            {
                runnerBeat = Time.unscaledTime;
                var speed = ZoltraakGameSpeedPatch.Multiplier;
                for (var i = Voices.Count - 1; i >= 0; i--)
                {
                    var voice = Voices[i];
                    var source = voice.Source;
                    if (!source) { Voices.RemoveAt(i); continue; }
                    var paused = speed <= 0f && !voice.IgnorePause;
                    if (paused != voice.Paused)
                    {
                        if (paused) source.Pause(); else source.UnPause();
                        voice.Paused = paused;
                    }
                    if (!paused)
                    {
                        var step = Time.unscaledDeltaTime * (voice.IgnorePause ? 1f : speed);
                        voice.Age += step;
                        if (voice.FadeLeft >= 0f) voice.FadeLeft = Math.Max(0f, voice.FadeLeft - step);
                        if (voice.StopAfter >= 0f && voice.Age >= voice.StopAfter && voice.FadeLeft < 0f) voice.FadeLeft = FadeOut;
                    }
                    source.volume = Level(voice);
                    var ended = voice.FadeLeft == 0f || (!voice.Loop && !paused && !source.isPlaying);
                    if (!ended) continue;
                    UnityEngine.Object.Destroy(source.gameObject);
                    Voices.RemoveAt(i);
                }
                yield return null;
            }
        }
        finally
        {
            runner = null;
        }
    }

}
