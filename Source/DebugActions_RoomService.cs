using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace HospitalityRoomService;

public static class DebugActions_RoomService
{
    /// <summary>
    /// One-click stand-in for waiting on the storyteller: fires Hospitality's own "VisitorGroup"
    /// incident directly against the current map, so a fresh batch of guests shows up immediately
    /// for testing companionship/medical treatment without needing to wait or fake a save state.
    /// </summary>
    [DebugAction("Room Service", "Spawn guest group now", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void SpawnGuestGroupNow()
    {
        var map = Find.CurrentMap;
        if (map == null) return;

        var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail("VisitorGroup");
        if (incidentDef == null)
        {
            Messages.Message("Couldn't find Hospitality's VisitorGroup incident - is Hospitality loaded?", MessageTypeDefOf.RejectInput, false);
            return;
        }

        var faction = Find.FactionManager.AllFactions
            .Where(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && !f.defeated && !f.def.hidden
                        && f.def.pawnGroupMakers != null && f.def.pawnGroupMakers.Any(m => m.kindDef == PawnGroupKindDefOf.Peaceful))
            .RandomElementWithFallback();

        if (faction == null)
        {
            Messages.Message("No eligible peaceful faction found to send guests.", MessageTypeDefOf.RejectInput, false);
            return;
        }

        // forced = true skips the incident's normal cooldown/CanFireNow gating - same trick the
        // vanilla dev "do incident" tool uses - so this works even right after a previous visit.
        var parms = new IncidentParms { target = map, faction = faction, forced = true };
        if (!incidentDef.Worker.TryExecute(parms))
        {
            Messages.Message("Failed to spawn a guest group - check the log for details.", MessageTypeDefOf.RejectInput, false);
        }
    }
}
