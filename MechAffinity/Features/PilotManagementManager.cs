using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using FluffyUnderware.DevTools;
using HBS.Collections;
using MechAffinity.Data;
using MechAffinity.Data.PilotManagement;
using Org.BouncyCastle.Security;
using SVGImporter;

namespace MechAffinity;

public class PilotManagementManager
{
    private static PilotManagementManager _instance;
        
    private Dictionary<string, PilotRequirementsDef> requirementsMap;
    private List<RoninSpawnModifierDef> SpawnModifiers;
    private RoninSpawnModifierDef _defaultSpawnModifierDef;
    private PilotManagementSettings settings;
    private bool hasInitialized = false;
    private StatCollection companyStats;
    private SimGameState _simGame;
    private const string MaPilotCount = "MaPilotCount";
    
    private const string MaPilotHiredPrefix = "hasPilot_";
    private const string MaPilotFiredPrefix = "firedPilot_";
    private const string MaPilotKilledPrefix = "killedPilot_";

    private const string MaFiredModifierPrefix = "MaFiredModifier_";
    private const string MaFiredModifierRecoveryPrefix = "MaFiredRecovery_";
    private const string MaKilledModifierPrefix = "MaKilledModifier_";
    private const string MaKilledModifierRecoveryPrefix = "MaKilledRecovery_";

    private const string MaRoninFired = "MaRoninFired";
    private const string MaRoninKilled = "MaRoninKilled";

    public static PilotManagementManager Instance
    {
        get
        {
            if (_instance == null) _instance = new PilotManagementManager();
            if (!_instance.hasInitialized) _instance.initialize(Main.settings.pilotManagementSettings, Main.PilotRequirementsDefs, Main.RoninSpawnModifiers);
            return _instance;
        }
    }

    public void initialize(PilotManagementSettings pilotUiSettings, List<PilotRequirementsDef> requirementsDefs, List<RoninSpawnModifierDef> roninSpawnModifiers)
    {
        if(hasInitialized) return;
        requirementsMap = new Dictionary<string, PilotRequirementsDef>();
        settings = pilotUiSettings;
        requirementsMap.Clear();
        foreach (PilotRequirementsDef requirementsDef in requirementsDefs)
        {
            requirementsMap.Add(requirementsDef.TagId, requirementsDef);
        }

        _defaultSpawnModifierDef = new RoninSpawnModifierDef();
        SpawnModifiers = new List<RoninSpawnModifierDef>();
        foreach (var spawnModifier in roninSpawnModifiers)
        {
            if (spawnModifier.IsDefault)
            {
                _defaultSpawnModifierDef = spawnModifier;
            }
            SpawnModifiers.Add(spawnModifier);
        }

        hasInitialized = true;
    }

    private string KilledModifierStatName(RoninSpawnModifierDef spawnModifierDef)
    {
        return $"{MaKilledModifierPrefix}{spawnModifierDef.Id}";
    }
    
    private string KilledRecoveryStatName(RoninSpawnModifierDef spawnModifierDef)
    {
        return $"{MaKilledModifierRecoveryPrefix}{spawnModifierDef.Id}";
    }
    
    private string FiredModifierStatName(RoninSpawnModifierDef spawnModifierDef)
    {
        return $"{MaFiredModifierPrefix}{spawnModifierDef.Id}";
    }
    
    private string FiredRecoveryStatName(RoninSpawnModifierDef spawnModifierDef)
    {
        return $"{MaFiredModifierRecoveryPrefix}{spawnModifierDef.Id}";
    }
    
    private int FiredRecoveryStat(RoninSpawnModifierDef spawnModifierDef)
    {
        return companyStats.GetValue<int>(FiredRecoveryStatName(spawnModifierDef));
    }
    
    private int KilledRecoveryStat(RoninSpawnModifierDef spawnModifierDef)
    {
        return companyStats.GetValue<int>(KilledRecoveryStatName(spawnModifierDef));
    }
    
    private void SetFiredRecoveryStat(RoninSpawnModifierDef spawnModifierDef, int value)
    { 
        companyStats.Set<int>(FiredRecoveryStatName(spawnModifierDef), value);
    }
    
    private void SetKilledRecoveryStat(RoninSpawnModifierDef spawnModifierDef, int value)
    {
        companyStats.Set<int>(KilledRecoveryStatName(spawnModifierDef), value);
    }

