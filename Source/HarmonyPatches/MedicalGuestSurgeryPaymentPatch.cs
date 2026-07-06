using HarmonyLib;
using RimWorld;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// Fires whenever any recipe (including surgery) is applied to any pawn - a no-op unless the
/// pawn/bill combination is one MedicalGuestUtility is tracking from an accepted "guest needs
/// an operation" event, in which case it charges payment.
/// </summary>
[HarmonyPatch(typeof(RecipeWorker), nameof(RecipeWorker.ApplyOnPawn))]
public static class MedicalGuestSurgeryPaymentPatch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, Bill bill)
    {
        MedicalGuestUtility.Notify_SurgeryApplied(pawn, bill);
    }
}
