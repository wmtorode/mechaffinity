using System;
using System.Reflection;
using BattleTech;
using BattleTech.UI;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SG_HiringHall_DetailPanel), "DisplayPilot")]
    public static class SG_HiringHall_DetailPanel_DisplayPilot_Patch
    {
        private static string origDesc;
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }

        public static void Prefix(ref bool __runOriginal, Pilot p)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
            origDesc = p.pilotDef.Description.Details;

            //because, #HBSWhy
            if (p.pilotDef.Description.Id.StartsWith("pilot_ronin") || p.pilotDef.Description.Id.StartsWith("pilot_backer"))
            {
                p.pilotDef.Description.Details = origDesc + PilotQuirkManager.Instance.getRoninHiringHallDescription(p);
            }
            else
            {
                p.pilotDef.Description.Details = origDesc + PilotQuirkManager.Instance.getRegularHiringHallDescription(p);
            }

        }

        public static void Postfix(Pilot p)
        {
            p.pilotDef.Description.Details = origDesc;
        }
    }
}
