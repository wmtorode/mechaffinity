using System;
using System.Reflection;
using Harmony;
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

        public static void Prefix(Pilot p)
        {
            origDesc = p.pilotDef.Description.Details;

            //because, #HBSWhy
            if (p.pilotDef.Description.Id.StartsWith("pilot_ronin") || p.pilotDef.Description.Id.StartsWith("pilot_backer"))
            {
                Traverse.Create(p.pilotDef.Description).Property("Details").SetValue(origDesc + PilotQuirkManager.Instance.getRoninHiringHallDescription(p));
            }
            else
            {
                Traverse.Create(p.pilotDef.Description).Property("Details").SetValue(origDesc + PilotQuirkManager.Instance.getRegularHiringHallDescription(p));
            }

        }

        public static void Postfix(Pilot p)
        {
            Traverse.Create(p.pilotDef.Description).Property("Details").SetValue(origDesc);
        }
    }
}
