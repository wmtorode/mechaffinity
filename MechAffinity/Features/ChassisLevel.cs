using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechAffinity
{
    public class ChassisLevel
    {
        /// <summary>
        /// The internal key for the chassis
        /// </summary>
        public string ChassisId { get; set; }

        /// <summary>
        /// The name of the Chassis (e.g. Griffin) 
        /// </summary>
        public string ChassisName { get; set; }

        /// <summary>
        /// The the levels text for the chassis.
        /// e.g.:  Comfortable (1/10)
        /// </summary>
        public List<string> LevelTextLines { get; set; }

        /// <summary>
        /// The number of deployments the pilot has had for this chassis
        /// </summary>
        public int DeploymentCount { get; set; }
    }
}
