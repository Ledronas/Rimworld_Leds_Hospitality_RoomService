using RimWorld;
using Verse;

namespace HospitalityRoomService;

/// <summary>
/// Traits from other mods that we react to if present, without hard-depending on them.
/// Looked up once, lazily, via GetNamedSilentFail so a missing mod just means no reaction
/// rather than a load error.
/// </summary>
public static class OptionalTraits
{
    private static TraitDef prude;
    private static TraitDef insatiable;
    private static bool looked;

    private static void EnsureLookedUp()
    {
        if (looked) return;
        looked = true;
        // From the "Vanilla Traits Expanded" workshop mod - not a hard dependency.
        prude = DefDatabase<TraitDef>.GetNamedSilentFail("VTE_Prude");
        insatiable = DefDatabase<TraitDef>.GetNamedSilentFail("VTE_Insatiable");
    }

    public static bool DislikesCompanionship(Pawn pawn)
    {
        EnsureLookedUp();
        if (pawn.story?.traits == null) return false;
        if (prude != null && pawn.story.traits.HasTrait(prude)) return true;
        return pawn.story.traits.HasTrait(TraitDefOf.Asexual);
    }

    public static bool LovesCompanionship(Pawn pawn)
    {
        EnsureLookedUp();
        if (pawn.story?.traits == null) return false;
        return insatiable != null && pawn.story.traits.HasTrait(insatiable);
    }
}
