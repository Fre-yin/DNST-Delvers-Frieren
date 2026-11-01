using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
#if BEPINEX
using ClingingVinesSkill = global::Refactor.Combat.SkillRelated.SKILL_ClingingVines;
using ExecutionDataList = Il2CppSystem.Collections.Generic.List<global::Refactor.Combat.IExecutionData>;
using ExecutionDataRoutine = Il2CppSystem.Collections.Generic.IEnumerator<global::Refactor.Combat.IExecutionData>;
using ExecutionDataSequence = Il2CppSystem.Collections.Generic.IEnumerable<global::Refactor.Combat.IExecutionData>;
#else
using ClingingVinesSkill = Il2CppRefactor.Combat.SkillRelated.SKILL_ClingingVines;
using ExecutionDataList = Il2CppSystem.Collections.Generic.List<Il2CppRefactor.Combat.IExecutionData>;
using ExecutionDataRoutine = Il2CppSystem.Collections.Generic.IEnumerator<Il2CppRefactor.Combat.IExecutionData>;
using ExecutionDataSequence = Il2CppSystem.Collections.Generic.IEnumerable<Il2CppRefactor.Combat.IExecutionData>;
#endif

namespace FrierenPortrait;

// A Core building block in waiting (ZOLTRAAK-RECHERCHE.md, "Design Alpha 2"). Nothing here may
// refer to Frieren: Zoltraak is its first user, later it moves to Core unchanged. The probes
// (ChannelProbes, VfxProbe) and the tick log exist only in the QA build.
//
// A native skill is a sequence of execution data that the game runs itself. A channel is the
// sequence [tick 0, delay, tick 1, delay...] returned by SkillRoutine. Delegated ticks
// (Execution.Delagate) were called but their data was never applied (probe), so every
// tick runs inside the skill routine. A lazy Il2Cpp enumerator builds each tick only when the game
// reaches it, so each tick finds fresh targets and can stop the channel. If that enumerator cannot
// be registered, the ticks are built at cast time with the targets of that moment.
//
// The skill borrows the class of a native template skill; for registered keys its SkillRoutine is
// replaced, so none of the template's own behaviour remains.
internal sealed class ChannelSkillDefinition
{
    internal string SkillKey;
    internal int Ticks;
    internal float Interval;
    // Builds the execution data of one tick. Targets are the hostiles in the skill's area now.
    internal Func<ChannelTick, IEnumerable<IExecutionData>> Tick;
    // Stops the remaining ticks, for example while the caster is stunned.
    internal Func<IEntity, string> InterruptReason;
    // Optional: the tiles one tick affects, for example a line that stops at walls. Without it
    // the skill's own area decides (GetAffectTargets).
    internal Func<ChannelTick, ChannelArea> Area;
    // Optional: called once when the game builds the skill's routine, before the first tick
    // (Index -1, no targets), for example to start a cast effect.
    internal Action<ChannelTick> Started;
    // Optional: called once when the channel ends, after its last tick or an interrupt.
    internal Action<ChannelEnd> Finished;
}

internal sealed class ChannelTick
{
    internal ActiveSkill_Base Skill;
    internal int Index;
    internal IEntity User;
    internal IInMapDataSupplier InMapData;
    internal SkillExecutionContext Context;
    internal UnityEngine.Vector2Int TargetPosition;
    // The definition's own area of this tick, null when the skill's area decides.
    internal ChannelArea Area;
    internal List<IEntity> Targets;
}

internal sealed class ChannelArea
{
    // Tiles whose hostiles the tick hits.
    internal Il2CppSystem.Collections.Generic.List<UnityEngine.Vector2Int> Tiles;
    // What ended the area, for the log, and the thing there that takes the hit as well
    // (whoever owns it), for example a destructible wall.
    internal string StopReason;
    internal IEntity StopTarget;
    // Why the area went on through a blocked tile, for the log (Zoltraak: summon), else null.
    internal string Pass;
}

internal sealed class ChannelEnd
{
    internal ActiveSkill_Base Skill;
    internal IEntity User;
    internal int Ticks;
    // "complete" or "interrupt-<reason>".
    internal string Reason;
}

internal static class ChannelSkills
{
    private static readonly Dictionary<string, ChannelSkillDefinition> Definitions = new(StringComparer.Ordinal);


