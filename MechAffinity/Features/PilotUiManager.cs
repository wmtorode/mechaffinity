using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
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
            Main.modLog.Info?.Write("Issuing Load requests!");
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
                    Main.modLog.Debug?.Write($"Found IconData for: {tag}!");
                    if (icon == null)
                    {
                        icon = iconMap[tag];
                    }
                    else if (icon.priority > iconMap[tag].priority)
                    {
                        Main.modLog.Debug?.Write($"icon IconData for: {tag} has higher priority, bumping");
                        icon = iconMap[tag];
                    }
                }
            }

            return icon;

        }

        public void AdjustExpertiseTextForAffinity(LocalizableText expertise, int deployCount, string defaultText)
        {
            
            
            if (settings.enableAffinityColour)
            {
                expertise.SetText(defaultText);
                int currentLvl = -1;
                string newColour = "";
                foreach (var affinityColour in settings.pilotAffinityColours)
                {
                    if (deployCount >= affinityColour.deploysRequired && affinityColour.deploysRequired > currentLvl)
                    {
                        currentLvl = affinityColour.deploysRequired;
                        newColour = affinityColour.colour;
                    }
                }

                if (!string.IsNullOrEmpty(newColour))
                {
                    expertise.SetText($"<color={newColour}>{defaultText}</color>");
                }
                
            }
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