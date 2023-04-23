using BattleTech;
using BattleTech.UI;
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
        
        public static void Prefix(ref bool __runOriginal, LanceLoadoutSlot __instance, IMechLabDraggableItem item, ref bool __result)
        {
            
            if (!__runOriginal)
            {
                return;
            }

            if (__instance.LC == null || !__instance.LC.IsSimGame)
            {
                return;
            }
            if (item.ItemType == MechLabDraggableItemType.Pilot)
            {
                var slots = __instance.LC.loadoutSlots;
                List<Pilot> pilotsInUse = new List<Pilot>();
                SGBarracksRosterSlot barracksRosterSlot = item as SGBarracksRosterSlot;
                pilotsInUse.Add(barracksRosterSlot.Pilot);
                foreach (var slot in slots)
                {
                    if (slot.SelectedPilot != null)
                    {
                        Main.modLog.Info?.Write($"Pilot In Slot: {slot.SelectedPilot.Pilot.Callsign}");
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
                    Main.modLog.Info?.Write($"preventing Pilot {barracksRosterSlot.Pilot.Callsign} from deploying: {restriction.restrictionCategory} in effect");
                    __instance.LC.ReturnItem(item);
                    __result = false;
                    GenericPopupBuilder.Create(restriction.errorTitle, restriction.errorMsg).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
                    __runOriginal = false;
                }
            }
            
        }
        
    }
}