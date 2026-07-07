using Hospitality.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityRoomService;

/// <summary>
/// Right-click a guest with a colonist selected to directly queue a solicit attempt, bypassing
/// the WorkGiver's own timing/priority scheduling - mainly useful for testing, but also just a
/// convenient way to make it happen on demand rather than waiting on the AI. Still runs through
/// the same CanSolicit eligibility and the same job/interaction pipeline as the autonomous path,
/// so it's a faithful test of the real mechanic, not a separate shortcut around it.
/// </summary>
public class FloatMenuOptionProvider_SolicitCompanionship : FloatMenuOptionProvider
{
    protected override bool Undrafted => true;
    protected override bool Drafted => false;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
    {
        return pawn.IsGuest() || RoomServiceUtility.IsCompanionshipTrainee(pawn);
    }

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        var pawn = context.FirstSelectedPawn;
        if (pawn == null || pawn == clickedPawn) return null;

        var label = "RoomService_SolicitFloatMenuOption".Translate(clickedPawn.LabelShort);

        var reason = RoomServiceUtility.WhyCannotSolicit(pawn, clickedPawn);
        if (reason != null)
        {
            return new FloatMenuOption(label + " (" + reason + ")", null);
        }

        return new FloatMenuOption(label, () =>
        {
            var job = JobMaker.MakeJob(RoomServiceDefOf.RoomService_SolicitGuest, clickedPawn);
            pawn.jobs.TryTakeOrderedJob(job);
        });
    }
}
