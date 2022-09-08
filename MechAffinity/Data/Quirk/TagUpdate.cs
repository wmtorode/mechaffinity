using System.Collections.Generic;

namespace MechAffinity.Data
{
    public class TagUpdate
    {
        public List<string> addTags = new List<string>();
        public List<string> removeTags = new List<string>();
        public string selector = "";
    }
}