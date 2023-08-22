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

    public List<PilotDef> GetShuffledRonin(SimGameState simGameState)
    {
        List<PilotDef> list = new List<PilotDef>(simGameState.RoninPilots);
        list.Shuffle();
        
        // This is mostly intended for testing
        if (settings.forcedRoninSelectionIds.Count > 0)
        {

            foreach (var pilotDef in new List<PilotDef>(list))
            {
                if (settings.forcedRoninSelectionIds.Contains(pilotDef.Description.Id))
                {
                    list.Remove(pilotDef);
                    list.Insert(0,pilotDef);
                }
            }
        }

        return list;
    }

    private bool AlreadyPicked(PilotDef pilotDef, List<PilotDef> currentPilots)
    {
        if (currentPilots == null) return false;
        return currentPilots.Contains(pilotDef);
    }

    public PilotDef GetRandomRonin(SimGameState sim, List<PilotDef> currentRonin = null)
    {
        List<PilotDef> list = GetShuffledRonin(sim);
        Main.modLog.Debug?.Write($"Have: {list.Count} Ronin to try");
        string reasonForRemoval;
        while (list.Count > 0)
        {
            if (!sim.usedRoninIDs.Contains(list[0].Description.Id) && sim.IsRoninWhitelisted(list[0]) 
                                                                   && IsPilotAvailable(list[0], 
                                                                       sim.CurSystem, sim, true, false, out reasonForRemoval) && !AlreadyPicked(list[0], currentRonin))
            {
                return list[0];
            }
            Main.modLog.Debug?.Write($"Rejecting: {list[0].Description.Callsign}");
            list.RemoveAt(0);
        }
        return null;
    }

    public List<Pilot> PilotsThatMustLeave(PilotDef pilotLeaving, List<Pilot> currentRoster)
    {
        HashSet<Pilot> pilotsThatAlsoLeave = new HashSet<Pilot>();

        foreach (var pilot in currentRoster)
        {
            if (pilot.Description.Id == pilotLeaving.Description.Id)
            {
                continue;
            }
            foreach (var tag in pilot.pilotDef.PilotTags)
            {
                PilotRequirementsDef requirementsDef;
                if (requirementsMap.TryGetValue(tag, out requirementsDef))
                {
                    if (requirementsDef.RequiredPilotIds.Count > 0)
                    {
                        if (requirementsDef.RequiredPilotIds.Contains(pilotLeaving.Description.Id))
                        {
                            Main.modLog.Info?.Write($"Pilot: {pilot.Callsign} must leave company because {pilotLeaving.Description.Callsign} is leaving or dead");
                            pilotsThatAlsoLeave.Add(pilot);
                            break;
                        }
                    }
                }
            }
        }

        return pilotsThatAlsoLeave.ToList();
    }

    public bool IsPilotAvailable(PilotDef pilotDef, StarSystem starSystem, SimGameState simGame, bool checkVisibility, bool checkHiring, out string reasonForNotAvailable)
    {
        if (settings.enableRoninBlacklisting && pilotDef.PilotTags.Contains(settings.roninBlacklistTag))
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

                    if (requirementsDef.RequiredSystemCoreIds.Count > 0 && !requirementsDef.RequiredSystemCoreIds.Contains(starSystem.Def.CoreSystemID))
                    {
                        reasonForNotAvailable =
                            $"Pilot: {pilotDef.Description.Callsign} is not available on this system";
                        Main.modLog.Debug?.Write(reasonForNotAvailable);
                        return false;
                    }
                    
                    if (requirementsDef.RequiredSystemOwner.Count > 0 && !requirementsDef.RequiredSystemOwner.Contains(starSystem.OwnerValue.Name))
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
                                if (missingPilot == null)
                                {
                                    reasonForNotAvailable =
                                        $"Requires a specific pilot to be a member of your company";
                                }
                                else
                                {
                                    reasonForNotAvailable =
                                        $"Requires {missingPilot.Description.Callsign} to be a member of your company";
                                }
                                
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
        Main.modLog.Debug?.Write($"Pilot: {pilotDef.Description.Callsign} is available");
        return true;
    }
}