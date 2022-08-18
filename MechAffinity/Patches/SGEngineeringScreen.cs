using System;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using Harmony;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGEngineeringScreen), "PurchaseSelectedUpgrade")]
    public static class SGEngineeringScreen_PurchaseSelectedUpgrade
    {
        private static int originalCost = 0;
        
        public static bool Prepare()
        {
            return Main.legacySettings.enablePilotQuirks;
        }
        
        public static void Prefix(SGEngineeringScreen __instance)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            ShipModuleUpgrade selectedUpgrade = (ShipModuleUpgrade) Traverse.Create(__instance).Property("SelectedUpgrade").GetValue();
            
            originalCost = selectedUpgrade.PurchaseCost;
            float multiplier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(sim.PilotRoster.ToList(),
                selectedUpgrade.Description.Id, false);
            Traverse.Create(selectedUpgrade).Property("PurchaseCost").SetValue((int)(originalCost * multiplier));
        }
        
        public static void Postfix(SGEngineeringScreen __instance)
        {
            ShipModuleUpgrade selectedUpgrade = (ShipModuleUpgrade) Traverse.Create(__instance).Property("SelectedUpgrade").GetValue();
            Traverse.Create(selectedUpgrade).Property("PurchaseCost").SetValue(originalCost);
        }
    }
}