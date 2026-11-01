using System.Collections;
using System.Globalization;
using UnityEngine;

namespace FrierenPortrait;

// Measurement log of Zoltraak (the share per hit is chosen after measuring
// its DPS against a normal group and in a boss fight). Only in Core test campaigns, so players
// never see it. Every line starts with FRIEREN_ZOLTRAAK_ and holds key=value pairs; the Modding
// Toolkit parses them:
//   FRIEREN_ZOLTRAAK_HIT channel= tick= target= kind=unit|building part=body|head|building
//                        result=Normal|Critical|Guard|Miss|Immune damage= head= body= stop= [pass=]
//   FRIEREN_ZOLTRAAK_TARGET channel= target= kind= key= hits= crits= damage= head= body=
//                           building= break= magicResistance=
//   FRIEREN_ZOLTRAAK_CHANNEL channel= user= percent= magicAttack= critChance= ticks= end=
//                            targets= hits= crits= damage= head= body= building= stops= [passes=]
// head and body are the health the hit took, damage is the game's total (shield included),
// building the durability a building lost. stop is why the tick's line ended: range, rock,
// wall or building. pass=summon marks a tick whose line went on through a summon of the
// caster's side, passes counts them per channel; both are left out when
// nothing was passed. end is complete, interrupt-<reason> or abandoned. The CHANNEL line comes
// last. The hits are read from the DamageExecutionLog the game attaches to each damage item
// the channel yields, so no extra hook is needed.
internal static class ZoltraakMeasurement
{
    private const float LogWaitSeconds = 5f;

    private sealed class Hit
    {
        internal int Tick;
        internal IExecutionData Data;
        internal IEntity Target;
        internal string Part;
        internal string Stop;
    }

    private sealed class Channel
    {
        internal int Number;
        internal string User;
        internal float Percent;
        internal float MagicAttack;
        internal float CritChance;
        internal int Ticks;
        internal string End;
        internal readonly List<Hit> Hits = new();
        internal readonly Dictionary<string, int> Stops = new(StringComparer.Ordinal);
        internal readonly Dictionary<int, string> Passes = new();
        internal readonly Dictionary<string, float> MagicResistance = new(StringComparer.Ordinal);
    }

    private sealed class TargetTotal
    {
        internal IEntity Target;
        internal string Kind;
        internal int Hits, Crits;
        internal float Damage, Head, Body, Building;
    }

    private static readonly Dictionary<string, Channel> Open = new(StringComparer.Ordinal);
    private static int channels;

    internal static void Begin(IEntity user, float percent)
    {
        var id = user.Guid.ToString();
        if (Open.TryGetValue(id, out var unfinished)) Close(unfinished, unfinished.Ticks, "abandoned");
        var status = Status(user);
        Open[id] = new Channel
        {
            Number = ++channels,
            User = id,
            Percent = percent,
            MagicAttack = status?.GetValue(StatType.MagicAttackPower) ?? -1f,
            CritChance = status?.GetValue(StatType.CriticalChance) ?? -1f
        };
    }

    internal static void Tick(IEntity user, int tick, string stop, string pass)
    {
        if (!Open.TryGetValue(user.Guid.ToString(), out var channel)) return;
        channel.Ticks = tick + 1;
        channel.Stops[stop] = channel.Stops.TryGetValue(stop, out var count) ? count + 1 : 1;
        if (pass != null) channel.Passes[tick] = pass;
    }

    internal static void Record(IEntity user, int tick, IExecutionData data, IEntity target, string part, string stop)
    {
        if (data == null || target == null || !Open.TryGetValue(user.Guid.ToString(), out var channel)) return;
        channel.Hits.Add(new Hit { Tick = tick, Data = data, Target = target, Part = part, Stop = stop });
        var id = target.Guid.ToString();
        if (!channel.MagicResistance.ContainsKey(id))
            channel.MagicResistance[id] = Status(target)?.GetValue(StatType.MagicResistance) ?? 0f;
    }

    internal static void End(ChannelEnd end)
    {
        if (end?.User != null && Open.TryGetValue(end.User.Guid.ToString(), out var channel))
            Close(channel, end.Ticks, end.Reason);
    }

    // The game fills a damage item's log when it processes the item, after the channel yielded
    // it; the lines are written once every log is there, or after a few seconds.
    private static void Close(Channel channel, int ticks, string end)
    {
        Open.Remove(channel.User);
        channel.Ticks = ticks;
        channel.End = end;
        DelversHost.StartCoroutine(WriteWhenProcessed(channel));
    }

    private static IEnumerator WriteWhenProcessed(Channel channel)
    {
        var deadline = Time.realtimeSinceStartup + LogWaitSeconds;
        while (Time.realtimeSinceStartup < deadline && channel.Hits.Any(hit => Log(hit) == null)) yield return null;
        try { Write(channel); }
        catch (Exception ex) { DelversHost.Warning("FRIEREN_ZOLTRAAK_MEASUREMENT_FAILED: " + ex.Message); }
    }

