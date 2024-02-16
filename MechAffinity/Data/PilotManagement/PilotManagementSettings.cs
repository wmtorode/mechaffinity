using System.Collections.Generic;

namespace MechAffinity.Data.PilotManagement;

public class PilotManagementSettings
{
    public string roninBlacklistTag = "";
    public bool enableRoninBlacklisting = false;
    public bool enablePilotGenTesting = false;
    public string statOnHireTag = "";
    public string statOnFireTag = "";
    public string statOnKilledTag = "";
    public List<string> forcedRoninSelectionIds = new List<string>();
}