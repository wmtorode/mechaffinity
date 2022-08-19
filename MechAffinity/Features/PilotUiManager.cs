using System.Collections.Generic;
using MechAffinity.Data;
using UnityEngine;

namespace MechAffinity
{
    public class PilotUiManager: BaseEffectManager
    {
        private static PilotUiManager _instance;
        
        private Dictionary<string, Color> iconColoursMap;
        private PilotUiSettings settings;

        public static PilotUiManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotUiManager();
                if (!_instance.hasInitialized) _instance.initialize(Main.settings.pilotUiSettings);
                return _instance;
            }
        }

        public void initialize(PilotUiSettings pilotUiSettings)
        {
            if(hasInitialized) return;
            iconColoursMap = new Dictionary<string, Color>();
            settings = pilotUiSettings;
            foreach (PilotIconColour pilotIcon in settings.iconColours)
            {
                iconColoursMap.Clear();
                if (iconColoursMap.ContainsKey(pilotIcon.tag)) continue;
                iconColoursMap.Add(pilotIcon.tag, pilotIcon.GetColor());
            }

            hasInitialized = true;
        }
    }
}