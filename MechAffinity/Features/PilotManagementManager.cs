using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using HBS.Collections;
using MechAffinity.Data;
using MechAffinity.Data.PilotManagement;
using SVGImporter;

namespace MechAffinity;

public class PilotManagementManager
{
    private static PilotManagementManager _instance;
        
    private Dictionary<string, PilotRequirementsDef> requirementsMap;
    private PilotManagementSettings settings;
    private bool hasInitialized = false;

    public static PilotManagementManager Instance
    {
        get
        {
            if (_instance == null) _instance = new PilotManagementManager();
            if (!_instance.hasInitialized) _instance.initialize(Main.settings.pilotManagementSettings, Main.PilotRequirementsDefs);
            return _instance;
        }
    }

    public void initialize(PilotManagementSettings pilotUiSettings, List<PilotRequirementsDef> requirementsDefs)
    {
        if(hasInitialized) return;
        requirementsMap = new Dictionary<string, PilotRequirementsDef>();
        settings = pilotUiSettings;
        requirementsMap.Clear();
        foreach (PilotRequirementsDef requirementsDef in requirementsDefs)
        {
            requirementsMap.Add(requirementsDef.TagId, requirementsDef);
        }

        hasInitialized = true;
    }

    private bool CheckRequirement(RequirementDef requirementDef, StarSystem starSystem, SimGameState simGameState)
    {
        RequirementDef requirements = new RequirementDef(requirementDef);
        TagSet tags;
        StatCollection stats;
        switch (requirements.Scope)
        {
            case EventScope.Commander:
                tags = simGameState.CommanderTags;
                stats = simGameState.CommanderStats;
                break;
            case EventScope.Company:
                tags = simGameState.CommanderTags;
                stats = simGameState.CompanyStats;
                break;
            case EventScope.StarSystem:
                tags = starSystem.Tags;
                stats = starSystem.Stats;
                break;
            default:
                throw new ArgumentException($"Event Scope: {requirements.Scope} not implemented for pilot reqs!");
                
        }

        return false;
    }

    public bool IsPilotAvailable(PilotDef pilotDef, StarSystem starSystem, SimGameState simGame, bool checkVisibility, bool checkHiring)
    {
        if (settings.enableRoninBlacklisting && pilotDef.PilotTags.Contains(settings.RoninBlacklistTag))
        {
            Main.modLog.Debug?.Write($"Pilot: {pilotDef.Description.Callsign} is blacklisted");
            return false;
        }

        foreach (var tag in pilotDef.PilotTags)
        {
            PilotRequirementsDef requirementsDef;
            if (requirementsMap.TryGetValue(tag, out requirementsDef))
            {
                
            }
        }

        return true;
    }
}