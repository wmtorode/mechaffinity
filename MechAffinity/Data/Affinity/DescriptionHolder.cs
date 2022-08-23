using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechAffinity.Data
{
    class DescriptionHolder: IComparable<DescriptionHolder>
    {
        public string name;
        public string description;
        public int missionsRquired;

        public DescriptionHolder(string level, string descript, int count)
        {
            name = level;
            description = descript;
            missionsRquired = count;
        }

        public string toString(bool showCount)
        {
            if (showCount)
            {
                return $"<b>{name} ({missionsRquired})</b>: {description}:\n\n";
            }
            return $"<b>{name}</b>: {description}:\n";
        }

        public int CompareTo(DescriptionHolder compareHolder)
        {
            // A null value means that this object is greater.
            if (compareHolder == null)
                return 1;

            else
                return this.missionsRquired.CompareTo(compareHolder.missionsRquired);
        }
    }
}
