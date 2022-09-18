using System;
using System.Collections.Generic;

namespace MechAffinity.Data
{
    public class PilotQuirkSettings
    {
        public bool playerQuirkPools = false;
        public bool argoAdditive = true;
        public bool argoMultiAutoAdjust = true;
        public float argoMin = 0.0f;
        
        public List<QuirkPool> quirkPools = new List<QuirkPool>();
        public List<PilotTooltipTag> tooltipTags = new List<PilotTooltipTag>();
        public List<String> addTags = new List<string>();
        public List<TagUpdate> tagUpdates = new List<TagUpdate>();
        public List<QuirkRestriction> restrictions = new List<QuirkRestriction>();

    }
}