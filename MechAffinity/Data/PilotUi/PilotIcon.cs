using System;
using UnityEngine;

namespace MechAffinity.Data
{
    public class PilotIcon
    {
        public string colour = "";
        public string tag = "";
        public string descriptionDefId = "";
        public string svgAssetId = "";
        public int priority = 1;
        
        private Color refColor;
        private bool cSet = false;

        public bool HasColour() => !String.IsNullOrEmpty(colour);
        public bool HasIcon() => !String.IsNullOrEmpty(svgAssetId);

        public bool HasDescription() => !String.IsNullOrEmpty(descriptionDefId);
        
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