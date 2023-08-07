using System.Collections.Generic;
using BattleTech;

namespace MechAffinity.Data.PilotManagement;

public class PilotRequirementsDef
{
    public string TagId = "";
    public List<RequirementDef> HiringRequirements = new List<RequirementDef>();
    public List<RequirementDef> HiringVisibilityRequirements = new List<RequirementDef>();
    public string RequiredSystemCoreId = "";
    public string RequiredSystemOwner = "";
    public List<string> RequiredPilotIds = new List<string>();
    public List<string> ConflictingPilotIds = new List<string>();
}