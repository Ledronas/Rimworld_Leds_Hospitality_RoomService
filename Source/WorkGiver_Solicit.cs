using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityRoomService;

public class WorkGiver_Solicit : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.OnCell;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (!ModSettings_RoomService.enableSolicitation) return true;
        return pawn?.WorkTagIsDisabled(def.workType.workTags) ?? true;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var guest = t as Pawn;
        return guest != null && RoomServiceUtility.CanSolicit(pawn, guest);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var guest = (Pawn)t;
        RoomServiceUtility.MarkAttempted(pawn, guest);
        return JobMaker.MakeJob(RoomServiceDefOf.RoomService_SolicitGuest, t);
    }
}
