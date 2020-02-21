using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticData.Shared.Model
{
    [Serializable]
    public class BdTable
    {
        public int Id { get; set; }
        public int GroupeId { get; set; }
        public string TeamName { get; set; }

    }
}
