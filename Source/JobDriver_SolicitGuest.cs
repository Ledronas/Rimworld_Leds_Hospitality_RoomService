using System.Collections.Generic;
using Hospitality;
using Hospitality.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace HospitalityRoomService;

public class JobDriver_SolicitGuest : JobDriver_GuestBase
{
    protected override InteractionDef InteractionDef => RoomServiceDefOf.RoomService_SolicitAttempt;

    private Toil TryProposition(Pawn initiator, Pawn guest)
    {
        var toil = new Toil
        {
            initAction = () =>
            {
                if (!RoomServiceUtility.CanSolicit(initiator, guest)) return;
                if (!initiator.CanTalkTo(guest)) return;
                initiator.interactions.TryInteractWith(guest, RoomServiceDefOf.RoomService_SolicitAttempt);
                PawnUtility.ForceWait(guest, 200, initiator);
            },
            socialMode = RandomSocialMode.Off,
            defaultCompleteMode = ToilCompleteMode.Delay,
            defaultDuration = 350
        };
        toil.AddFailCondition(FailCondition);
        return toil;
    }

    protected override IEnumerable<Toil> Perform()
    {
        yield return TryProposition(pawn, Talkee);
    }

    protected override bool FailCondition()
    {
        return base.FailCondition() || !RoomServiceUtility.CanSolicit(pawn, Talkee);
    }
}
