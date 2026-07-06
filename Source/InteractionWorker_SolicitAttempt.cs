using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HospitalityRoomService;

public class InteractionWorker_SolicitAttempt : InteractionWorker
{
    public override void Interacted(Pawn initiator, Pawn guest, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
    {
        letterDef = null;
        letterLabel = null;
        letterText = null;
        lookTargets = null;

        RoomServiceUtility.TrySolicit(initiator, guest);
    }
}
