using Verse;

namespace HospitalityRoomService;

/// <summary>
/// Per-pawn opt-in flag, toggled from the Slave tab, marking a colony slave as available for
/// "companionship training" - lets colonists with the Companionship work type target them the
/// same way they'd target a visiting guest, instead of only ever soliciting visitors.
/// </summary>
public class CompCompanionshipTrainee : ThingComp
{
    public bool trainingEnabled;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref trainingEnabled, "roomService_trainingEnabled");
    }
}