    private static void Write(Channel channel)
    {
        var c = CultureInfo.InvariantCulture;
        var totals = new Dictionary<string, TargetTotal>(StringComparer.Ordinal);
        foreach (var hit in channel.Hits)
        {
            var id = hit.Target.Guid.ToString();
            if (!totals.TryGetValue(id, out var total))
                totals[id] = total = new TargetTotal { Target = hit.Target, Kind = Kind(hit.Target) };
            var log = Log(hit);
            var result = log != null ? log.ResultType.ToString() : "missing";
            var damage = log?.TotalDamage ?? 0f;
            var head = log != null ? Math.Max(0f, log.TargetHeadHealth_Before - log.TargetHeadHealth_After) : 0f;
            var body = log != null ? Math.Max(0f, log.TargetBodyHealth_Before - log.TargetBodyHealth_After) : 0f;
            total.Hits++;
            if (log != null && log.ResultType == ExecutionDataResult.Critical) total.Crits++;
            total.Damage += damage;
            total.Head += head;
            total.Body += body;
            if (total.Kind == "building") total.Building += damage;
            DelversHost.Info(string.Format(c,
                "FRIEREN_ZOLTRAAK_HIT channel={0} tick={1} target={2} kind={3} part={4} result={5} damage={6:0.##} head={7:0.##} body={8:0.##} stop={9}{10}",
                channel.Number, hit.Tick + 1, id, total.Kind, hit.Part, result, damage, head, body, hit.Stop,
                channel.Passes.TryGetValue(hit.Tick, out var pass) ? " pass=" + pass : string.Empty));
        }
        foreach (var (id, total) in totals)
            DelversHost.Info(string.Format(c,
                "FRIEREN_ZOLTRAAK_TARGET channel={0} target={1} kind={2} key={3} hits={4} crits={5} damage={6:0.##} head={7:0.##} body={8:0.##} building={9:0.##} break={10} magicResistance={11:0.##}",
                channel.Number, id, total.Kind, Key(total.Target), total.Hits, total.Crits, total.Damage, total.Head, total.Body,
                total.Building, BreakStacks(total.Target), channel.MagicResistance.TryGetValue(id, out var resistance) ? resistance : 0f));
        DelversHost.Info(string.Format(c,
            "FRIEREN_ZOLTRAAK_CHANNEL channel={0} user={1} percent={2:0.##} magicAttack={3:0.##} critChance={4:0.##} ticks={5}/{6} end={7} targets={8} hits={9} crits={10} damage={11:0.##} head={12:0.##} body={13:0.##} building={14:0.##} stops={15}{16}",
            channel.Number, channel.User, channel.Percent, channel.MagicAttack, channel.CritChance, channel.Ticks, Zoltraak.Ticks,
            channel.End, totals.Count, totals.Values.Sum(t => t.Hits), totals.Values.Sum(t => t.Crits),
            totals.Values.Sum(t => t.Damage), totals.Values.Sum(t => t.Head), totals.Values.Sum(t => t.Body),
            totals.Values.Sum(t => t.Building),
            channel.Stops.Count == 0 ? "none" : string.Join(",", channel.Stops.Select(stop => stop.Key + ":" + stop.Value)),
            channel.Passes.Count == 0 ? string.Empty
                : " passes=" + string.Join(",", channel.Passes.Values.GroupBy(pass => pass).Select(pass => pass.Key + ":" + pass.Count()))));
    }

    private static DamageExecutionLog Log(Hit hit)
    {
        try { return hit.Data.TryCast<DamageExecutionData>()?.GetLog()?.TryCast<DamageExecutionLog>(); }
        catch (Exception) { return null; }
    }

    // The final values the game calculates with (DataValue.SelfCoef reads StatusReader.GetValue).
    internal static StatusReader Status(IEntity entity)
    {
        var status = EntityComponent._instance?.GetComponentOf<StatusComponent>(entity);
        return status != null ? new StatusReader(status) : null;
    }

    private static string Kind(IEntity target) => target.TryCast<BuildingEntity>() != null ? "building" : "unit";

    private static string Key(IEntity target)
    {
        try
        {
            var building = target.TryCast<BuildingEntity>();
            return (building != null ? building.Data?.Key : target.TryCast<UnitEntity>()?.ProfileKey) ?? "?";
        }
        catch (Exception) { return "?"; }
    }

    private static int BreakStacks(IEntity target)
    {
        try { return EntityComponent._instance?.GetComponentOf<AffecterComponent>(target)?.GetStack(Zoltraak.BreakKey) ?? 0; }
        catch (Exception) { return 0; }
    }
}
