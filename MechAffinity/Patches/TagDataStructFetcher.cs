using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(TagDataStructFetcher), "GetItem")]
    public static class TagDataStructFetcher_getItem_Patch
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Postfix(string id, TagDataStruct __result)
        {

            string desc;
            if (PilotQuirkManager.Instance.lookUpQuirkDescription(id, out desc))
            {
                if (!string.IsNullOrEmpty(__result.DescriptionTag)) __result.DescriptionTag += "\n\n";
                __result.DescriptionTag += desc;
            }
        }
    }
}
