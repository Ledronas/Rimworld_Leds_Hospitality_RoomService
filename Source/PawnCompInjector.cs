using System.Linq;
using Verse;

namespace HospitalityRoomService;

[StaticConstructorOnStartup]
public static class PawnCompInjector
{
    static PawnCompInjector()
    {
        var candidates = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => def.race is { Humanlike: true });

        foreach (var def in candidates)
        {
            if (def.comps.Any(c => c is CompProperties_CompanionshipTrainee)) continue;
            def.comps.Add(new CompProperties_CompanionshipTrainee());
        }
    }
}
