using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using GuestUtility = Hospitality.Utilities.GuestUtility;

namespace HospitalityRoomService;

public static class RoomServiceUtility
{
    /// <summary>
    /// Prefers the pawn's own bed if it's marked and free; otherwise falls back to the closest
    /// other marked bed on the map that isn't someone else's private room. Lets a bed be
    /// designated for this regardless of who (if anyone) owns it.
    /// </summary>
    public static Building_Bed GetSolicitationBed(Pawn pawn)
    {
        var ownBed = pawn.ownership?.OwnedBed;
        if (ownBed != null && IsQualifyingBed(ownBed, pawn)) return ownBed;

        var map = pawn.MapHeld;
        if (map == null) return null;

        return map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>()
            .Where(bed => bed != ownBed && IsQualifyingBed(bed, pawn))
            .OrderBy(bed => bed.Position.DistanceToSquared(pawn.Position))
            .FirstOrDefault();
    }

    private static bool IsQualifyingBed(Building_Bed bed, Pawn pawn)
    {
        if (bed.SleepingSlotsCount < 2) return false; // needs room for both pawns - see BedCompInjector
        var comp = bed.TryGetComp<CompSolicitationBed>();
        if (comp is not { enabled: true }) return false;
        if (bed.CurOccupants.Any()) return false;

        // Unowned beds are fair game for anyone; a bed owned by someone else is their private
        // room and stays off-limits unless the pawn using it is the sole owner.
        var owners = bed.OwnersForReading;
        if (owners.Count > 1) return false;
        if (owners.Count == 1 && owners[0] != pawn) return false;

        return true;
    }

    public static bool CanSolicit(Pawn pawn, Pawn guest)
    {
        return WhyCannotSolicit(pawn, guest) == null;
    }

    /// <summary>
    /// Same eligibility chain as CanSolicit, but returns the specific reason for the first
    /// failing check (null if eligible) - used by the debug float menu option so "not eligible"
    /// actually says why, instead of leaving you to guess which of a dozen conditions failed.
    /// </summary>
    public static string WhyCannotSolicit(Pawn pawn, Pawn guest)
    {
        if (!ModSettings_RoomService.enableSolicitation) return "RoomService_Reason_FeatureDisabled".Translate();
        if (pawn == null || guest == null || pawn == guest) return "RoomService_Reason_InvalidPawns".Translate();
        if (!pawn.IsColonist) return "RoomService_Reason_NotColonist".Translate();
        if (pawn.WorkTagIsDisabled(RoomServiceDefOf.RoomService_Companionship.workTags)) return "RoomService_Reason_WorkTagDisabled".Translate();
        // Pawn_RelationsTracker.MinLovinAge is only 16 - that's vanilla's own threshold for the
        // Lovin' job itself, not a stand-in for "is this pawn an adult". Gate on the actual adult
        // life stage instead, so nobody below 18 is ever a valid initiator or target.
        if (!pawn.DevelopmentalStage.Adult()) return "RoomService_Reason_InitiatorTooYoung".Translate();
        if (!guest.DevelopmentalStage.Adult()) return "RoomService_Reason_GuestTooYoung".Translate();
        var guestReason = WhyGuestNotViable(guest, IsCompanionshipTrainee(guest));
        if (guestReason != null) return guestReason;
        if (guest.relations.OpinionOf(pawn) <= -10) return "RoomService_Reason_OpinionTooLow".Translate();
        if (!SocialInteractionUtility.CanInitiateInteraction(pawn)) return "RoomService_Reason_CannotInitiate".Translate();
        if (!SocialInteractionUtility.CanReceiveInteraction(guest)) return "RoomService_Reason_CannotReceive".Translate();
        if (!RelationsUtility.AttractedToGender(pawn, guest.gender)) return "RoomService_Reason_NotAttractedToGuest".Translate();
        if (!RelationsUtility.AttractedToGender(guest, pawn.gender)) return "RoomService_Reason_GuestNotAttracted".Translate();

        // Respect whichever "Lovin'" precept each pawn's own ideoligion has selected, the same
        // way vanilla gates real lovin' with a non-spouse - a strict monogamy precept makes a
        // married (or just principled) pawn unwilling regardless of which bed is used, while a
        // free-love ideo (or no ideo at all) doesn't restrict it.
        if (ModsConfig.IdeologyActive)
        {
            if (pawn.Ideo != null && !pawn.Ideo.MemberWillingToDo(new HistoryEvent(HistoryEventDefOf.SharedBed_NonSpouse, pawn.Named(HistoryEventArgsNames.Doer))))
            {
                return "RoomService_Reason_IdeoUnwilling".Translate();
            }

            if (guest.Ideo != null && !guest.Ideo.MemberWillingToDo(new HistoryEvent(HistoryEventDefOf.SharedBed_NonSpouse, guest.Named(HistoryEventArgsNames.Doer))))
            {
                return "RoomService_Reason_GuestIdeoUnwilling".Translate();
            }
        }

        var bed = GetSolicitationBed(pawn);
        if (bed == null) return "RoomService_Reason_NoBed".Translate();

        var comp = bed.TryGetComp<CompSolicitationBed>();
        if (comp != null && comp.IsOnCooldownWith(guest)) return "RoomService_Reason_OnCooldown".Translate();

        if (!pawn.HasReserved(guest) && !pawn.CanReserveAndReach(guest, PathEndMode.OnCell, pawn.NormalMaxDanger())) return "RoomService_Reason_CannotReachGuest".Translate();
        if (!pawn.HasReserved(bed) && !pawn.CanReserveAndReach(bed, PathEndMode.OnCell, pawn.NormalMaxDanger())) return "RoomService_Reason_CannotReachBed".Translate();

        return null;
    }

