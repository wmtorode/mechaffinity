using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using MechAffinity;
using MechAffinity.Data;
using SVGImporter;


namespace MechAffinity.Patches;

[HarmonyPatch(typeof(SG_HiringHall_Screen), "CanHireSelectedPilot")]
class SG_HiringHall_Screen_CanHireSelectedPilot
{
    public static bool Prepare()
    {
        return Main.settings.enablePilotManagement;
    }
    
    public static void Postfix(SG_HiringHall_Screen __instance, ref bool __result)
    {
        if (__instance.selectedPilot != null)
        {
            string notAvailableReason;
            __result = __result && PilotManagementManager.Instance.IsPilotAvailable(__instance.selectedPilot.pilotDef,
                __instance.simState.CurSystem, __instance.simState, false, true, out notAvailableReason);
        }
    }
}

[HarmonyPatch(typeof(SG_HiringHall_Screen), "WarningsCheck")]
class SG_HiringHall_Screen_WarningsCheck
{
    public static bool Prepare()
    {
        return Main.settings.enablePilotManagement;
    }
    
    public static void Postfix(SG_HiringHall_Screen __instance)
    {
        if (__instance.selectedPilot != null)
        {
            string notAvailableReason;
            if (!PilotManagementManager.Instance.IsPilotAvailable(__instance.selectedPilot.pilotDef,
                    __instance.simState.CurSystem, __instance.simState, false, true, out notAvailableReason))
            {
                __instance.SetWarningText(notAvailableReason);
                __instance.WarningAreaObject.SetActive(true);
                __instance.HireButton.SetState(ButtonState.Disabled);
            }
        }
    }
}