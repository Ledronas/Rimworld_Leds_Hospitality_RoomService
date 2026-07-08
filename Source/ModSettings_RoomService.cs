using System;
using UnityEngine;
using Verse;

namespace HospitalityRoomService;

public class ModSettings_RoomService : ModSettings
{
    // Medical treatment
    public static bool allowGuestMedicalBeds = true;
    public static bool guestsPayForTreatment = true;
    public static float medicalPriceFactor = 0.55f;
    public static bool chargeForBedOccupancy = true;
    public static float bedOccupancyRatePerDay = 30f;

    // Medical guest events (arriving guest needs a specific operation)
    public static bool enableMedicalGuestEvents = true;
    public static float medicalGuestChance = 0.03f;
    public static float surgeryLaborFee = 200f;
    public static float partReimbursementFactor = 0.6f;

    // Solicitation
    public static bool enableSolicitation = true;
    public static float baseChance = 0.5f;
    public static float socialWeight = 0.5f;
    public static float beautyWeight = 0.3f;
    public static float priceFactorSolicit = 1f;
    public static float roomQualityWeight = 2f;
    public static float bedValueWeight = 0.1f;
    public static float socialXpPerSession = 50f;
    public static float rejectionCooldownHours = 1f;
    public static bool affectsFactionGoodwill = true;
    public static bool personalityReactions = true;
    public static float slaveWillReduction = 1f;

