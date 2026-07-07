using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityRoomService;

public class JobDriver_DeliverMealToGuest : JobDriver
{
    private Thing Food => job.GetTarget(TargetIndex.A).Thing;
    private Pawn Guest => (Pawn)job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(TargetIndex.A), job, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
        this.FailOnDowned(TargetIndex.B);

        yield return Toils_Reserve.Reserve(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);

        var gotoGuest = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        gotoGuest.AddFailCondition(() => !RoomServiceUtility.CanDeliverMeal(pawn, Guest));
        yield return gotoGuest;

        yield return DeliverMeal();
    }

    private Toil DeliverMeal()
    {
        return new Toil
        {
            initAction = () =>
            {
                var guest = Guest;
                var food = Food;
                if (food == null || food.Destroyed || guest == null || !guest.Spawned) return;

                var nutrition = FoodUtility.GetNutrition(guest, food, food.def);
                food.Ingested(guest, nutrition);

                RoomServiceUtility.OnMealDelivered(pawn, guest);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }
}
