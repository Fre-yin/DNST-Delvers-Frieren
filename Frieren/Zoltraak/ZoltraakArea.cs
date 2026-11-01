using Il2CppInterop.Runtime;
using UnityEngine;

namespace FrierenPortrait;

// Zoltraak, the area: the line the game aims along at its real angle, the trace the channel hits
// along (up to the first enemy or blocked tile) and the tile tests the trace asks.
internal static partial class Zoltraak
{
    // The template's area type Direction snaps the line to 8 directions
    // (GridHelper.FindPositionWithDirection), so Zoltraak could only hit
    // along 8 lines. Its own area keeps the real angle. The game reads it for its own uses
    // (aiming, GetAffectTargets); the channel's hits and the beam use LineArea, which also stops
    // at walls. ActiveSkill_Base.OnInit rebuilds the area from the data, hence also the postfix.
    private static ExecutionArea straightLine;
    private static Func<Vector2Int, Vector2Int, int, Il2CppSystem.Collections.Generic.List<Vector2Int>> straightLineGetter;

    internal static void UseStraightLine(ActiveSkill_Base skill)
    {
        var current = skill?._executionArea;
        if (current == null) return;
        if (straightLine == null)
        {
            var native = current;   // the template's line, still used when there is no direction
            straightLineGetter = (user, target, radius) => StraightLine(user, target, radius) ?? native.GetArea(user, target, radius);
            straightLine = new ExecutionArea
            {
                Type = native.Type,
                AreaGetter = DelegateSupport.ConvertDelegate<
                    Il2CppSystem.Func<Vector2Int, Vector2Int, int, Il2CppSystem.Collections.Generic.List<Vector2Int>>>(straightLineGetter)
            };
            DelversHost.Info("FRIEREN_ZOLTRAAK_AREA line=realAngle fallback=" + native.Type);
        }
        if (current.Pointer != straightLine.Pointer) skill._executionArea = straightLine;
    }

    // The tiles come from ZoltraakLineRules. Vector2Int is a blittable struct in both loaders
    // (two ints, no Il2CppSystem.ValueType wrapper), so List.Add copies it correctly; the
    // struct trap of Lesson 11 only hits structs with reference fields.
    internal static Il2CppSystem.Collections.Generic.List<Vector2Int> StraightLine(Vector2Int from, Vector2Int to, int radius)
    {
        var tiles = ZoltraakLineRules.Tiles(from.x, from.y, to.x, to.y, radius);
        if (tiles == null) return null;
        var line = new Il2CppSystem.Collections.Generic.List<Vector2Int>(tiles.Count);
        foreach (var (x, y) in tiles) line.Add(new Vector2Int(x, y));
        return line;
    }

    // The beam hits everything up to the first blocked tile. A destructible
    // building there (no AFFECTER_IndestructibleBuilding, whoever owns it, like the game's
    // Explosion) takes the hit and holds the beam until it is gone; rock, the map edge and an
    // indestructible building stop it without damage. The same test as the game's own
    // SKILL_ElderGlothCharge (Is2x2BlockedConsideringDestructible, the map edge through its
    // blocked cells, not measured yet). Summons of
    // the caster's side are the one exception (HoldsFriendlySummon).
    private static ChannelArea LineArea(ChannelTick tick)
        => TraceArea(tick.InMapData, tick.InMapData.GetLogicalPosition(tick.User.Guid), tick.TargetPosition, tick.Skill.AreaRadius, tick.User);

    // Also used by the self-test, which checks the line on the real map. With a user the beam ends
    // at the first tile holding a living enemy of hers (no passing through)
    // and goes on through her own and allied summons.
    internal static ChannelArea TraceArea(IInMapDataSupplier inMap, Vector2Int from, Vector2Int to, int radius, IEntity user = null)
    {
        Func<int, int, bool> holdsTarget = null, passes = null;
        if (user != null)
        {
            holdsTarget = (x, y) => HoldsEnemy(inMap, new Vector2Int(x, y), user);
            passes = (x, y) => HoldsFriendlySummon(inMap, new Vector2Int(x, y), user);
        }
        var trace = ZoltraakLineRules.Trace(from.x, from.y, to.x, to.y, radius,
            (x, y) => inMap.IsBlocked(new Vector2Int(x, y)), holdsTarget, passes);
        var area = new ChannelArea { Tiles = new Il2CppSystem.Collections.Generic.List<Vector2Int>(), StopReason = "range" };
        if (trace == null) return area;
        foreach (var (x, y) in trace.Open) area.Tiles.Add(new Vector2Int(x, y));
        if (trace.Passed.Count > 0) area.Pass = ZoltraakLineRules.SummonPass;
        if (trace.Target != null)
        {
            area.StopReason = ZoltraakLineRules.TargetStop;
            return area;
        }
        if (trace.Blocked is not { } blocked) return area;
        var building = BuildingOn(inMap, new Vector2Int(blocked.X, blocked.Y));
        var indestructible = building != null
            && EntityComponent._instance?.GetComponentOf<AffecterComponent>(building)?.Has(IndestructibleBuildingKey) == true;
        area.StopReason = ZoltraakLineRules.StopReason(true, building != null, indestructible);
        if (building != null && !indestructible) area.StopTarget = building;
        return area;
    }

    // A living unit the user is hostile to, the same test the channel uses for its targets.
    private static bool HoldsEnemy(IInMapDataSupplier inMap, Vector2Int tile, IEntity user)
    {
        var entities = inMap.GetAllEntitiesOnPosition(tile);
        if (entities == null) return false;
        foreach (var entity in entities)
        {
            var unit = entity?.TryCast<UnitEntity>();
            if (unit != null && !unit.IsDead && inMap.IsHostileTo(user.Guid, entity.Guid)) return true;
        }
        return false;
    }

    // The caster's own and allied summons let the beam through and take no
    // damage. A skill sets the summoner when it summons a building (Execution.Summon: StrangeSeed,
    // SunHeartBloom, SolarBloom, MucusSprayer, and StoneThrow's PulsatingRock); built buildings,
    // traps, trees and ore have none and keep the rule Enemy summons are hostile to
    // her and still stop the beam and take the hit. Hostile is the game's faction test, the one
    // that also guards the player's own attack on a building (BuildingAttackValidator).
    // The summoner comes from Core's DelversSummons; Summoner.HasValue through the
    // interop wrapper reads the wrong bytes.
    private static bool HoldsFriendlySummon(IInMapDataSupplier inMap, Vector2Int tile, IEntity user)
    {
        var entities = inMap.GetAllEntitiesOnPosition(tile);
        if (entities == null) return false;
        foreach (var entity in entities)
        {
            var building = entity?.TryCast<BuildingEntity>();
            if (building != null && !building.IsDead && DelversSummons.SummonerGuid(entity).Length > 0 && !EntityExtensions.IsHostile(user, entity)) // trap-ok: IsHostile: assumes the caster is a player unit and the target a building; whether Frieren ever comes as an enemy is open, then a faction check would be needed
                return true;
        }
        return false;
    }

    private static IEntity BuildingOn(IInMapDataSupplier inMap, Vector2Int tile)
    {
        var entities = inMap.GetAllEntitiesOnPosition(tile);
        if (entities == null) return null;
        foreach (var entity in entities)
        {
            var building = entity?.TryCast<BuildingEntity>();
            if (building != null && !building.IsDead) return entity;
        }
        return null;
    }
}
