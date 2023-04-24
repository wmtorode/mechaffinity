using System;
using BattleTech;
using BattleTech.UI;
using System.Collections.Generic;
using System.Linq;
using BattleTech.UI.TMProWrapper;
using HBS;
using MechAffinity.Data;


namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(LanceConfiguratorPanel), "OnButtonClicked")]
    public class LanceConfiguratorPanel_OnButtonClicked
    {
        public static bool Prepare()
        {
            return Main.settings.pilotUiSettings.enableAffinityColour || Main.settings.pilotUiSettings.orderByAffinity;
        }

        public static void Postfix(LanceConfiguratorPanel __instance, IMechLabDraggableItem item)
        {

            if (UnityGameInstance.BattleTechGame.Simulation == null) return;

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;

            try
            {
                MechDef selectedMech = item.MechDef;

                if (selectedMech == null) return;

                Dictionary<string, SGBarracksRosterSlot> currentRoster = __instance.pilotListWidget.currentRoster;
                
                List<SGBarracksRosterSlot> unselectedPilots = new List<SGBarracksRosterSlot>(currentRoster.Values);

                if (Main.settings.pilotUiSettings.enableAffinityColour)
                {
                    // iterate over the pilots not yet selected
                    foreach (var pilotSlot in unselectedPilots)
                    {
                        int deployCount =
                            PilotAffinityManager.Instance.getDeploymentCountWithMech(pilotSlot.Pilot, selectedMech);
                        LocalizableText expertise = pilotSlot.expertise;
                        PilotUiManager.Instance.AdjustExpertiseTextForAffinity(expertise, deployCount,
                            simGameState.GetPilotFullExpertise(pilotSlot.Pilot));
                    }

                    // now do pilots already assigned to a unit
                    foreach (var pilotSlot in __instance.loadoutSlots.Where(x => x.SelectedPilot?.Pilot != null)
                                 .Select(x => x.SelectedPilot))
                    {
                        int deployCount =
                            PilotAffinityManager.Instance.getDeploymentCountWithMech(pilotSlot.Pilot, selectedMech);
                        LocalizableText expertise = pilotSlot.expertise;
                        PilotUiManager.Instance.AdjustExpertiseTextForAffinity(expertise, deployCount,
                            simGameState.GetPilotFullExpertise(pilotSlot.Pilot));
                    }
                }

                if (Main.settings.pilotUiSettings.orderByAffinity)
                {
                    unselectedPilots = unselectedPilots.OrderBy(x => PilotAffinityManager.Instance
                            .getDeploymentCountWithMech(x.Pilot, selectedMech))
                        .ThenByDescending(x => x.Pilot?.Description?.DisplayName).ToList();
                    __instance.pilotListWidget.ApplySort(unselectedPilots);
                    __instance.pilotListWidget.ForceRefreshImmediate();
                }
            }
            catch (Exception ex)
            {
                Main.modLog.Error?.Write(ex);
            }

        }
    }

    [HarmonyPatch(typeof(LanceConfiguratorPanel), "ContinueConfirmClicked")]
    public class LanceConfiguratorPanel_ContinueConfirmClicked
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity || Main.settings.enablePilotQuirks;
        }

        public static void Postfix(LanceConfiguratorPanel __instance)
        {
            if (Main.settings.enablePilotAffinity)
            {
                PilotAffinityManager.Instance.ResetEffectCache();
                List<Pilot> pilots = new List<Pilot>();
                foreach (var slot in __instance.loadoutSlots)
                {
                    if (slot.SelectedPilot != null)
                    {
                        pilots.Add(slot.SelectedPilot.pilot);
                    }
                }
                PilotAffinityManager.Instance.AddSharedAffinity(pilots);
            }

            if (Main.settings.enablePilotQuirks)
            {
                PilotQuirkManager.Instance.ResetEffectCache();
            }
        }
    }
}