using System.Collections.Generic;

namespace MechAffinity.Data
{
    public class PilotSelectSettings
    {
        public List<string> PossibleStartingRonin = new List<string>();
        public int RoninFromList = 0;
        public int ProceduralPilots = 4;
        public int RandomRonin = 4;
    }
}