    private int KilledSpawnModifier(RoninSpawnModifierDef spawnModifierDef)
    {
        return companyStats.GetValue<int>(KilledModifierStatName(spawnModifierDef));
    }
    
    private void SetKilledSpawnModifier(RoninSpawnModifierDef spawnModifierDef, int value)
    {
        string statName = KilledModifierStatName(spawnModifierDef);
        string recoveryStat = KilledRecoveryStatName(spawnModifierDef);
        int roninKilled = RoninKilled;
        if (roninKilled < spawnModifierDef.KilledThreshold) return;
        if (roninKilled > spawnModifierDef.KilledCap)
        {
            Main.modLog.Debug?.Write(
                $"Setting spawn mod: {statName} to capped value {spawnModifierDef.KilledCappedModifier}");
            companyStats.Set<int>(statName, spawnModifierDef.KilledCappedModifier);
        }
        else
        {
            if (value < spawnModifierDef.KilledFloor)
            {
                Main.modLog.Debug?.Write(
                    $"Setting spawn mod: {statName} to floored value {spawnModifierDef.KilledFloor}");
                companyStats.Set<int>(statName, spawnModifierDef.KilledFloor);
            }
            else
            {
                if (value > spawnModifierDef.DefaultKilledModifier)
                {
                    companyStats.Set(statName, spawnModifierDef.DefaultKilledModifier);
                }
                else
                {
                    companyStats.Set<int>(statName, value);
                }
            }
            
        }
        
        companyStats.Set(recoveryStat, spawnModifierDef.KilledRecoveryDays);
    }
    
    private int FiredSpawnModifier(RoninSpawnModifierDef spawnModifierDef)
    {
        return companyStats.GetValue<int>(FiredModifierStatName(spawnModifierDef));
    }
    
    private void SetFiredSpawnModifier(RoninSpawnModifierDef spawnModifierDef, int value)
    {
        string statName = FiredModifierStatName(spawnModifierDef);
        string recoveryStat = FiredRecoveryStatName(spawnModifierDef);
        int roninFired = RoninFired;
        if (roninFired <= spawnModifierDef.FiredThreshold) return;
        if (roninFired >= spawnModifierDef.FiredCap)
        {
            Main.modLog.Debug?.Write(
                $"Setting spawn mod: {statName} to capped value {spawnModifierDef.FiredCappedModifier}");
            companyStats.Set<int>(statName, spawnModifierDef.FiredCappedModifier);
        }
        else
        {
            if (value < spawnModifierDef.FiredFloor)
            {
                Main.modLog.Debug?.Write(
                    $"Setting spawn mod: {statName} to floored value {spawnModifierDef.FiredFloor}");
                companyStats.Set<int>(statName, spawnModifierDef.FiredFloor);
            }
            else
            {
                if (value > spawnModifierDef.DefaultFiredModifier)
                {
                    companyStats.Set(statName, spawnModifierDef.DefaultFiredModifier);
                }
                else
                {
                    companyStats.Set<int>(statName, value);
                }
            }
        }
        
        companyStats.Set(recoveryStat, spawnModifierDef.FiredRecoveryDays);
    }

    public RoninSpawnModifierDef FindModifier(PilotDef pilotDef)
    {
        foreach (var modifier in SpawnModifiers)
        {
            if (modifier.ApplicableTags.Count == 0) continue; // This is the Default modifier
            foreach (var tag in pilotDef.PilotTags)
            {
                if (modifier.ApplicableTags.Contains(tag)) return modifier;
            }
        }

        return _defaultSpawnModifierDef;
    }
    
    public void setSimGameState(SimGameState simGameState)
    {
        _simGame = simGameState;
    }
    
    public void setCompanyStats(StatCollection stats)
    {
        companyStats = stats;
        
        if (!companyStats.ContainsStatistic(MaPilotCount))
        {
            companyStats.AddStatistic<int>(MaPilotCount, 0);
        }
        if (!companyStats.ContainsStatistic(MaRoninFired))
        {
            companyStats.AddStatistic<int>(MaRoninFired, 0);
        }
        if (!companyStats.ContainsStatistic(MaRoninKilled))
        {
            companyStats.AddStatistic<int>(MaRoninKilled, 0);
        }
        
        foreach (var spawnModifier in SpawnModifiers)
        {
            var firedStat = FiredModifierStatName(spawnModifier);
            var firedRecoveryStat = FiredRecoveryStatName(spawnModifier);
            var killedStat = KilledModifierStatName(spawnModifier);
            var killedRecoveryStat = KilledRecoveryStatName(spawnModifier);
            if (!companyStats.ContainsStatistic(firedStat))
            {
                companyStats.AddStatistic(firedStat, spawnModifier.DefaultFiredModifier);
            }
            if (!companyStats.ContainsStatistic(firedRecoveryStat))
            {
                companyStats.AddStatistic(firedRecoveryStat, 0);
            }
            if (!companyStats.ContainsStatistic(killedStat))
            {
                companyStats.AddStatistic(killedStat, spawnModifier.DefaultKilledModifier);
            }
            if (!companyStats.ContainsStatistic(killedRecoveryStat))
            {
                companyStats.AddStatistic(killedRecoveryStat, 0);
            }
        }
    }

