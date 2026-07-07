using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// Adds an "allow companionship training" checkbox to the vanilla Slave tab. ITab_Pawn_Guest,
/// ITab_Pawn_Prisoner and ITab_Pawn_Slave all share one base class (ITab_Pawn_Visitor) and its
/// one FillTab() method - there's no slave-only override to hook, so this patches the shared
/// method and self-gates on the tab actually being the Slave tab for a colony slave.
///
/// The Prefix grows the tab's (protected) size.y by a fixed strip before FillTab lays out its
/// own content, so the checkbox drawn in the Postfix lands in genuinely empty space rather than
/// overlapping whatever vanilla already drew.
/// </summary>
[HarmonyPatch(typeof(ITab_Pawn_Visitor), "FillTab")]
public static class SlaveCompanionshipTrainingTabPatch
{
    private const float ExtraHeight = 34f;

    // SelPawn and TabRect are protected on ITab/InspectTabBase, not accessible directly from
    // outside a subclass - go through Traverse instead of casting our way around it.
    private static Pawn GetSelPawn(ITab_Pawn_Visitor instance) => Traverse.Create(instance).Property("SelPawn").GetValue<Pawn>();
    private static Rect GetTabRect(ITab_Pawn_Visitor instance) => Traverse.Create(instance).Property("TabRect").GetValue<Rect>();

    private static bool IsApplicable(ITab_Pawn_Visitor instance, out Pawn slave)
    {
        slave = null;
        if (!ModSettings_RoomService.enableSlaveCompanionshipTraining) return false;
        if (instance is not ITab_Pawn_Slave) return false;

        slave = GetSelPawn(instance);
        return slave is { IsSlaveOfColony: true };
    }

    [HarmonyPrefix]
    public static void Prefix(ITab_Pawn_Visitor __instance)
    {
        if (!IsApplicable(__instance, out _)) return;

        var sizeField = AccessTools.Field(typeof(ITab), "size");
        var size = (Vector2)sizeField.GetValue(__instance);
        sizeField.SetValue(__instance, new Vector2(size.x, size.y + ExtraHeight));
    }

    [HarmonyPostfix]
    public static void Postfix(ITab_Pawn_Visitor __instance)
    {
        if (!IsApplicable(__instance, out var slave)) return;

        var comp = slave.TryGetComp<CompCompanionshipTrainee>();
        if (comp == null) return;

        var tabRect = GetTabRect(__instance);
        var rect = new Rect(tabRect.x + 17f, tabRect.yMax - ExtraHeight, tabRect.width - 34f, 24f);
        Widgets.CheckboxLabeled(rect, "RoomService_SlaveTrainingCheckbox".Translate(), ref comp.trainingEnabled);
    }
}
