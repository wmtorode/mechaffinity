using System.Collections.Generic;
using BattleTech;
using BattleTech.UI;
using MechAffinity.Data;
using UnityEngine;

namespace MechAffinity
{
    public class PilotUiManager: BaseEffectManager
    {
        private static PilotUiManager _instance;
        
        private Dictionary<string, PilotIcon> iconMap;
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
            iconMap = new Dictionary<string, PilotIcon>();
            settings = pilotUiSettings;
            foreach (PilotIcon pilotIcon in settings.pilotIcons)
            {
                iconMap.Clear();
                if (iconMap.ContainsKey(pilotIcon.tag)) continue;
                iconMap.Add(pilotIcon.tag, pilotIcon);
            }

            hasInitialized = true;
        }

        public void SetPilotIcon(Pilot pilot, UIColorRefTracker pilotTypeBackground)
        {
            foreach (string tag in pilot.pilotDef.PilotTags)
            {
                Main.modLog.LogMessage($"checking tag: {tag}: {iconMap.ContainsKey(tag)}");
                if (iconMap.ContainsKey(tag))
                {
                    Main.modLog.LogMessage("Setting Pilot Icon Colour!");
                    pilotTypeBackground.SetUIColor(UIColor.Custom);
                    pilotTypeBackground.OverrideWithColor(iconMap[tag].GetColor());
                    break;
                }
            }
        }
        
    }
}