using System;
using BattleTech;
using BattleTech.UI;
using Harmony;
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

        public static void Postfix(LanceConfiguratorPanel __instance, IMechLabDraggableItem item,
            LanceLoadoutSlot[] ___loadoutSlots)
        {
            
            if (UnityGameInstance.BattleTechGame.Simulation == null) return;

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;

            try
            {
                MechDef selectedMech = item.MechDef;

                if (selectedMech == null) return;

                Dictionary<string, SGBarracksRosterSlot> currentRoster =
                    (Dictionary<string, SGBarracksRosterSlot>)Traverse.Create(__instance.pilotListWidget)
                        .Field("currentRoster").GetValue();


                List<SGBarracksRosterSlot> unselectedPilots = new List<SGBarracksRosterSlot>(currentRoster.Values);

                if (Main.settings.pilotUiSettings.enableAffinityColour)
                {
                    // iterate over the pilots not yet selected
                    foreach (var pilotSlot in unselectedPilots)
                    {
                        int deployCount =
                            PilotAffinityManager.Instance.getDeploymentCountWithMech(pilotSlot.Pilot, selectedMech);
                        LocalizableText expertise = (LocalizableText)Traverse.Create(pilotSlot)
                            .Field("expertise").GetValue();
                        PilotUiManager.Instance.AdjustExpertiseTextForAffinity(expertise, deployCount, simGameState.GetPilotFullExpertise(pilotSlot.Pilot));
                    }

                    // now do pilots already assigned to a unit
                    foreach (var pilotSlot in ___loadoutSlots.Where(x => x.SelectedPilot?.Pilot != null)
                                 .Select(x => x.SelectedPilot))
                    {
                        int deployCount =
                            PilotAffinityManager.Instance.getDeploymentCountWithMech(pilotSlot.Pilot, selectedMech);
                        LocalizableText expertise = (LocalizableText)Traverse.Create(pilotSlot)
                            .Field("expertise").GetValue();
                        PilotUiManager.Instance.AdjustExpertiseTextForAffinity(expertise, deployCount, simGameState.GetPilotFullExpertise(pilotSlot.Pilot));
                    }
                }

                if (Main.settings.pilotUiSettings.orderByAffinity)
                {
                    unselectedPilots = unselectedPilots.OrderBy(x => PilotAffinityManager.Instance
                            .getDeploymentCountWithMech(x.Pilot, selectedMech))
                        .ThenByDescending(x => x.Pilot?.Description?.DisplayName).ToList();
                    Traverse.Create(__instance.pilotListWidget).Method("ApplySort", new object[]
                    {
                        unselectedPilots
                    }).GetValue();
                    __instance.pilotListWidget.ForceRefreshImmediate();
                }
            }
            catch (Exception ex)
            {
                Main.modLog.LogException(ex);
            }

        }
    }
}