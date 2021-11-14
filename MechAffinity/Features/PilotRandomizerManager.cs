using System;
using System.Collections.Generic;
using System.Linq;

namespace MechAffinity
{
    public class PilotRandomizerManager : BaseEffectManager
    {
        
        private static PilotRandomizerManager _instance;
        private static readonly Random rng = new Random();
        
        public static PilotRandomizerManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotRandomizerManager();
                return _instance;
            }
        }

        private static List<T> GetRandomSubList<T>(List<T> list, int number)
        {
            var subList = new List<T>();

            if (list.Count <= 0 || number <= 0)
                return subList;

            var randomizeMe = new List<T>(list);
            
            // add enough duplicates of the list to satisfy the number specified
            while (randomizeMe.Count < number)
                randomizeMe.AddRange(list);
            
            var randomized = randomizeMe.OrderBy(item => rng.Next());
            
            for (var i = 0; i < number; i++)
                subList.Add(randomizeMe[i]);

            return subList;
        }
    }
}