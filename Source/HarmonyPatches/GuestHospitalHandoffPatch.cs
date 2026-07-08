using HarmonyLib;
using Hospitality.Utilities;
using RimWorld;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// When a visiting Hospitality guest develops a real disease or injury - ordinary random events
/// happening to hit them while they're in the colony, not our own scripted medical-guest events -
/// hand them off to the separate "Hospital" mod (if installed) as one of its own tracked patients.
/// Patches both AddHediff overloads since callers use either depending on whether they already
/// have a constructed Hediff instance or just a HediffDef.
/// </summary>
[HarmonyPatch(typeof(Pawn_HealthTracker))]
public static class GuestHospitalHandoffPatch
{
    private static void TryHandoff(Hediff hediff)
    {
        if (!ModSettings_RoomService.enableGuestHospitalHandoff) return;
        if (hediff?.def == null || !hediff.def.isBad || !hediff.def.tendable) return;

        var pawn = hediff.pawn;
        if (pawn == null || !pawn.IsGuest()) return;
        if (Rand.Value > ModSettings_RoomService.guestHospitalHandoffChance) return;

        OptionalHospital.TryHandoff(pawn, hediff);
    }

    [HarmonyPatch(nameof(Pawn_HealthTracker.AddHediff), typeof(HediffDef), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult))]
    private static class AddHediffFromDefPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hediff __result) => TryHandoff(__result);
    }

    [HarmonyPatch(nameof(Pawn_HealthTracker.AddHediff), typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult))]
    private static class AddHediffInstancePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hediff hediff) => TryHandoff(hediff);
    }
}