    // Meal delivery
    public static bool enableMealDelivery = true;
    public static float mealDeliveryFee = 5f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref allowGuestMedicalBeds, "allowGuestMedicalBeds", true);
        Scribe_Values.Look(ref guestsPayForTreatment, "guestsPayForTreatment", true);
        Scribe_Values.Look(ref medicalPriceFactor, "medicalPriceFactor", 0.55f);
        Scribe_Values.Look(ref chargeForBedOccupancy, "chargeForBedOccupancy", true);
        Scribe_Values.Look(ref bedOccupancyRatePerDay, "bedOccupancyRatePerDay", 30f);

        Scribe_Values.Look(ref enableMedicalGuestEvents, "enableMedicalGuestEvents", true);
        Scribe_Values.Look(ref medicalGuestChance, "medicalGuestChance", 0.03f);
        Scribe_Values.Look(ref surgeryLaborFee, "surgeryLaborFee", 200f);
        Scribe_Values.Look(ref partReimbursementFactor, "partReimbursementFactor", 0.6f);

        Scribe_Values.Look(ref enableSolicitation, "enableSolicitation", true);
        Scribe_Values.Look(ref baseChance, "baseChance", 0.5f);
        Scribe_Values.Look(ref socialWeight, "socialWeight", 0.5f);
        Scribe_Values.Look(ref beautyWeight, "beautyWeight", 0.3f);
        Scribe_Values.Look(ref priceFactorSolicit, "priceFactorSolicit", 1f);
        Scribe_Values.Look(ref roomQualityWeight, "roomQualityWeight", 2f);
        Scribe_Values.Look(ref bedValueWeight, "bedValueWeight", 0.1f);
        Scribe_Values.Look(ref socialXpPerSession, "socialXpPerSession", 50f);
        Scribe_Values.Look(ref rejectionCooldownHours, "rejectionCooldownHours", 1f);
        Scribe_Values.Look(ref affectsFactionGoodwill, "affectsFactionGoodwill", true);
        Scribe_Values.Look(ref personalityReactions, "personalityReactions", true);
        Scribe_Values.Look(ref slaveWillReduction, "slaveWillReduction", 1f);

        Scribe_Values.Look(ref enableMealDelivery, "enableMealDelivery", true);
        Scribe_Values.Look(ref mealDeliveryFee, "mealDeliveryFee", 5f);
    }

    private Vector2 scrollPosition = Vector2.zero;
    private float viewHeight = 1000f;

    public void DoSettingsWindowContents(Rect inRect)
    {
        // Only "Guest medical treatment" (the very first line) was rendering in-game, with
        // everything after it silently missing and no visible scrollbar - almost certainly an
        // exception partway through this method that Unity's IMGUI swallows without a clean
        // stack trace reaching Player.log on its own. Wrapping the whole thing so the real
        // exception (if any) gets logged explicitly instead of just vanishing.
        try
        {
            DoSettingsWindowContentsInner(inRect);
        }
        catch (Exception e)
        {
            Log.Error($"[RoomService] Exception in mod settings window:\n{e}");
        }
    }

    private void DoSettingsWindowContentsInner(Rect inRect)
    {
        var viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);
        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

        var listing = new Listing_Standard();
        listing.Begin(viewRect);

        listing.Label("RoomService_Settings_MedicalSection".Translate());
        listing.CheckboxLabeled("RoomService_Settings_AllowGuestMedicalBeds".Translate(), ref allowGuestMedicalBeds);
        listing.CheckboxLabeled("RoomService_Settings_GuestsPayForTreatment".Translate(), ref guestsPayForTreatment);
        if (guestsPayForTreatment)
        {
            listing.Label("RoomService_Settings_MedicalPriceFactor".Translate(medicalPriceFactor.ToString("F2")));
            medicalPriceFactor = listing.Slider(medicalPriceFactor, 0f, 2f);

            listing.CheckboxLabeled("RoomService_Settings_ChargeForBedOccupancy".Translate(), ref chargeForBedOccupancy);
            if (chargeForBedOccupancy)
            {
                listing.Label("RoomService_Settings_BedOccupancyRatePerDay".Translate(bedOccupancyRatePerDay.ToString("F0")));
                bedOccupancyRatePerDay = listing.Slider(bedOccupancyRatePerDay, 0f, 200f);
            }
        }

        listing.CheckboxLabeled("RoomService_Settings_EnableMedicalGuestEvents".Translate(), ref enableMedicalGuestEvents);
        if (enableMedicalGuestEvents)
        {
            listing.Label("RoomService_Settings_MedicalGuestChance".Translate(medicalGuestChance.ToString("P1")));
            medicalGuestChance = listing.Slider(medicalGuestChance, 0f, 0.25f);

            listing.Label("RoomService_Settings_SurgeryLaborFee".Translate(surgeryLaborFee.ToString("F0")));
            surgeryLaborFee = listing.Slider(surgeryLaborFee, 0f, 1000f);

            listing.Label("RoomService_Settings_PartReimbursementFactor".Translate(partReimbursementFactor.ToString("F2")));
            partReimbursementFactor = listing.Slider(partReimbursementFactor, 0f, 2f);
        }

        listing.GapLine();

        listing.Label("RoomService_Settings_SolicitationSection".Translate());
        listing.CheckboxLabeled("RoomService_Settings_EnableSolicitation".Translate(), ref enableSolicitation);
        if (enableSolicitation)
        {
            listing.Label("RoomService_Settings_BaseChance".Translate(baseChance.ToString("F2")));
            baseChance = listing.Slider(baseChance, 0f, 1f);

            listing.Label("RoomService_Settings_SocialWeight".Translate(socialWeight.ToString("F2")));
            socialWeight = listing.Slider(socialWeight, 0f, 1f);

            listing.Label("RoomService_Settings_BeautyWeight".Translate(beautyWeight.ToString("F2")));
            beautyWeight = listing.Slider(beautyWeight, 0f, 1f);

            listing.Label("RoomService_Settings_PriceFactorSolicit".Translate(priceFactorSolicit.ToString("F2")));
            priceFactorSolicit = listing.Slider(priceFactorSolicit, 0f, 3f);

            listing.Label("RoomService_Settings_RoomQualityWeight".Translate(roomQualityWeight.ToString("F2")));
            roomQualityWeight = listing.Slider(roomQualityWeight, 0f, 5f);

            listing.Label("RoomService_Settings_BedValueWeight".Translate(bedValueWeight.ToString("F2")));
            bedValueWeight = listing.Slider(bedValueWeight, 0f, 1f);

            listing.Label("RoomService_Settings_SocialXpPerSession".Translate(socialXpPerSession.ToString("F0")));
            socialXpPerSession = listing.Slider(socialXpPerSession, 0f, 200f);

            listing.Label("RoomService_Settings_RejectionCooldownHours".Translate(rejectionCooldownHours.ToString("F1")));
            rejectionCooldownHours = listing.Slider(rejectionCooldownHours, 0f, 12f);

            listing.CheckboxLabeled("RoomService_Settings_AffectsFactionGoodwill".Translate(), ref affectsFactionGoodwill);
            listing.CheckboxLabeled("RoomService_Settings_PersonalityReactions".Translate(), ref personalityReactions);
            if (personalityReactions)
            {
                listing.Label("RoomService_Settings_SlaveWillReduction".Translate(slaveWillReduction.ToString("F1")));
                slaveWillReduction = listing.Slider(slaveWillReduction, 0f, 5f);
            }
        }

        listing.GapLine();

        listing.Label("RoomService_Settings_MealDeliverySection".Translate());
        listing.CheckboxLabeled("RoomService_Settings_EnableMealDelivery".Translate(), ref enableMealDelivery);
        if (enableMealDelivery)
        {
            listing.Label("RoomService_Settings_MealDeliveryFee".Translate(mealDeliveryFee.ToString("F0")));
            mealDeliveryFee = listing.Slider(mealDeliveryFee, 0f, 50f);
        }

        listing.End();
        viewHeight = listing.CurHeight;
        Widgets.EndScrollView();
    }
}
