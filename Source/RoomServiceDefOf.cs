using RimWorld;
using Verse;

namespace HospitalityRoomService;

[DefOf]
public static class RoomServiceDefOf
{
    public static JobDef RoomService_SolicitGuest;
    public static JobDef RoomService_DeliverMeal;
    public static InteractionDef RoomService_SolicitAttempt;
    public static WorkTypeDef RoomService_Companionship;

    public static ThoughtDef RoomService_Accepted;
    public static ThoughtDef RoomService_Declined;
    public static ThoughtDef RoomService_GotPaid;
    public static ThoughtDef RoomService_MealDelivered;
    public static ThoughtDef RoomService_CompanionshipTraining;
    public static ThoughtDef RoomService_PaidForTreatment;
    public static ThoughtDef RoomService_FreeTreatment;
    public static ThoughtDef RoomService_DislikesIt;
    public static ThoughtDef RoomService_LovesIt;
    public static ThoughtDef RoomService_ForcedCompanionship;

    public static HediffDef RoomService_Experience;

    static RoomServiceDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(RoomServiceDefOf));
    }
}
