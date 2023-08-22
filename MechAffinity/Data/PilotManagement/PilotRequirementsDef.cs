using System.Collections.Generic;
using BattleTech;
using BattleTech.Framework;
using HBS.Util;

namespace MechAffinity.Data.PilotManagement;

public class PilotRequirementsDef
{
    public string TagId = "";
    public List<RequirementDef> HiringRequirements = new List<RequirementDef>();
    public List<RequirementDef> HiringVisibilityRequirements = new List<RequirementDef>();
    public List<string> RequiredSystemCoreIds = new List<string>();
    public List<string> RequiredSystemOwner = new List<string>();
    public List<string> RequiredPilotIds = new List<string>();
    public List<string> ConflictingPilotIds = new List<string>();
    public Dictionary<string, int> RequiredPilotTags = new Dictionary<string, int>();
    public bool LeaveIfRequiredPilotsLost = true;
    
    public void FromJSON(string json)
    {
        JSONSerializationUtility.FromJSON<PilotRequirementsDef>(this, json);
    }
}