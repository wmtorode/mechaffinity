using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using Harmony;
using MechAffinity;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
    public static class SGBarracksRosterSlot_Refresh_Patch
    {
        public static void Postfix(SGBarracksRosterSlot __instance)
        {
            if (__instance.Pilot == null)
                return;

            var tooltip = __instance.gameObject.GetComponent<HBSTooltip>()
                          ?? __instance.gameObject.AddComponent<HBSTooltip>();

            var pilot = __instance.Pilot;
            string Desc = tooltip.GetText();
            if (String.IsNullOrEmpty(Desc))
            {
                Desc = "";
            }

            if (Main.settings.enablePilotQuirks)
            {
                if (pilot.pilotDef.PilotTags.Contains("pilot_fatigued"))
                {
                    Desc +=
                        "<b>***PILOT FATIGUED***</b>\nPilot will suffer from Low Spirits if used in combat. The lance will also experience reduced Resolve per turn during combat.\n\n";
                }
                else if (pilot.pilotDef.PilotTags.Contains("pilot_lightinjury"))
                    Desc +=
                        "<b>***PILOT LIGHT INJURY***</b>\nPilot cannot drop into combat. This pilot requires rest after dropping too frequently while fatigued.\n\n";
            }

            Desc += PilotQuirkManager.Instance.getPilotToolTip(pilot);
            Desc += "<b>Pilot Affinities:</b>\n\n";
            Desc += PilotAffinityManager.Instance.getPilotToolTip(pilot);

            var descriptionDef = new BaseDescriptionDef("Tags", pilot.Callsign, Desc, null);
            tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descriptionDef));
        }
    }
}