    internal static void Register(ChannelSkillDefinition definition)
    {
        if (definition?.SkillKey == null || definition.Tick == null || definition.Ticks < 1 || definition.Interval < 0)
            throw new ArgumentException("Kanalzauber braucht Schlüssel, Takt, mindestens einen Treffer und ein Intervall ab 0.");
        Definitions[definition.SkillKey] = definition;
    }

    internal static bool TryBuildRoutine(ActiveSkill_Base skill, IInMapDataSupplier inMapData, SkillExecutionContext context,
        out ExecutionDataRoutine routine)
    {
        routine = null;
        if (skill == null || !Definitions.TryGetValue(skill.Key, out var definition)) return false;
        if (context?.User == null || inMapData == null) return false;
        Start(definition, skill, inMapData, context);

        if (DelversChannelRoutine.TryCreate(Sequence(definition, skill, inMapData, context, "lazy"), out var lazy))
        {
            routine = lazy;
            return true;
        }
        var items = new ExecutionDataList();
        foreach (var data in Sequence(definition, skill, inMapData, context, "fixed")) items.Add(data);
        routine = items.Cast<ExecutionDataSequence>().GetEnumerator();
        return true;
    }

    private static IEnumerable<IExecutionData> Sequence(ChannelSkillDefinition definition, ActiveSkill_Base skill,
        IInMapDataSupplier inMapData, SkillExecutionContext context, string path)
    {
        var user = context.User;
        for (var index = 0; index < definition.Ticks; index++)
        {
            if (index > 0) yield return Execution.Delay(definition.Interval, false);
            var reason = definition.InterruptReason?.Invoke(user);
            if (reason != null)
            {
                DelversHost.Info($"DELVERS_CHANNEL_INTERRUPTED skill={skill.Key} tick={index + 1}/{definition.Ticks} reason={reason}");
                Finish(definition, skill, user, index, "interrupt-" + reason);
                yield break;
            }

            var tick = new ChannelTick
            {
                Skill = skill,
                Index = index,
                User = user,
                InMapData = inMapData,
                Context = context,
                TargetPosition = TargetPosition(inMapData, user, context)
            };
            tick.Area = definition.Area?.Invoke(tick);
            tick.Targets = tick.Area != null ? Targets(tick.Area, inMapData, user)
                : HostilesInArea(skill, inMapData, user, tick.TargetPosition);
            var count = 0;
            foreach (var data in definition.Tick(tick))
            {
                if (data == null) continue;
                count++;
                yield return data;
            }
        }
        Finish(definition, skill, user, definition.Ticks, "complete");
    }

    // A start callback must never stop the cast.
    private static void Start(ChannelSkillDefinition definition, ActiveSkill_Base skill, IInMapDataSupplier inMapData,
        SkillExecutionContext context)
    {
        if (definition.Started == null) return;
        try
        {
            definition.Started(new ChannelTick
            {
                Skill = skill,
                Index = -1,
                User = context.User,
                InMapData = inMapData,
                Context = context,
                TargetPosition = TargetPosition(inMapData, context.User, context),
                Targets = new List<IEntity>()
            });
        }
        catch (Exception ex) { DelversHost.Warning($"DELVERS_CHANNEL_START_FAILED skill={skill.Key}: {ex.Message}"); }
    }

    // An end callback must never break the game's skill routine.
    private static void Finish(ChannelSkillDefinition definition, ActiveSkill_Base skill, IEntity user, int ticks, string reason)
    {
        if (definition.Finished == null) return;
        try { definition.Finished(new ChannelEnd { Skill = skill, User = user, Ticks = ticks, Reason = reason }); }
        catch (Exception ex) { DelversHost.Warning($"DELVERS_CHANNEL_FINISH_FAILED skill={skill.Key}: {ex.Message}"); }
    }

    // Hostiles on the definition's tiles, each once, plus the thing that stopped the area.
    private static List<IEntity> Targets(ChannelArea area, IInMapDataSupplier inMapData, IEntity user)
    {
        var result = new List<IEntity>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (area.Tiles != null && area.Tiles.Count > 0)
        {
            var found = inMapData.GetAllAttackableEntitiesOnArea(
                area.Tiles.Cast<Il2CppSystem.Collections.Generic.IEnumerable<UnityEngine.Vector2Int>>());
            if (found != null)
                foreach (var target in found)
                    if (target != null && inMapData.IsHostileTo(user.Guid, target.Guid) && seen.Add(target.Guid.ToString()))
                        result.Add(target);
        }
        if (area.StopTarget != null && seen.Add(area.StopTarget.Guid.ToString())) result.Add(area.StopTarget);
        return result;
    }

