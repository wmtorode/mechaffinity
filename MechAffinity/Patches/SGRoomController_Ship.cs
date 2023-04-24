using BattleTech;
using BattleTech.UI;
using System;
namespace MechAffinity.Patches;

[HarmonyPatch(typeof(SGRoomController_Ship), "RefreshData")]
public static class SGRoomController_Ship_RefreshData
{
    public static bool Prepare()
    {
        return Main.settings.enablePilotQuirks;
    }

    public static void Prefix(ref bool __runOriginal, SGRoomController_Ship __instance)
    {

        if (!__runOriginal)
        {
            return;
        }
        
        __instance.LocationWidget.RefreshLocation();
        __instance.roomManager.RefreshLeftNavFromShipScreen();
        __instance.TimePlayPause.SetDay(__instance.simState.DaysPassed);
        __instance.TimePlayPause.UpdateLaunchContractButton();
        if (!PilotQuirkManager.Instance.BlockFinanceScreenUpdate)
            __instance.QuarterlyReport.RefreshData(__instance.simState.ExpenditureLevel);
        PilotQuirkManager.Instance.BlockFinanceScreenUpdate = false;

        __runOriginal = false;
    }
}