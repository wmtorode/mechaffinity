using System;
using System.Collections.Generic;
using BattleTech;
using Newtonsoft.Json;

namespace MechAffinity.Data
{
    public class QuirkPool
    {
        public String tag = "";
        public int quirksToPick = 0;
        public int defaultQuirkWeight = 3;
        public bool drawUntilPicksFull = true;
        public List<string> quirksAvailable = new List<string>();
        public Dictionary<string, int> weightedQuirks = new Dictionary<string, int>();

        public List<string> GetQuirks()
        {
            List<string> quirkList = new List<string>();
            WeightedList<string> quirkPool = new WeightedList<string>(WeightedListType.WeightedRandomUseOnce);

            foreach (var keyPair in weightedQuirks)
            {
                quirkPool.Add(keyPair.Key, keyPair.Value);
            }

            foreach (var quirk in quirksAvailable)
            {
                quirkPool.Add(quirk, defaultQuirkWeight);
            }

            return quirkList;
        }
    }
}