    private static UnityEngine.Vector2Int TargetPosition(IInMapDataSupplier inMapData, IEntity user, SkillExecutionContext context)
        => context.TargetGrid != null ? context.TargetGrid.Position
            : context.TargetEntity != null ? inMapData.GetLogicalPosition(context.TargetEntity.Guid)
            : inMapData.GetLogicalPosition(user.Guid);

    private static List<IEntity> HostilesInArea(ActiveSkill_Base skill, IInMapDataSupplier inMapData, IEntity user,
        UnityEngine.Vector2Int targetPosition)
    {
        var result = new List<IEntity>();
        var found = skill.GetAffectTargets(inMapData, user, targetPosition);
        if (found == null) return result;
        foreach (var target in found)
            if (target != null && inMapData.IsHostileTo(user.Guid, target.Guid)) result.Add(target);
        return result;
    }
}

// An Il2Cpp IEnumerator<IExecutionData> backed by a managed sequence: the game pulls the next item
// only when it reaches it. Registered at the first cast, never at mod start.
public sealed class DelversChannelRoutine : Il2CppSystem.Object
{
    private static bool registrationTried;
    private static bool registered;

    private IEnumerator<IExecutionData> source;
    private IExecutionData current;

    public DelversChannelRoutine(IntPtr pointer) : base(pointer) { }

    public DelversChannelRoutine() : base(ClassInjector.DerivedConstructorPointer<DelversChannelRoutine>())
        => ClassInjector.DerivedConstructorBody(this);

    [HideFromIl2Cpp]
    internal static bool TryCreate(IEnumerable<IExecutionData> sequence, out ExecutionDataRoutine routine)
    {
        routine = null;
        if (!registrationTried)
        {
            registrationTried = true;
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<DelversChannelRoutine>(new RegisterTypeOptions
                {
                    Interfaces = new[] { typeof(ExecutionDataRoutine), typeof(Il2CppSystem.Collections.IEnumerator), typeof(Il2CppSystem.IDisposable) }
                });
                registered = true;
                DelversHost.Info("DELVERS_CHANNEL_ROUTINE_REGISTERED lazy=true");
            }
            catch (Exception ex)
            {
                DelversHost.Warning("DELVERS_CHANNEL_ROUTINE_REGISTER_FAILED, ticks use the targets at cast time: " + ex.Message);
            }
        }
        if (!registered) return false;
        try
        {
            var instance = new DelversChannelRoutine { source = sequence.GetEnumerator() };
            routine = instance.Cast<ExecutionDataRoutine>();
            return true;
        }
        catch (Exception ex)
        {
            registered = false;
            DelversHost.Warning("DELVERS_CHANNEL_ROUTINE_CREATE_FAILED, ticks use the targets at cast time: " + ex.Message);
            return false;
        }
    }

    public bool MoveNext()
    {
        try
        {
            if (source != null && source.MoveNext())
            {
                current = source.Current;
                return true;
            }
        }
        catch (Exception ex)
        {
            DelversHost.Error("DELVERS_CHANNEL_TICK_FAILED: " + ex);
        }
        current = null;
        source = null;
        return false;
    }

    public IExecutionData Current => current;

    public void Reset() { }

    public void Dispose() => source = null;
}

// The template class of the prototype. In Core this becomes a patch per registered template.
[HarmonyPatch(typeof(ClingingVinesSkill), nameof(ClingingVinesSkill.SkillRoutine))]
internal static class ChannelSkillRoutinePatch
{
    private static bool Prefix(ActiveSkill_Base __instance, IInMapDataSupplier inMapData, SkillExecutionContext context,
        ref ExecutionDataRoutine __result)
    {
        try
        {
            if (!ChannelSkills.TryBuildRoutine(__instance, inMapData, context, out var routine)) return true;
            __result = routine;
            return false;
        }
        catch (Exception ex)
        {
            DelversHost.Error("DELVERS_CHANNEL_ROUTINE_FAILED: " + ex);
            return true;
        }
    }
}