    private int RoninFired
    {
        get
        {
            return companyStats.GetValue<int>(MaRoninFired);
        }

        set
        {
            companyStats.Set<int>(MaRoninFired, value);
        }
    }
    
    private int RoninKilled
    {
        get
        {
            return companyStats.GetValue<int>(MaRoninKilled);
        }

        set
        {
            companyStats.Set<int>(MaRoninKilled, value);
        }
    }

    public void setPilotCountStat(int pilotCount)
    {
        companyStats.Set<int>(MaPilotCount, pilotCount);
    }

    public void processHiredPilot(PilotDef pilotDef)
    {
        if (_simGame == null) return;
        if (string.IsNullOrEmpty(settings.StatOnHireTag)) return;
        if (pilotDef.PilotTags.Contains(settings.StatOnHireTag))
        {
            _simGame.companyTags.Add($"{MaPilotHiredPrefix}{pilotDef.Description.Id}");
        }
    }

    public void UpdateSpawnModifiers()
    {
        if (!settings.EnableSpawnModifiers) return;
        
        foreach (var spawnModifier in SpawnModifiers)
        {
            
            var firedModifier = FiredSpawnModifier(spawnModifier);
            var killedModifier = KilledSpawnModifier(spawnModifier);

            if (spawnModifier.DefaultFiredModifier > firedModifier)
            {
                var firedRecoveryStat = FiredRecoveryStat(spawnModifier);
                firedRecoveryStat--;
                if (firedRecoveryStat <= 0)
                {
                    SetFiredSpawnModifier(spawnModifier, firedModifier + spawnModifier.FiredProgression);
                }
                else
                {
                    SetFiredRecoveryStat(spawnModifier, firedRecoveryStat);
                }
            }

            if (spawnModifier.DefaultKilledModifier > killedModifier)
            {
                var killedRecoveryStat = KilledRecoveryStat(spawnModifier);
                killedRecoveryStat--;
                if (killedRecoveryStat <= 0)
                {
                    SetKilledSpawnModifier(spawnModifier, killedModifier + spawnModifier.KilledProgression);
                }
                else
                {
                    SetKilledRecoveryStat(spawnModifier, killedRecoveryStat);
                }
            }
        }
    }

    private void UpdatedFiredModifiers()
    {
        foreach (var spawnModifier in SpawnModifiers)
        {
            var modifier = FiredSpawnModifier(spawnModifier);
            modifier -= spawnModifier.FiredProgression;
            SetFiredSpawnModifier(spawnModifier, modifier);
        }
    }

    private void UpdateKilledModifiers()
    {
        foreach (var spawnModifier in SpawnModifiers)
        {
            var modifier = KilledSpawnModifier(spawnModifier);
            modifier -= spawnModifier.KilledProgression;
            SetKilledSpawnModifier(spawnModifier, modifier);
        }
    }

    public void processFiredPilot(PilotDef pilotDef)
    {
        if (_simGame == null) return;
        if (_simGame.companyTags.Contains($"{MaPilotHiredPrefix}{pilotDef.Description.Id}"))
        {
            _simGame.companyTags.Remove($"{MaPilotHiredPrefix}{pilotDef.Description.Id}");
        }
        
        if (!string.IsNullOrEmpty(settings.StatOnFireTag))
        {
            if (pilotDef.PilotTags.Contains(settings.StatOnFireTag))
            {
                _simGame.companyTags.Add($"{MaPilotFiredPrefix}{pilotDef.Description.Id}");
            }
        }

        if (pilotDef.IsRonin)
        {
            RoninFired++;
            if (settings.EnableSpawnModifiers)
            {
                UpdatedFiredModifiers();
            }
        }

        if (settings.CanRepoolRonin && pilotDef.IsRonin)
        {
            bool canRepool = true;
            
            // some ronin can never be re-added to the ronin pool
            foreach (var tag in pilotDef.PilotTags)
            {
                if (settings.ExcludeRePoolingTags.Contains(tag))
                {
                    canRepool = false;
                    break;
                }
            }

            // experienced ronin should also not be re-added
            if (pilotDef.MissionsPiloted > settings.RepoolImmuneDeployments)
            {
                canRepool = false;
            }

            // if roll was successful and the pilot is eligible for re-pooling
            // then remove them from the used ronin list, allowing them to be selected again
            Random random = new Random();
            int roll = random.Next(0, 100);
            if (canRepool && roll < settings.RepoolRoninChance)
            {
                _simGame.usedRoninIDs.Remove(pilotDef.Description.Id);
            }
        }
    }
    
