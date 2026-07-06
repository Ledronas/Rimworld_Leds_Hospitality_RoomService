using HarmonyLib;
using Verse;

namespace HospitalityRoomService.HarmonyPatches;

/// <summary>
/// Rolls the "arriving guest needs a specific operation" event right as a visiting guest is
/// spawned onto the map - the single choke point Hospitality uses for every guest, new or
/// returning. Hospitality.Utilities.SpawnGroupUtility is declared internal, so it can't be
/// referenced with typeof() from this assembly - patched by name instead, applied manually
/// from RoomServiceMod's constructor rather than via [HarmonyPatch] + PatchAll().
/// </summary>
public static class MedicalGuestSpawnPatch
{
    public static void Apply(Harmony harmony)
    {
        var type = AccessTools.TypeByName("Hospitality.Utilities.SpawnGroupUtility");
        var method = AccessTools.Method(type, "SpawnVisitor");
        harmony.Patch(method, postfix: new HarmonyMethod(typeof(MedicalGuestSpawnPatch), nameof(Postfix)));
    }

    public static void Postfix(Pawn __result)
    {
        if (__result != null) MedicalGuestUtility.TryStartMedicalGuestEvent(__result);
    }
}
