using BattleTech;
using BattleTech.UI;
using Harmony;
using System.Collections.Generic;
using System.Linq;

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

            if (!___LC.IsSimGame) return true;
            if (item.ItemType == MechLabDraggableItemType.Pilot)
            {
                List<LanceLoadoutSlot> slots = Traverse.Create(___LC).Field<LanceLoadoutSlot[]>("loadoutSlots").Value.ToList();
                foreach (var slot in slots)
                {
                    if (slot.SelectedPilot != null)
                    {
                        Main.modLog.LogMessage($"Pilot In Slot: {slot.SelectedPilot.Pilot.Callsign}");
                    }
                }
            }
            
            return true;
        }
        
    }
}