using System.Collections.Generic;
using HarmonyLib;
using Hospitality.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// Charges a Hospitality guest for tending once a tend job finishes. Never blocks the
/// actual care - if the guest can't pay, they just get treated for free with a thought
/// instead of a bill.
/// </summary>
[HarmonyPatch(typeof(Toils_Tend), nameof(Toils_Tend.FinalizeTend))]
public static class GuestTendPaymentPatch
{
    // Tracks when we last billed each patient for holding a bed, so repeated tend cycles bill
    // incrementally for the stretch since the last charge rather than the whole stay at once.
    private static readonly Dictionary<Pawn, int> lastBedBillTick = new();

    [HarmonyPostfix]
    public static void Postfix(Pawn patient)
    {
        if (patient == null || !patient.IsGuest()) return;
        if (!ModSettings_RoomService.guestsPayForTreatment) return;

        // Flat base cost per tend action, scaled by the price factor setting - simple and
        // predictable rather than trying to price each hediff's severity individually.
        const float baseCostPerTend = 20f;
        var cost = Mathf.Max(1, Mathf.RoundToInt(baseCostPerTend * ModSettings_RoomService.medicalPriceFactor));
        cost += GetBedOccupancyCost(patient);

        var paid = RoomServiceUtility.TakeSilverFromPawn(patient, cost);

        if (paid > 0)
        {
            RoomServiceUtility.DropSilverNear(patient.Map, patient.Position, paid);
            patient.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_PaidForTreatment);
            Messages.Message("RoomService_GuestPaidForTreatment".Translate(patient.LabelShortCap, paid), patient, MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            patient.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_FreeTreatment);
        }
    }

    /// <summary>
    /// Bills for the stretch of time since the patient was last charged while occupying a bed -
    /// they're paying for holding the spot, not just the tending itself. Resets the clock each
    /// time so a long stay bills incrementally across tend cycles instead of all at once at the
    /// end (and instead of quietly piling up unbilled if they're never fully "discharged").
    /// </summary>
    private static int GetBedOccupancyCost(Pawn patient)
    {
        if (!ModSettings_RoomService.chargeForBedOccupancy || !patient.InBed())
        {
            lastBedBillTick.Remove(patient);
            return 0;
        }

        var now = GenTicks.TicksGame;
        if (!lastBedBillTick.TryGetValue(patient, out var lastBillTick))
        {
            lastBedBillTick[patient] = now;
            return 0; // first time seen - nothing owed yet, just start the clock
        }

        var elapsedDays = (now - lastBillTick) / (float)GenDate.TicksPerDay;
        lastBedBillTick[patient] = now;
        return Mathf.RoundToInt(elapsedDays * ModSettings_RoomService.bedOccupancyRatePerDay);
    }
}
