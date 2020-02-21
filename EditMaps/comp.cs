using StaticData.Shared.Model;
using System.Collections.Generic;

namespace EditMaps
{
    public class Comp : IEqualityComparer<UnicData>
    {
        public bool Equals(UnicData x, UnicData y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(UnicData obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
