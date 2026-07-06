using System.Collections.Generic;
using HarmonyLib;
using Hospitality;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// Hospitality's Building_GuestBed.GetGizmos() explicitly disables the vanilla
/// "set as medical" toggle. Re-enable it when our mod setting allows guest medical beds.
/// The "disabled" flag lives on the protected Gizmo.disabled field, so it's flipped back
/// via Harmony's Traverse rather than a public setter (there isn't one).
/// </summary>
[HarmonyPatch(typeof(Building_GuestBed), nameof(Building_GuestBed.GetGizmos))]
public static class BuildingGuestBedMedicalPatch
{
    [HarmonyPostfix]
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result)
    {
        var medicalLabel = "CommandBedSetAsMedicalLabel".Translate().ToString();

        foreach (var gizmo in __result)
        {
            if (ModSettings_RoomService.allowGuestMedicalBeds
                && gizmo is Command_Toggle { defaultLabel: not null } toggle
                && toggle.defaultLabel == medicalLabel)
            {
                Traverse.Create(toggle).Field("disabled").SetValue(false);
                toggle.disabledReason = null;
            }

            yield return gizmo;
        }
    }
}
