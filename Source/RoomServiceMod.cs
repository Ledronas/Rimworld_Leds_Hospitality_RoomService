using HarmonyLib;
using HospitalityRoomService.HarmonyPatches;
using UnityEngine;
using Verse;

namespace HospitalityRoomService;

public class RoomServiceMod : Mod
{
    private static ModSettings_RoomService settings;

    public RoomServiceMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<ModSettings_RoomService>();
        var harmony = new Harmony("ledronas.hospitalityroomservice");
        harmony.PatchAll();

        // Patches an internal Hospitality type by name - can't use [HarmonyPatch(typeof(...))].
        MedicalGuestSpawnPatch.Apply(harmony);
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "Hospitality: Room Service";
    }
}
