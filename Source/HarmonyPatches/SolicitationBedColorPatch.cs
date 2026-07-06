using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// Tints the sheets burgundy on beds with "allow entertaining guests" turned on. Patches both
/// DrawColor and DrawColorTwo - an unowned bed apparently falls back to DrawColor for its
/// visible color rather than DrawColorTwo (owned beds showed the tint fine on just the latter,
/// unowned ones didn't), so both are covered the same way Hospitality overrides both on its own
/// guest beds rather than just one.
/// </summary>
public static class SolicitationBedColorPatch
{
    private static readonly Color SheetColor = new(0.5f, 0f, 0.125f); // burgundy

    private static void Apply(Building_Bed instance, ref Color result)
    {
        var comp = instance.TryGetComp<CompSolicitationBed>();
        if (comp is { enabled: true })
        {
            result = SheetColor;
        }
    }

    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.DrawColorTwo), MethodType.Getter)]
    private static class DrawColorTwoPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Building_Bed __instance, ref Color __result) => Apply(__instance, ref __result);
    }

    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.DrawColor), MethodType.Getter)]
    private static class DrawColorPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Building_Bed __instance, ref Color __result) => Apply(__instance, ref __result);
    }
}
