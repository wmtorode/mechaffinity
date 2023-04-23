using System;
using System.Reflection;
using BattleTech;
using BattleTech.UI;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGShipModuleUpgradeViewPopulator), "Populate")]
    public static class SGShipModuleUpgradeViewPopulator_Populate
    {
        private static int originalCost = 0;
        private static int originalUpkeep = 0;
        
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        
        public static void Prefix(ref bool __runOriginal, SGShipModuleUpgradeViewPopulator __instance, ShipModuleUpgrade upgrade)
        {
            if (!__runOriginal)
            {
                return;
            }
            
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            float multiplier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(sim.PilotRoster.rootList,
                upgrade.Description.Id, false);
            float upkeepMultiplier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(sim.PilotRoster.rootList,
                upgrade.Description.Id, true);

            originalCost = upgrade.PurchaseCost;
            originalUpkeep = upgrade.AdditionalCost;

            upgrade.PurchaseCost = (int)(originalCost * multiplier);
            upgrade.AdditionalCost = (int)(originalUpkeep * upkeepMultiplier);

        }
        
        public static void Postfix(ShipModuleUpgrade upgrade)
        {
            upgrade.PurchaseCost = originalCost;
            upgrade.AdditionalCost = originalUpkeep;
        }
    }
}