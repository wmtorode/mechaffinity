using System;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using Harmony;

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
        
        public static void Prefix(SGShipModuleUpgradeViewPopulator __instance, ShipModuleUpgrade upgrade)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            float multiplier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(sim.PilotRoster.ToList(),
                upgrade.Description.Id, false);
            float upkeepMultiplier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(sim.PilotRoster.ToList(),
                upgrade.Description.Id, true);

            originalCost = upgrade.PurchaseCost;
            originalUpkeep = upgrade.AdditionalCost;

            Traverse.Create(upgrade).Property("PurchaseCost").SetValue((int)(originalCost * multiplier));
            Traverse.Create(upgrade).Property("AdditionalCost").SetValue((int)(originalUpkeep * upkeepMultiplier));
        }
        
        public static void Postfix(ShipModuleUpgrade upgrade)
        {
            Traverse.Create(upgrade).Property("PurchaseCost").SetValue(originalCost);
            Traverse.Create(upgrade).Property("AdditionalCost").SetValue(originalUpkeep);
        }
    }
}