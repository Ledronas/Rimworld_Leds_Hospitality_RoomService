using System.Collections.Generic;
using Verse;

namespace HospitalityRoomService;

public class CompSolicitationBed : ThingComp
{
    public bool enabled;
    public Dictionary<Pawn, int> cooldownUntilTick = new();

    // Pawn keys need Reference lookMode, which requires explicit working lists for RimWorld's
    // two-pass save/load (LoadingVars then ResolveCrossRefs) - these are just scratch storage
    // for that process, not meant to be read outside PostExposeData.
    private List<Pawn> cooldownKeysWorkingList;
    private List<int> cooldownValuesWorkingList;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref enabled, "roomService_enabled");
        Scribe_Collections.Look(
            ref cooldownUntilTick, "roomService_cooldowns", LookMode.Reference, LookMode.Value,
            ref cooldownKeysWorkingList, ref cooldownValuesWorkingList);
        cooldownUntilTick ??= new Dictionary<Pawn, int>();
    }

    public bool IsOnCooldownWith(Pawn guest)
    {
        return cooldownUntilTick.TryGetValue(guest, out var tick) && GenTicks.TicksGame < tick;
    }

    public void ApplyCooldown(Pawn guest, int ticks)
    {
        cooldownUntilTick[guest] = GenTicks.TicksGame + ticks;
    }

    public void ClearCooldown(Pawn guest)
    {
        cooldownUntilTick.Remove(guest);
    }
}
