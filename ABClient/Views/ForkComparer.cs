using System.Collections.Generic;
using ABShared;

namespace ABClient.Views
{
    public class ForkComparer : IEqualityComparer<Fork>
    {
        public bool Equals(Fork x, Fork y)
        {
            if (x.Teams == y.Teams)
                return true;
            return false;
        }

        public int GetHashCode(Fork obj)
        {
            return obj.Teams.GetHashCode();
        }
    }
}