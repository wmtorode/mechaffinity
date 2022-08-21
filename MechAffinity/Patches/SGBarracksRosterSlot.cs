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
using MechAffinity.Data;
using SVGImporter;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
    public static class SGBarracksRosterSlot_Refresh_Patch
    {
        public static void Postfix(SGBarracksRosterSlot __instance, UIColorRefTracker ___pilotTypeBackground, SVGImage ___roninIcon, HBSTooltip ___RoninTooltip)
        {
            if (__instance.Pilot == null)
                return;

            HBSTooltip tooltip = __instance.gameObject.GetComponent<HBSTooltip>() ?? __instance.gameObject.AddComponent<HBSTooltip>();

            Pilot pilot = __instance.Pilot;
            string Desc = tooltip.GetText();
            if (String.IsNullOrEmpty(Desc))
            {
                Desc = "";
            }
            
            foreach (PilotTooltipTag pqTag in Main.settings.quirkSettings.tooltipTags)
            {
                if (pilot.pilotDef.PilotTags.Contains(pqTag.tag))
                {
                    Desc += $"{pqTag.tooltipText}\n\n";
                }
            }

            PilotIcon pilotIcon = PilotUiManager.Instance.GetPilotIcon(pilot);

            if (pilotIcon != null)
            {
                if (pilotIcon.HasColour())
                {
                    Main.modLog.LogMessage("Setting Pilot Icon Colour!");
                    ___pilotTypeBackground.SetUIColor(UIColor.Custom);
                    ___pilotTypeBackground.OverrideWithColor(pilotIcon.GetColor());
                }

                if (pilotIcon.HasIcon())
                {
                    Main.modLog.LogMessage("Setting Pilot Icon Image!");
                    SVGAsset svgAsset = PilotUiManager.Instance.GetSvgAsset(pilotIcon.svgAssetId);
                    ___roninIcon.gameObject.SetActive((UnityEngine.Object) svgAsset != (UnityEngine.Object) null);
                    ___roninIcon.vectorGraphics = svgAsset;
                }
                
                if (pilotIcon.HasDescription())
                {
                    Main.modLog.LogMessage("Setting Pilot Icon Description!");
                    BaseDescriptionDef def =
                        UnityGameInstance.BattleTechGame.DataManager.BaseDescriptionDefs
                            .Get(pilotIcon.descriptionDefId);
                    ___RoninTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(def));
                
                }
                
            }

            if (Main.settings.enablePilotQuirks)
            {
                Desc += PilotQuirkManager.Instance.getPilotToolTip(pilot);
            }

            if (Main.settings.enablePilotAffinity)
            {
                Desc += "<b>Pilot Affinities:</b>\n\n";
                Desc += PilotAffinityManager.Instance.getPilotToolTip(pilot);
            }

            var descriptionDef = new BaseDescriptionDef("Tags", pilot.Callsign, Desc, null);
            tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descriptionDef));
        }
    }
}
