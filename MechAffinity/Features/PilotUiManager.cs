using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using MechAffinity.Data;
using UnityEngine;
using SVGImporter;

namespace MechAffinity
{
    public class PilotUiManager: BaseEffectManager
    {
        private static PilotUiManager _instance;
        
        private Dictionary<string, PilotIcon> iconMap;
        private PilotUiSettings settings;
        private DataManager dataManager;

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
            dataManager = UnityGameInstance.BattleTechGame.DataManager;
            iconMap = new Dictionary<string, PilotIcon>();
            settings = pilotUiSettings;
            LoadRequest loadRequest = dataManager.CreateLoadRequest();
            iconMap.Clear();
            foreach (PilotIcon pilotIcon in settings.pilotIcons)
            {
                if (iconMap.ContainsKey(pilotIcon.tag)) continue;
                iconMap.Add(pilotIcon.tag, pilotIcon);
                if (pilotIcon.HasIcon()) loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, pilotIcon.svgAssetId, null);
                if (pilotIcon.HasDescription()) loadRequest.AddLoadRequest<BaseDescriptionDef>(BattleTechResourceType.BaseDescriptionDef, pilotIcon.descriptionDefId, null);
            }
            
            loadRequest.ProcessRequests();

            hasInitialized = true;
        }

        public void issueLoadRequests()
        {
            Main.modLog.LogMessage("Issuing Load requests!");
            LoadRequest loadRequest = dataManager.CreateLoadRequest();
            foreach (PilotIcon pilotIcon in settings.pilotIcons)
            {
                if (pilotIcon.HasIcon()) loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, pilotIcon.svgAssetId, null);
                if (pilotIcon.HasDescription()) loadRequest.AddLoadRequest<BaseDescriptionDef>(BattleTechResourceType.BaseDescriptionDef, pilotIcon.descriptionDefId, null);
            }
            
            loadRequest.ProcessRequests();
        }

        public PilotIcon GetPilotIcon(Pilot pilot)
        {
            PilotIcon icon = null;
            foreach (string tag in pilot.pilotDef.PilotTags)
            {
                if (iconMap.ContainsKey(tag))
                {
                    Main.modLog.LogMessage($"Found IconData for: {tag}!");
                    if (icon == null)
                    {
                        icon = iconMap[tag];
                    }
                    else if (icon.priority > iconMap[tag].priority)
                    {
                        Main.modLog.LogMessage($"icon IconData for: {tag} has higher priority, bumping");
                        icon = iconMap[tag];
                    }
                }
            }

            return icon;

        }

        public SVGAsset GetSvgAsset(string iconId)
        {
            return dataManager.GetObjectOfType<SVGAsset>(iconId, BattleTechResourceType.SVGAsset);
        }
        
        public BaseDescriptionDef GetDescriptionDef(string defId)
        {
            return dataManager.BaseDescriptionDefs.Get(defId);
        }

    }
}