using BattleTech;
using BattleTech.UI;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using HBS;
using MechAffinity.Data;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(LanceLoadoutSlot), "OnAddItem")]
    public class LanceLoadoutSlot_OnAddItem
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        
        public static bool Prefix(LanceLoadoutSlot __instance, LanceConfiguratorPanel ___LC, IMechLabDraggableItem item, ref bool __result)
        {

            if (___LC == null || !___LC.IsSimGame) return true;
            if (item.ItemType == MechLabDraggableItemType.Pilot)
            {
                List<LanceLoadoutSlot> slots = Traverse.Create(___LC).Field<LanceLoadoutSlot[]>("loadoutSlots").Value.ToList();
                List<Pilot> pilotsInUse = new List<Pilot>();
                SGBarracksRosterSlot barracksRosterSlot = item as SGBarracksRosterSlot;
                pilotsInUse.Add(barracksRosterSlot.Pilot);
                foreach (var slot in slots)
                {
                    if (slot.SelectedPilot != null)
                    {
                        Main.modLog.LogMessage($"Pilot In Slot: {slot.SelectedPilot.Pilot.Callsign}");
                        pilotsInUse.Add(slot.SelectedPilot.Pilot);
                    }
                }

                if (__instance.SelectedPilot != null)
                {
                    pilotsInUse.Remove(__instance.SelectedPilot.Pilot);
                }

                QuirkRestriction restriction = PilotQuirkManager.Instance.pilotRestrictionInEffect(pilotsInUse);
                if (restriction != null)
                {
                    Main.modLog.LogMessage($"preventing Pilot {barracksRosterSlot.Pilot.Callsign} from deploying: {restriction.restrictionCategory} in effect");
                    ___LC.ReturnItem(item);
                    __result = false;
                    GenericPopupBuilder.Create(restriction.errorTitle, restriction.errorMsg).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
                    return false;
                }
            }
            
            return true;
        }
        
    }
}