using System.Linq;
using RimWorld;
using Verse;

namespace HospitalityRoomService;

[StaticConstructorOnStartup]
public static class BedCompInjector
{
    static BedCompInjector()
    {
        // Skip Hospitality's own guest beds - those are rented to visitors, not owned by
        // colonists, so the "entertain guests here" toggle would never apply to them anyway.
        // Also restrict to double-wide beds only: single beds only have one sleeping slot,
        // which caused visible glitches trying to fit both pawns in for the lovin' job.
        var candidates = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => def.building is { bed_humanlike: true }
                          && def.size.x >= 2
                          && !typeof(Hospitality.Building_GuestBed).IsAssignableFrom(def.thingClass));

        foreach (var def in candidates)
        {
            if (def.comps.Any(c => c is CompProperties_SolicitationBed)) continue;
            def.comps.Add(new CompProperties_SolicitationBed());
        }
    }
}
