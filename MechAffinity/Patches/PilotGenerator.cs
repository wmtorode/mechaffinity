using System.Collections.Generic;
using BattleTech;
using UnityEngine;

namespace MechAffinity.Patches;

[HarmonyPatch(typeof(PilotGenerator), "GeneratePilots")]

class PilotGenerator_GeneratePilots
{
    public static bool Prepare()
    {
        return Main.settings.enablePilotManagement && Main.settings.pilotManagementSettings.enablePilotGenTesting;
    }
    public static void Prefix(ref bool __runOriginal, PilotGenerator __instance, int numPilots, int systemDifficulty, float roninChance, ref List<PilotDef> __result, out List<PilotDef> roninList)
    {
        
        roninList = null;
        
        if (!__runOriginal)
        {
            return;
        }

        __runOriginal = false;



        if (numPilots <= 0)
        {
            __result = null;
            return;
        }
        
        Main.modLog.Debug?.Write($"Attempting to Generate: {numPilots}!");
        roninList = new List<PilotDef>();
        roninChance = Mathf.Clamp01(roninChance);
        List<PilotDef> pilots = new List<PilotDef>();
        for (int index = 0; index < numPilots; ++index)
        {
            if (__instance.Sim.NetworkRandom.Float() <= roninChance)
            {
                PilotDef unusedRonin = PilotManagementManager.Instance.GetRandomRonin(__instance.Sim, roninList);
                if (unusedRonin != null)
                {
                    Main.modLog.Debug?.Write($"Got Pilot: {unusedRonin.Description.Callsign}");
                    roninList.Add(unusedRonin);
                }
                else
                {
                    roninChance = -1f;
                    --index;
                }
            }
            else
                pilots.Add(__instance.GenerateRandomPilot(systemDifficulty));
        }

        __result = pilots;
        return;
            
    }
}