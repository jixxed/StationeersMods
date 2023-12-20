using System.Collections.Generic;

namespace StationeersMods
{
    public class ValidationResult
    {
        
        public bool Retry = false;
        public bool NeedSave = false;
        public Dictionary<ulong, int> ReorderCount = new Dictionary<ulong, int>();
    }
}