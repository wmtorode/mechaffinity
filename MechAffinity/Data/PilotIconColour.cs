using UnityEngine;

namespace MechAffinity.Data
{
    public class PilotIconColour
    {
        public string colour = "#FF00FF";
        public string tag = "";
        
        private Color refColor;
        private bool cSet = false;
        
        
        public Color GetColor()
        {
            if (!cSet)
            {
                ColorUtility.TryParseHtmlString(colour, out refColor);
                cSet = true;
            }
            return refColor;
        }
    }
}