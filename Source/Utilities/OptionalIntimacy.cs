using System.Reflection;
using RimWorld;
using Verse;

namespace HospitalityRoomService;

/// <summary>
/// Soft integration with the "Intimacy - Friends n' Lovers" workshop mod, if installed.
/// It adds its own SEX_Intimacy Need with a public Need_Intimacy.GainIntimacy(float) method,
/// so a completed companionship session can feed that need too - entirely via reflection,
/// so there's no compile-time or load-time dependency on that mod's assembly at all.
/// </summary>
public static class OptionalIntimacy
{
    private const float GainAmount = 0.4f;

    private static NeedDef intimacyNeedDef;
    private static MethodInfo gainIntimacyMethod;
    private static bool looked;

    private static void EnsureLookedUp()
    {
        if (looked) return;
        looked = true;

        intimacyNeedDef = DefDatabase<NeedDef>.GetNamedSilentFail("SEX_Intimacy");
        gainIntimacyMethod = intimacyNeedDef?.needClass?.GetMethod("GainIntimacy", new[] { typeof(float) });
    }

    public static void TryGainIntimacy(Pawn pawn)
    {
        EnsureLookedUp();
        if (intimacyNeedDef == null || gainIntimacyMethod == null) return;

        var need = pawn.needs?.TryGetNeed(intimacyNeedDef);
        if (need == null) return;

        gainIntimacyMethod.Invoke(need, new object[] { GainAmount });
    }
}
