using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HospitalityRoomService;

/// <summary>
/// Handles the "guest arrives needing a specific operation" event: an arriving guest may
/// already be missing a body part that has a matching bionic/prosthetic install recipe. If so,
/// the player gets an accept/decline choice; accepting queues a normal surgery bill on the
/// guest, which a colonist doctor will pick up once the required part is actually in stock
/// (colony-supplied, same as any other bill). Payment (labor + part reimbursement) is charged
/// once the surgery actually completes - see MedicalGuestSurgeryPaymentPatch.
/// </summary>
public static class MedicalGuestUtility
{
    private static readonly Dictionary<string, string[]> CandidateRecipesByPart = new()
    {
        { "Leg", new[] { "InstallBionicLeg", "InstallSimpleProstheticLeg" } },
        { "Arm", new[] { "InstallBionicArm", "InstallSimpleProstheticArm" } },
        { "Eye", new[] { "InstallBionicEye" } },
        { "Ear", new[] { "InstallBionicEar", "InstallCochlearImplant" } },
        { "Heart", new[] { "InstallBionicHeart", "InstallSimpleProstheticHeart" } },
        { "Jaw", new[] { "InstallBionicJaw" } },
        { "Spine", new[] { "InstallBionicSpine" } },
        { "Stomach", new[] { "InstallBionicStomach" } }
    };

    private static readonly Dictionary<string, RecipeDef> RecipeCache = new();

    private class PendingOperation
    {
        public Pawn Guest;
        public Bill_Medical Bill;
        public RecipeDef Recipe;
        public ThingDef Ingredient;
    }

    private static readonly Dictionary<Pawn, PendingOperation> Pending = new();

    private static RecipeDef GetRecipe(string defName)
    {
        if (RecipeCache.TryGetValue(defName, out var cached)) return cached;
        var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
        RecipeCache[defName] = recipe;
        return recipe;
    }

    public static void TryStartMedicalGuestEvent(Pawn guest)
    {
        if (!ModSettings_RoomService.enableMedicalGuestEvents) return;
        if (guest?.health?.hediffSet == null) return;
        if (Pending.ContainsKey(guest)) return;
        if (Rand.Value > ModSettings_RoomService.medicalGuestChance) return;

        if (!TryFindCandidate(guest, out var recipe, out var part, out var ingredient)) return;

        var text = "RoomService_MedicalGuestOfferText".Translate(guest.LabelShortCap, part.LabelCap, ingredient.LabelCap);
        var title = "RoomService_MedicalGuestOfferTitle".Translate();

        void Accept()
        {
            var bill = (Bill_Medical)HealthCardUtility.CreateSurgeryBill(guest, recipe, part, sendMessages: false);
            guest.BillStack.AddBill(bill);
            Pending[guest] = new PendingOperation { Guest = guest, Bill = bill, Recipe = recipe, Ingredient = ingredient };
        }

        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, Accept, null, false, title));
    }

    private static bool TryFindCandidate(Pawn guest, out RecipeDef recipe, out BodyPartRecord part, out ThingDef ingredient)
    {
        recipe = null;
        part = null;
        ingredient = null;

        var missingParts = guest.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().ToArray();
        foreach (var missing in missingParts)
        {
            var partDefName = missing.Part?.def?.defName;
            if (partDefName == null || !CandidateRecipesByPart.TryGetValue(partDefName, out var candidateNames)) continue;

            foreach (var candidateName in candidateNames)
            {
                var candidateRecipe = GetRecipe(candidateName);
                if (candidateRecipe?.Worker == null) continue;

                var applicableParts = candidateRecipe.Worker.GetPartsToApplyOn(guest, candidateRecipe);
                if (!applicableParts.Contains(missing.Part)) continue;

                var fixedIngredient = candidateRecipe.ingredients?.FirstOrDefault()?.FixedIngredient;
                if (fixedIngredient == null) continue;

                recipe = candidateRecipe;
                part = missing.Part;
                ingredient = fixedIngredient;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Called from MedicalGuestSurgeryPaymentPatch when any surgery finishes. No-ops unless
    /// this specific completed bill is one we're tracking.
    /// </summary>
    public static void Notify_SurgeryApplied(Pawn pawn, Bill bill)
    {
        if (!Pending.TryGetValue(pawn, out var pending) || pending.Bill != bill) return;
        Pending.Remove(pawn);

        var laborFee = ModSettings_RoomService.surgeryLaborFee * ModSettings_RoomService.medicalPriceFactor;
        var partCost = pending.Ingredient.BaseMarketValue * ModSettings_RoomService.partReimbursementFactor;
        var price = Mathf.Max(1, Mathf.RoundToInt(laborFee + partCost));

        var paid = RoomServiceUtility.TakeSilverFromPawn(pawn, price);
        if (paid > 0)
        {
            RoomServiceUtility.DropSilverNear(pawn.MapHeld, pawn.PositionHeld, paid);
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_PaidForTreatment);
            Messages.Message("RoomService_GuestPaidForSurgery".Translate(pawn.LabelShortCap, paid), pawn, MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_FreeTreatment);
        }
    }
}
