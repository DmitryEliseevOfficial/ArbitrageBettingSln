using System;

namespace ABShared
{
    [Serializable]
    public class SportTypeData
    {
        public bool IsFilter { get; set; }
        public SportType SportType { get; set; }
    }
}