    public void processKilledPilot(PilotDef pilotDef)
    {
        if (_simGame == null) return;
        if (_simGame.companyTags.Contains($"{MaPilotHiredPrefix}{pilotDef.Description.Id}"))
        {
            _simGame.companyTags.Remove($"{MaPilotHiredPrefix}{pilotDef.Description.Id}");
        }
        
        if (!string.IsNullOrEmpty(settings.StatOnKilledTag))
        {
            if (pilotDef.PilotTags.Contains(settings.StatOnKilledTag))
            {
                _simGame.companyTags.Add($"{MaPilotKilledPrefix}{pilotDef.Description.Id}");
            }
        }
        
        if (pilotDef.IsRonin)
        {
            RoninKilled++;
            if (settings.EnableSpawnModifiers)
            {
                UpdateKilledModifiers();
            }
        }
    }

    private bool CanSpawn(PilotDef pilotDef)
    {
        if (!settings.EnableSpawnModifiers) return true;
        
        var spawnModifier = FindModifier(pilotDef);
        
        var firedRoll = _simGame.NetworkRandom.Int(0, 100);
        var killedRoll = _simGame.NetworkRandom.Int(0, 100);

        var firedRate = FiredSpawnModifier(spawnModifier);
        var killedRate = KilledSpawnModifier(spawnModifier);

        if (firedRoll >= firedRate)
        {
            Main.modLog.Info?.Write($"Pilot: {pilotDef.Description.Callsign} cannot spawn, failed fired roll: {firedRoll}/{firedRate}");
            return false;
        }
        
        if (killedRoll >= killedRate)
        {
            Main.modLog.Info?.Write($"Pilot: {pilotDef.Description.Callsign} cannot spawn, failed killed roll: {killedRoll}/{killedRate}");
            return false;
        }

        return true;

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
        if (settings.ForcedRoninSelectionIds.Count > 0)
        {

            foreach (var pilotDef in new List<PilotDef>(list))
            {
                if (settings.ForcedRoninSelectionIds.Contains(pilotDef.Description.Id))
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

    public PilotDef GetRandomRonin(SimGameState sim, List<PilotDef> currentRonin, out bool spawnRollFailed)
    {
        List<PilotDef> list = GetShuffledRonin(sim);
        Main.modLog.Debug?.Write($"Have: {list.Count} Ronin to try");
        spawnRollFailed = false;
        string reasonForRemoval;
        while (list.Count > 0)
        {
            if (!sim.usedRoninIDs.Contains(list[0].Description.Id) && sim.IsRoninWhitelisted(list[0]) 
                                                                   && IsPilotAvailable(list[0], 
                                                                       sim.CurSystem, sim, true, false, out reasonForRemoval) && !AlreadyPicked(list[0], currentRonin))
            {
                if (!CanSpawn(list[0]))
                {
                    spawnRollFailed = true;
                    return null;
                }
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
        if (settings.EnableRoninBlacklisting && pilotDef.PilotTags.Contains(settings.RoninBlacklistTag))
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
                    
                    if (!String.IsNullOrEmpty(requirementsDef.RequiredSystemCoreIdPrefix) && !starSystem.Def.CoreSystemID.StartsWith(requirementsDef.RequiredSystemCoreIdPrefix, StringComparison.InvariantCultureIgnoreCase))
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
                    
                    if (requirementsDef.AntiSystemOwner.Count > 0 && requirementsDef.AntiSystemOwner.Contains(starSystem.OwnerValue.Name))
                    {
                        reasonForNotAvailable =
                            $"Pilot: {pilotDef.Description.Callsign} is not available on systems controlled by {requirementsDef.AntiSystemOwner}";
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