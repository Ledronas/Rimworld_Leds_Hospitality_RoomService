using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityRoomService;

public class WorkGiver_DeliverMealToGuest : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (!ModSettings_RoomService.enableMealDelivery) return true;
        // Same Companionship work type as soliciting - hard lock at the work-giver level so an
        // underage colonist can't be assigned any duty under it, not just the solicit half.
        if (pawn == null || pawn.ageTracker.AgeBiologicalYears < RoomServiceUtility.MinAdultAge) return true;
        return pawn.WorkTagIsDisabled(def.workType.workTags);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return t is Pawn guest
               && RoomServiceUtility.CanDeliverMeal(pawn, guest)
               && FoodUtility.TryFindBestFoodSourceFor(pawn, guest, false, out _, out _);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var guest = (Pawn)t;
        if (!FoodUtility.TryFindBestFoodSourceFor(pawn, guest, false, out var foodSource, out _)) return null;
        var job = JobMaker.MakeJob(RoomServiceDefOf.RoomService_DeliverMeal, foodSource, guest);
        // Bring one meal, not the whole stack - this is room service, not restocking the guest's pantry.
        job.count = 1;
        return job;
    }
}
