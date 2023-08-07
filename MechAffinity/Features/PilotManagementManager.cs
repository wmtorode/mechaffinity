using System;
using System.Collections.Generic;
using System.Linq;
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
        return SimGameState.MeetsRequirements(requirements, tags, stats);
    }

    public bool IsPilotAvailable(PilotDef pilotDef, StarSystem starSystem, SimGameState simGame, bool checkVisibility, bool checkHiring, out string reasonForNotAvailable)
    {
        if (settings.enableRoninBlacklisting && pilotDef.PilotTags.Contains(settings.RoninBlacklistTag))
        {
            reasonForNotAvailable = $"Pilot: {pilotDef.Description.Callsign} is blacklisted";
            Main.modLog.Debug?.Write(reasonForNotAvailable);
            return false;
        }

        foreach (var tag in pilotDef.PilotTags)
        {
            PilotRequirementsDef requirementsDef;
            if (requirementsMap.TryGetValue(tag, out requirementsDef))
            {
                if (checkVisibility)
                {
                    foreach (var requirement in requirementsDef.HiringVisibilityRequirements)
                    {
                        if (!CheckRequirement(requirement, starSystem, simGame))
                        {
                            reasonForNotAvailable =
                                $"Pilot: {pilotDef.Description.Callsign} has failed visibility requirements";
                            Main.modLog.Debug?.Write(reasonForNotAvailable);
                            return false;
                        }
                    }

                    if (!String.IsNullOrEmpty(requirementsDef.RequiredSystemCoreId) && starSystem.Def.CoreSystemID != requirementsDef.RequiredSystemCoreId)
                    {
                        reasonForNotAvailable =
                            $"Pilot: {pilotDef.Description.Callsign} is only available on {simGame.starDict[requirementsDef.RequiredSystemCoreId].Name}";
                        Main.modLog.Debug?.Write(reasonForNotAvailable);
                        return false;
                    }
                    
                    if (!String.IsNullOrEmpty(requirementsDef.RequiredSystemOwner) && starSystem.OwnerValue.Name != requirementsDef.RequiredSystemOwner)
                    {
                        reasonForNotAvailable =
                            $"Pilot: {pilotDef.Description.Callsign} is only available on systems controlled by {requirementsDef.RequiredSystemOwner}";
                        Main.modLog.Debug?.Write(reasonForNotAvailable);
                        return false;
                    }
                    
                }
                
                if (checkHiring)
                {
                    foreach (var requirement in requirementsDef.HiringRequirements)
                    {
                        if (!CheckRequirement(requirement, starSystem, simGame))
                        {
                            reasonForNotAvailable =
                                $"Company does not meet hiring requirements for this pilot";
                            Main.modLog.Debug?.Write($"Pilot: {pilotDef.Description.Callsign} has unfilled hiring requirements");
                            return false;
                        }
                    }

                    List<string> currentPilotIds = simGame.PilotRoster.rootList.Select(x => x.Description.Id).ToList();

                    if (requirementsDef.RequiredPilotIds.Count > 0)
                    {
                        foreach (var requiredPilot in requirementsDef.RequiredPilotIds)
                        {
                            if (!currentPilotIds.Contains(requiredPilot))
                            {
                                var missingPilot = simGame.RoninPilots.Where(p => p.Description.Id == requiredPilot).FirstOrDefault();
                                reasonForNotAvailable =
                                    $"Requires {missingPilot.Description.Callsign} to be a member of your company";
                                Main.modLog.Debug?.Write($"Pilot: {pilotDef.Description.Callsign} {reasonForNotAvailable}");
                                return false;
                                
                            }
                        }
                    }
                    
                    if (requirementsDef.ConflictingPilotIds.Count > 0)
                    {
                        foreach (var conflictedPilot in requirementsDef.ConflictingPilotIds)
                        {
                            if (currentPilotIds.Contains(conflictedPilot))
                            {
                                var conflictedCrew = simGame.GetPilot(conflictedPilot).pilotDef;
                                reasonForNotAvailable =
                                    $"Will not work with {conflictedCrew.Description.Callsign}";
                                Main.modLog.Debug?.Write($"Pilot: {pilotDef.Description.Callsign} {reasonForNotAvailable}");
                                return false;
                                
                            }
                        }
                    }
                }
            }
        }

        reasonForNotAvailable = "";
        return true;
    }
}