    /// <summary>
    /// A slave flagged for companionship training (see CompCompanionshipTrainee, toggled from
    /// the Slave tab) is a valid target alongside actual Hospitality guests - a colony pawn, not
    /// a visitor, so it obviously can't pass IsArrivedGuest, but every other liveness check below
    /// still applies to them the same way.
    /// </summary>
    public static bool IsCompanionshipTrainee(Pawn pawn)
    {
        return ModSettings_RoomService.enableSlaveCompanionshipTraining
               && pawn.IsSlaveOfColony
               && pawn.TryGetComp<CompCompanionshipTrainee>() is { trainingEnabled: true };
    }

    /// <summary>
    /// Breaks GuestUtility.ViableGuestTarget's checks out individually so the debug float menu
    /// can say specifically why (busy job, eating, tired, etc.) instead of just "not viable" -
    /// two of the original checks are private in Hospitality's own code, so those two are
    /// reproduced here directly against the same public vanilla members they use internally.
    /// </summary>
    private static string WhyGuestNotViable(Pawn guest, bool isTrainingSlave)
    {
        if (!isTrainingSlave && !GuestUtility.IsArrivedGuest(guest, out _)) return "RoomService_Reason_GuestNotArrived".Translate();
        if (guest.Downed) return "RoomService_Reason_GuestDowned".Translate();
        if (!guest.Awake()) return "RoomService_Reason_GuestAsleep".Translate();
        if (GuestUtility.HasDismissiveThought(guest)) return "RoomService_Reason_GuestDismissive".Translate();
        if (GuestUtility.IsInTherapy(guest)) return "RoomService_Reason_GuestInTherapy".Translate();
        if (GuestUtility.IsTired(guest)) return "RoomService_Reason_GuestTired".Translate();
        if (guest.CurJobDef == JobDefOf.Ingest) return "RoomService_Reason_GuestEating".Translate();
        if (guest.CurJob?.def.casualInterruptible == false) return "RoomService_Reason_GuestBusy".Translate();

        return null;
    }

    /// <summary>
    /// Marks a guest as "just attempted" the moment a solicit job is handed out, before we
    /// know whether the job will even get as far as playing the interaction. Without this,
    /// a job that dies immediately after starting (e.g. a reservation conflict, or the guest
    /// becoming non-interruptible right as the pawn arrives) leaves CanSolicit still true, so
    /// the WorkGiver re-offers the exact same guest instantly - producing a same-tick
    /// StartJob loop ("pawn started 10 jobs in one tick"). This only needs to survive a
    /// handful of ticks to break that loop - it's deliberately much shorter than the real
    /// "just got rejected" cooldown (applied in TrySolicit/Fail), so a guest who walks off or
    /// becomes busy before the pawn ever gets a chance to actually proposition them isn't stuck
    /// on an hour-long cooldown with no visible interaction and no mood consequence to show for it.
    /// </summary>
    public static void MarkAttempted(Pawn pawn, Pawn guest)
    {
        const int attemptCooldownTicks = 300; // ~5 seconds - just enough to break a same-tick retry loop
        var bed = GetSolicitationBed(pawn);
        bed?.TryGetComp<CompSolicitationBed>()?.ApplyCooldown(guest, attemptCooldownTicks);
    }

