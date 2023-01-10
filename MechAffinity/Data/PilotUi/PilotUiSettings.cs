using System.Collections.Generic;

namespace MechAffinity.Data
{
    public class PilotUiSettings
    {
        public List<PilotIcon> pilotIcons = new List<PilotIcon>();
        public List<PilotAffinityColour> pilotAffinityColours = new List<PilotAffinityColour>();
        public bool enableAffinityColour = false;
        public bool orderByAffinity = false;
    }
}