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

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
    public static class SGBarracksRosterSlot_Refresh_Patch
    {
        public static void Postfix(SGBarracksRosterSlot __instance)
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
                    Main.modLog.Debug?.Write("Setting Pilot Icon Colour!");
                    __instance.pilotTypeBackground.SetUIColor(UIColor.Custom);
                    __instance.pilotTypeBackground.OverrideWithColor(pilotIcon.GetColor());
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
    
    [HarmonyPatch(typeof(SGBarracksRosterSlot), "RefreshCostColorAndAvailability")]
    public static class SGBarracksRosterSlot_RefreshCostColorAndAvailability
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotManagement;
        }
        public static void Prefix(ref bool __runOriginal, SGBarracksRosterSlot __instance)
        {
            
            if (!__runOriginal)
            {
                return;
            }

            __runOriginal = false;
            
            int reputationModifier = __instance.simState.CurSystem.GetPurchaseCostAfterReputationModifier(__instance.simState.GetMechWarriorHiringCost(__instance.pilot.pilotDef));
            __instance.costText.SetText(SimGameState.GetCBillString(reputationModifier), Array.Empty<object>());
            if (reputationModifier <= __instance.simState.Funds)
                __instance.costTextColor.SetUIColor(UIColor.White);
            else
                __instance.costTextColor.SetUIColor(UIColor.Red);
            bool mrbRating = __instance.simState.CanMechWarriorBeHiredAccordingToMRBRating(__instance.pilot);
            bool morale = __instance.simState.CanMechWarriorBeHiredAccordingToMorale(__instance.pilot);
            string notAvailableReason;
            bool available = PilotManagementManager.Instance.IsPilotAvailable(__instance.pilot.pilotDef,
                __instance.simState.CurSystem, __instance.simState, false, true, out notAvailableReason);
            if (!mrbRating || !morale || !available)
            {
                if (!available)
                {
                    var descriptionDef = new BaseDescriptionDef("CantHire", "Pilot Will Not Work For You", notAvailableReason, null);
                    __instance.cantBuyMRBOverlay.SetActive(true);
                    __instance.cantBuyToolTip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descriptionDef));
                }
                else
                {
                    HBSTooltipStateData defaultStateData = new HBSTooltipStateData();
                    if (!mrbRating & morale)
                        defaultStateData.SetContextString("DM.BaseDescriptionDefs[ConceptMechWarriorMRBPTooLow]");
                    else if (mrbRating && !morale)
                        defaultStateData.SetContextString("DM.BaseDescriptionDefs[ConceptMechWarriorMoraleTooLow]");
                    else
                        defaultStateData.SetContextString(
                            "DM.BaseDescriptionDefs[ConceptMechWarriorMRBAndMoraleTooLow]");
                    __instance.cantBuyMRBOverlay.SetActive(true);
                    __instance.cantBuyToolTip.SetDefaultStateData(defaultStateData);
                }
            }
            else
                __instance.cantBuyMRBOverlay.SetActive(false);
            
            
        }
    }
}
