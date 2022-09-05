using Harmony;
using BattleTech;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(PilotDef))]
    [HarmonyPatch("IsImmortal", MethodType.Getter)]
    public static class PilotDef_IsImmortal
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        
        [HarmonyPriority(Priority.First)]
        public static void Postfix(PilotDef __instance, ref bool __result)
        {
            if (!__result)
            {
                __result = PilotQuirkManager.Instance.hasImmortality(__instance);
            }
            Main.modLog.DebugMessage($"Pilot: {__instance.Description.Callsign}, Immortal: {__result}");
        }
    }
}