    public static float ComputeChance(Pawn pawn, Pawn guest)
    {
        var chance = ModSettings_RoomService.baseChance;

        var socialLevel = pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
        chance += socialLevel / 20f * ModSettings_RoomService.socialWeight;

        var beauty = pawn.GetStatValue(StatDefOf.PawnBeauty);
        chance += Mathf.Clamp(beauty / 2f, -1f, 1f) * ModSettings_RoomService.beautyWeight;

        chance += guest.relations.OpinionOf(pawn) * 0.01f;

        if (ModSettings_RoomService.personalityReactions)
        {
            // The more comfortable a pawn has become with this work, the better they are at it.
            var experience = pawn.health.hediffSet.GetFirstHediffOfDef(RoomServiceDefOf.RoomService_Experience);
            if (experience != null) chance += experience.Severity * 0.2f;

            if (OptionalTraits.DislikesCompanionship(pawn)) chance -= 0.2f;
            if (OptionalTraits.LovesCompanionship(pawn)) chance += 0.2f;
        }

        return Mathf.Clamp01(chance);
    }

    public static void TrySolicit(Pawn pawn, Pawn guest)
    {
        var bed = GetSolicitationBed(pawn);
        var comp = bed?.TryGetComp<CompSolicitationBed>();

        if (bed == null || !CanSolicit(pawn, guest))
        {
            return;
        }

        var chance = ComputeChance(pawn, guest);

        if (Rand.Value <= chance)
        {
            Succeed(pawn, guest, bed);
            comp?.ClearCooldown(guest);
        }
        else
        {
            Fail(pawn, guest);
            comp?.ApplyCooldown(guest, Mathf.RoundToInt(GenDate.TicksPerHour * ModSettings_RoomService.rejectionCooldownHours));
        }
    }

