using System.Collections.Generic;
using BattleTech;

namespace MechAffinity.Data.PilotManagement;

public class PilotRequirementsDef
{
    public string TagId = "";
    public List<RequirementDef> HiringHallRequirements = new List<RequirementDef>();
    public List<RequirementDef> hiringVisibilityRequirements = new List<RequirementDef>();
}