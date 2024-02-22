using System.Collections.Generic;

namespace MechAffinity.Data.PilotManagement;

public class PilotManagementSettings
{
    public string RoninBlacklistTag = "";
    public bool EnableRoninBlacklisting = false;
    public bool EnablePilotGenTesting = false;
    public bool OverideRoninRate = false;
    public float RoninRate = 1f;
    public string StatOnHireTag = "";
    public string StatOnFireTag = "";
    public string StatOnKilledTag = "";
    public List<string> ForcedRoninSelectionIds = new List<string>();
    public List<string> ExcludeRePoolingTags = new List<string>();
    public bool CanRepoolRonin = false;
    public int RepoolRoninChance = 0;
    public int RepoolImmuneDeployments = 0;
    public bool EnableSpawnModifiers = false;
}