    private static void Succeed(Pawn pawn, Pawn guest, Building_Bed bed)
    {
        // Grant a temporary Lover relation so vanilla's bed-sharing/JobDriver_Lovin checks
        // accept the pairing (RestUtility.BedOwnerWillShare gates on an existing love-partner
        // relation). Removed again once the lovin' job ends, so this stays a one-off
        // transaction rather than a lasting relationship.
        var grantedTempRelation = false;
        if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, guest))
        {
            pawn.relations.AddDirectRelation(PawnRelationDefOf.Lover, guest);
            grantedTempRelation = true;
        }

        // Vanilla auto-reassigns bed ownership to "partners" sharing a bed, which would
        // otherwise silently evict a guest from a bed they already paid Hospitality to rent -
        // remember it so it can be restored once the encounter is over.
        var guestPreviousBed = guest.ownership?.OwnedBed;

        var job = JobMaker.MakeJob(JobDefOf.Lovin, guest, bed);
        pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: false);

        if (pawn.jobs.curJob == job && pawn.jobs.curDriver != null)
        {
            // Wait until the pawns actually finish the lovin' job (i.e. they made it to the
            // bed and went through with it) before charging payment or granting thoughts -
            // otherwise a job that gets interrupted partway (bed taken, pawn drafted, etc.)
            // would still have already been "paid for", which felt disconnected and glitchy.
            pawn.jobs.curDriver.AddFinishAction(condition => OnLovinFinished(pawn, guest, bed, condition, grantedTempRelation, guestPreviousBed));
        }
        else if (grantedTempRelation)
        {
            // Job didn't actually start (reservation lost the race, etc.) - don't leave a dangling relation.
            RemoveTempLoverRelation(pawn, guest);
        }
    }

    private static void OnLovinFinished(Pawn pawn, Pawn guest, Building_Bed bed, JobCondition condition, bool grantedTempRelation, Building_Bed guestPreviousBed)
    {
        if (grantedTempRelation) RemoveTempLoverRelation(pawn, guest);

        // Vanilla treats the (temporary) Lover relation as a real pairing and auto-assigns bed
        // ownership to partners who share a bed. That leaves the guest "owning" the colonist's
        // bed afterward, which our own bed-quality checks then treat as a second owner - making
        // the bed ineligible for anyone else and effectively locking the colonist into only ever
        // being able to solicit that one guest again. This is a one-off transaction, not a
        // move-in, so undo the claim immediately.
        if (guest.ownership?.OwnedBed == bed)
        {
            guest.ownership.UnclaimBed();
        }

        // Restore whatever bed the guest had before (e.g. a Hospitality guest bed they already
        // paid rent for) - otherwise the same vanilla "one owned bed at a time" reshuffle above
        // leaves them without one, and Hospitality makes them buy it all over again. Re-claim it
        // through Building_GuestBed's own path (not just vanilla ownership) so its rental stats
        // and assignment tracking stay consistent, matching however they originally claimed it.
        if (guestPreviousBed != null && guestPreviousBed.Spawned && guest.ownership?.OwnedBed != guestPreviousBed)
        {
            if (guestPreviousBed is Hospitality.Building_GuestBed guestBed)
            {
                guestBed.TryClaimBed(guest);
            }
            else
            {
                guest.ownership?.ClaimBedIfNonMedical(guestPreviousBed);
            }
        }

        if (condition != JobCondition.Succeeded) return; // interrupted/errored partway - no payment, no thoughts

        if (IsCompanionshipTrainee(guest))
        {
            // A slave in training isn't a paying customer - no silver, no faction goodwill,
            // just the deliberate training payoff (their own Social XP and a suppression boost).
            ApplyTrainingOutcome(pawn, guest);
        }
        else
        {
            var room = RegionAndRoomQuery.GetRoom(bed, RegionType.Set_Passable);
            var roomBonus = (room?.GetStat(RoomStatDefOf.Impressiveness) ?? 0f) * ModSettings_RoomService.roomQualityWeight;
            var bedBonus = bed.MarketValue * ModSettings_RoomService.bedValueWeight;

            var basePrice = guest.GetStatValue(StatDefOf.PawnBeauty) * 10 + pawn.skills.GetSkill(SkillDefOf.Social).Level * 5 + roomBonus + bedBonus;
            var price = Mathf.Max(1, Mathf.RoundToInt(basePrice * ModSettings_RoomService.priceFactorSolicit));
            var paid = TakeSilverFromPawn(guest, price);
            if (paid > 0)
            {
                DropSilverNear(pawn.MapHeld, bed.Position, paid);
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_GotPaid);
            }

            guest.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_Accepted, pawn);

            if (ModSettings_RoomService.affectsFactionGoodwill && guest.Faction is { IsPlayer: false })
            {
                guest.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 1, canSendMessage: false, canSendHostilityLetter: false);
            }
        }

        // Social is one of the hardest skills to train through normal means, and actually
        // seeing an encounter through is a reasonable thing to practice it on.
        pawn.skills?.Learn(SkillDefOf.Social, ModSettings_RoomService.socialXpPerSession);

        // No-op unless "Intimacy - Friends n' Lovers" is installed.
        OptionalIntimacy.TryGainIntimacy(pawn);
        OptionalIntimacy.TryGainIntimacy(guest);

        if (ModSettings_RoomService.personalityReactions) ApplyPersonalityReaction(pawn);
    }

    private static void ApplyTrainingOutcome(Pawn pawn, Pawn guest)
    {
        guest.skills?.Learn(SkillDefOf.Social, ModSettings_RoomService.slaveTrainingSocialXp);

        var suppression = guest.needs?.TryGetNeed<Need_Suppression>();
        if (suppression != null)
        {
            SlaveRebellionUtility.IncrementSuppression(suppression, pawn, guest, ModSettings_RoomService.slaveTrainingSuppressionBoost);
        }

        guest.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_CompanionshipTraining, pawn);
    }

    /// <summary>
    /// Free colonists and slaves alike slowly build up RoomService_Experience (mood + a small
    /// success chance bonus that grows over time) - it just tracks growing familiarity with the
    /// activity itself. A slave still gets a consistently bad "forced into this" thought every
    /// time on top of that, since they have no say in being assigned the work, and their will
    /// (escape/rebellion chance) is ground down each session. Both sides can also get a
    /// trait-based reaction (Prude/Asexual dislike it, Insatiable loves it) if present.
    /// </summary>
    private static void ApplyPersonalityReaction(Pawn pawn)
    {
        var experience = pawn.health.hediffSet.GetFirstHediffOfDef(RoomServiceDefOf.RoomService_Experience);
        if (experience == null)
        {
            experience = HediffMaker.MakeHediff(RoomServiceDefOf.RoomService_Experience, pawn);
            pawn.health.AddHediff(experience);
        }

        experience.Severity = Mathf.Min(experience.Severity + 0.1f, experience.def.maxSeverity);

        if (pawn.IsSlaveOfColony)
        {
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_ForcedCompanionship);

            if (pawn.guest != null)
            {
                // Being worn down like this makes them more submissive - lower will means a
                // lower chance of the slave attempting to escape or start a rebellion.
                pawn.guest.will = Mathf.Max(0f, pawn.guest.will - ModSettings_RoomService.slaveWillReduction);

                // Fully broken (will gone) but has, despite themselves, come to not mind this
                // one part of it (experience maxed out toward "enthusiastic") - that combination
                // is enough to crack an otherwise-unwavering resistance to being recruited.
                if (!pawn.guest.Recruitable && pawn.guest.will <= 0f && experience.Severity >= 0.8f)
                {
                    pawn.guest.Recruitable = true;
                    Messages.Message("RoomService_BrokeUnwavering".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        if (OptionalTraits.DislikesCompanionship(pawn))
        {
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_DislikesIt);
        }
        else if (OptionalTraits.LovesCompanionship(pawn))
        {
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_LovesIt);
        }
    }

    private static void RemoveTempLoverRelation(Pawn pawn, Pawn guest)
    {
        pawn.relations?.TryRemoveDirectRelation(PawnRelationDefOf.Lover, guest);
    }

    private static void Fail(Pawn pawn, Pawn guest)
    {
        guest.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_Declined, pawn);
    }

    /// <summary>
    /// Gated on the guest actually resting in their room (not just standing around anywhere on
    /// the map) so this reads as room service, not a colonist chasing down every hungry guest.
    /// </summary>
    public static bool CanDeliverMeal(Pawn pawn, Pawn guest)
    {
        if (!ModSettings_RoomService.enableMealDelivery) return false;
        if (pawn == null || guest == null || pawn == guest) return false;
        if (!pawn.IsColonist) return false;
        if (!GuestUtility.IsArrivedGuest(guest, out _)) return false;
        if (guest.Downed) return false;
        if (!RestUtility.InBed(guest)) return false;
        if (guest.needs?.food == null || guest.needs.food.CurCategory < HungerCategory.Hungry) return false;
        if (guest.CurJobDef == JobDefOf.Ingest) return false;
        if (guest.CurJob?.def.casualInterruptible == false) return false;
        if (!pawn.CanReserveAndReach(guest, PathEndMode.Touch, pawn.NormalMaxDanger())) return false;
        return true;
    }

    public static void OnMealDelivered(Pawn pawn, Pawn guest)
    {
        var fee = Mathf.Max(0, Mathf.RoundToInt(ModSettings_RoomService.mealDeliveryFee));
        var paid = TakeSilverFromPawn(guest, fee);
        if (paid > 0)
        {
            DropSilverNear(pawn.MapHeld, pawn.Position, paid);
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_GotPaid);
        }

        guest.needs?.mood?.thoughts?.memories?.TryGainMemory(RoomServiceDefOf.RoomService_MealDelivered, pawn);
    }

    public static int TakeSilverFromPawn(Pawn pawn, int amount)
    {
        if (amount <= 0) return 0;
        var taken = 0;
        var silverStacks = pawn.inventory.innerContainer.Where(t => t.def == ThingDefOf.Silver).ToArray();
        foreach (var stack in silverStacks)
        {
            if (taken >= amount) break;
            var need = amount - taken;
            var take = Mathf.Min(need, stack.stackCount);
            stack.SplitOff(take).Destroy();
            taken += take;
        }

        return taken;
    }

    public static void DropSilverNear(Map map, IntVec3 pos, int amount)
    {
        if (map == null || amount <= 0) return;
        var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
        silver.stackCount = amount;
        GenPlace.TryPlaceThing(silver, pos, map, ThingPlaceMode.Near);
    }
}
