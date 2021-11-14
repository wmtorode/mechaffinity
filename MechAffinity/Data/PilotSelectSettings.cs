using System.Collections.Generic;

namespace MechAffinity.Data
{
    public class PilotSelectSettings
    {
        public List<string> PossibleStartingRonin = new List<string>();
        public int RoninFromList = 4;
        public int ProceduralPilots = 0;
        public int RandomRonin = 4;
    }
}