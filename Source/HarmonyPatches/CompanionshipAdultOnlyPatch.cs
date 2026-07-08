using HarmonyLib;
using RimWorld;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// WorkGiver.ShouldSkip (see WorkGiver_Solicit/WorkGiver_DeliverMealToGuest) already stops an
/// underage colonist from ever actually being handed a Companionship job, but it doesn't affect
/// the Work tab - the priority checkbox for an underage colonist still showed as assignable there,
/// same as any other work type, which looked like the age lock hadn't done anything. This patches
/// the actual method the Work tab (and Pawn_WorkSettings.SetPriority) use to decide whether a
/// work type is disabled for a pawn, so it's greyed out there too - the same treatment vanilla
/// already gives children on every other work type.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTypeIsDisabled))]
public static class CompanionshipAdultOnlyPatch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, WorkTypeDef w, ref bool __result)
    {
        if (__result) return; // already disabled for some other reason
        if (w != RoomServiceDefOf.RoomService_Companionship) return;

        // Non-biological pawns (mechanoids, etc.) have no ageTracker at all - WorkTypeIsDisabled
        // gets called for every pawn during Pawn_WorkSettings.EnableAndInitialize(), not just
        // humanlike colonists, and this crashed mech generation entirely for anyone with a mech
        // framework mod installed alongside this one.
        if (__instance.ageTracker == null) return;

        if (__instance.ageTracker.AgeBiologicalYears < RoomServiceUtility.MinAdultAge) __result = true;
    }
}
