using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace HospitalityRoomService;

/// <summary>
/// Soft integration with the separate "Hospital" mod (Adamas, workshop id 2992224079 - not to be
/// confused with "Hospitality"), if installed. Hospital has its own complete patient-tracking,
/// billing and tending UI; rather than reimplementing any of that, a guest who develops a real
/// disease or injury while visiting gets handed off to become one of Hospital's own tracked
/// patients via its public HospitalMapComponent.PatientArrived(Pawn, PatientData) method. Entirely
/// via reflection against the already-loaded assembly, so there's no compile-time or load-time
/// dependency on that mod at all.
/// </summary>
public static class OptionalHospital
{
    private static bool looked;
    private static bool available;

    private static Type patientTypeEnumType;
    private static MethodInfo mapGetHospitalComponent;
    private static MethodInfo patientArrivedMethod;
    private static MethodInfo isPatientMethod;
    private static ConstructorInfo patientDataCtor;
    private static FieldInfo diagnosisField;
    private static FieldInfo cureField;

    private static void EnsureLookedUp()
    {
        if (looked) return;
        looked = true;

        var hospitalAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Hospital");
        if (hospitalAssembly == null) return;

        var hospitalMapComponentType = hospitalAssembly.GetType("Hospital.HospitalMapComponent");
        var patientDataType = hospitalAssembly.GetType("Hospital.PatientData");
        patientTypeEnumType = hospitalAssembly.GetType("Hospital.PatientType");
        var patientUtilityType = hospitalAssembly.GetType("Hospital.Utilities.PatientUtility");
        if (hospitalMapComponentType == null || patientDataType == null || patientTypeEnumType == null || patientUtilityType == null) return;

        patientArrivedMethod = hospitalMapComponentType.GetMethod("PatientArrived", BindingFlags.Public | BindingFlags.Instance);
        isPatientMethod = patientUtilityType.GetMethod("IsPatient", BindingFlags.Public | BindingFlags.Static);
        patientDataCtor = patientDataType.GetConstructor(new[] { typeof(int), typeof(float), typeof(float), patientTypeEnumType });
        diagnosisField = patientDataType.GetField("Diagnosis");
        cureField = patientDataType.GetField("Cure");
        mapGetHospitalComponent = typeof(Map).GetMethod("GetComponent", Type.EmptyTypes)?.MakeGenericMethod(hospitalMapComponentType);

        available = patientArrivedMethod != null && isPatientMethod != null && patientDataCtor != null
                    && diagnosisField != null && cureField != null && mapGetHospitalComponent != null;
    }

    public static bool IsHospitalPatient(Pawn pawn)
    {
        EnsureLookedUp();
        if (!available) return false;

        var args = new object[] { pawn, null, false };
        return (bool)isPatientMethod.Invoke(null, args);
    }

    /// <summary>
    /// Hands the pawn off to Hospital as a newly arrived patient for the given hediff. No-op
    /// (returns false) if Hospital isn't installed, the pawn isn't spawned on a map, or they're
    /// already one of Hospital's patients.
    /// </summary>
    public static bool TryHandoff(Pawn pawn, Hediff hediff)
    {
        EnsureLookedUp();
        if (!available || pawn?.MapHeld == null || hediff == null) return false;
        if (IsHospitalPatient(pawn)) return false;

        var hospitalComp = mapGetHospitalComponent.Invoke(pawn.MapHeld, null);
        if (hospitalComp == null) return false;

        // Hospital's own PatientType only distinguishes Wounds vs Disease (Test/Surgery are for
        // its own incident-driven patients) - a physical injury maps to Wounds, anything else
        // (illness, infection, etc.) maps to Disease.
        var patientTypeValue = hediff is Hediff_Injury ? 1 : 2;
        var patientType = Enum.ToObject(patientTypeEnumType, patientTypeValue);

        var patientData = patientDataCtor.Invoke(new[] { (object)GenTicks.TicksGame, pawn.MarketValue, pawn.needs?.mood?.CurLevel ?? 0.5f, patientType });
        diagnosisField.SetValue(patientData, hediff.LabelCap);
        cureField.SetValue(patientData, string.Empty);

        patientArrivedMethod.Invoke(hospitalComp, new[] { pawn, patientData });
        return true;
    }
}
