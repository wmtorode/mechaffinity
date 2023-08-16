using System.Collections.Generic;

namespace MechAffinity.Data.PilotManagement;

public class PilotManagementSettings
{
    public string roninBlacklistTag = "";
    public bool enableRoninBlacklisting = false;
    public bool enablePilotGenTesting = false;
    public List<string> forcedRoninSelectionIds = new List<string>();
}