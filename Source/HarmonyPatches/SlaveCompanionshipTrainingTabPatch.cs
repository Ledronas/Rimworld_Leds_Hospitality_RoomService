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
/// Earlier attempt grew the tab's (protected) size.y in a Prefix to make room for the checkbox
/// without overlapping vanilla content - that broke the whole tab's layout/clipping (the outer
/// container that decides the tab's clickable area doesn't agree with a size change made mid
/// FillTab), so this version doesn't touch size at all. It just draws a small opaque-backed
/// checkbox as an overlay in the Postfix - it may sit close to or slightly over existing content,
/// but it can't corrupt vanilla's own layout since nothing about the tab's geometry is modified.
/// </summary>
[HarmonyPatch(typeof(ITab_Pawn_Visitor), "FillTab")]
public static class SlaveCompanionshipTrainingTabPatch
{
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

    [HarmonyPostfix]
    public static void Postfix(ITab_Pawn_Visitor __instance)
    {
        if (!IsApplicable(__instance, out var slave)) return;

        var comp = slave.TryGetComp<CompCompanionshipTrainee>();
        if (comp == null)
        {
            Log.WarningOnce($"[RoomService] {slave.LabelShort} has no CompCompanionshipTrainee - pawn comp injection may have missed this race/def.", slave.thingIDNumber ^ 0x726F6F6D);
            return;
        }

        var tabRect = GetTabRect(__instance);
        var rect = new Rect(tabRect.x + 8f, tabRect.y + 4f, tabRect.width - 16f, 24f);
        Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.6f));
        Widgets.CheckboxLabeled(rect, "RoomService_SlaveTrainingCheckbox".Translate(), ref comp.trainingEnabled);
    }
}
