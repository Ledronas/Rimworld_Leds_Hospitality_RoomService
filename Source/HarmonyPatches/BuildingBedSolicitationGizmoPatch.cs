using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.GetGizmos))]
public static class BuildingBedSolicitationGizmoPatch
{
    // Drop a PNG at Textures/UI/Commands/RoomService_Solicit.png (mod root) to replace this -
    // falls back to a plain checkbox icon until then. Matches vanilla's own Command icon
    // convention (square, transparent background, 64x64 or 128x128 both work fine).
    private static readonly Texture2D ToggleIcon =
        ContentFinder<Texture2D>.Get("UI/Commands/RoomService_Solicit", false) ?? Widgets.CheckboxOnTex;

    [HarmonyPostfix]
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_Bed __instance)
    {
        foreach (var gizmo in __result)
        {
            yield return gizmo;
        }

        var comp = __instance.TryGetComp<CompSolicitationBed>();
        if (comp == null) yield break;

        // Allow marking it whether it's unowned (a standalone "suite" anyone can use) or owned
        // by exactly one colonist. A bed shared by multiple owners, or owned by a slave/prisoner,
        // isn't offered - those aren't meant to be private companionship rooms.
        var owners = __instance.OwnersForReading;
        if (owners.Count > 1) yield break;
        if (owners.Count == 1 && !owners[0].IsColonist) yield break;

        yield return new Command_Toggle
        {
            defaultLabel = "RoomService_ToggleBedLabel".Translate(),
            defaultDesc = "RoomService_ToggleBedDesc".Translate(),
            icon = ToggleIcon,
            isActive = () => comp.enabled,
            toggleAction = () =>
            {
                comp.enabled = !comp.enabled;
                // Building visuals are baked into batched map-mesh sections for performance,
                // so just changing the color the getter returns doesn't repaint it on its own -
                // this forces the section to redraw with the new sheet color.
                __instance.DirtyMapMesh(__instance.Map);
            }
        };
    }
}
