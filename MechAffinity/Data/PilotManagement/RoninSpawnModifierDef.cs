using System.Collections.Generic;

namespace MechAffinity.Data.PilotManagement;

public class RoninSpawnModifierDef
{
    public string Id = "Default";
    public bool IsDefault = false;
    public List<string> ApplicableTags = new List<string>();
    public int FiredThreshold = 10;
    public int KilledThreshold = 10;
    public int DefaultFiredModifier = 100;
    public int DefaultKilledModifier = 100;
    public int FiredProgression = 5;
    public int KilledProgression = 5;
    public int FiredRecoveryDays = 5;
    public int KilledRecoveryDays = 5;
    public int FiredFloor = 0;
    public int KilledFloor = 0;
    public int FiredCap = 10000;
    public int KilledCap = 10000;
    public int FiredCappedModifier = 50;
    public int KilledCappedModifier = 50;
}