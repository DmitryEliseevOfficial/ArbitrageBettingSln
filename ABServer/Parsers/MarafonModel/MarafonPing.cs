using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ABServer.Parsers.MarafonModel
{
    [DebuggerDisplay("R:{Removed?.Count} M:{Modified?.Count} U:{Updated}")]
    public class MarafonPingResponse
    {

        [JsonProperty("removed")]
        public IList<int> Removed { get; set; }

        [JsonProperty("modified")]
        public IList<Modified> Modified { get; set; }

        [JsonProperty("updated")]
        public long Updated { get; set; }
    }

    [DebuggerDisplay("{EventId} {Type} U:{Updates?.Count}")]
    public class Modified
    {

        [JsonProperty("eventId")]
        public int EventId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("updates")]
        public Dictionary<string, UpdateData> Updates { get; set; }

        [JsonProperty("treeId")]
        public int? TreeId { get; set; }

        [JsonProperty("filteredCategory")]
        public bool? FilteredCategory { get; set; }

        [JsonProperty("html")]
        public string Html { get; set; }
    }

    public class UpdateData
    {

        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("html")]
        public string Html { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
