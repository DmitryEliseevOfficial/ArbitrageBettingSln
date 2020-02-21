using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ABServer.Parsers
{
    [DebuggerDisplay("{GameId} - {Odd} cf:{cf} d1:{d1}")]
    public class ZenitBet
    {
        [JsonProperty("gid")]
        public int GameId { get; set; }

        [JsonProperty("lid")]
        public int lid { get; set; }

        [JsonProperty("sid")]
        public int sid { get; set; }

        [JsonProperty("odd")]
        public int Odd { get; set; }

        [JsonProperty("id")]
        public int id { get; set; }

        [JsonProperty("cf")]
        public float cf { get; set; }

        [JsonProperty("d1")]
        public string d1 { get; set; }

        [JsonProperty("d2")]
        public string d2 { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("max")]
        public int MaximumSum { get; set; }
    }

    public class Game
    {

        [JsonProperty("id")]
        public int id { get; set; }

        [JsonProperty("lid")]
        public int lid { get; set; }

        [JsonProperty("sid")]
        public int sid { get; set; }

        [JsonProperty("date")]
        public string date { get; set; }

        [JsonProperty("time")]
        public int time { get; set; }

        [JsonProperty("score")]
        public string score { get; set; }

        [JsonProperty("c1")]
        public string c1 { get; set; }

        [JsonProperty("c2")]
        public string c2 { get; set; }

        [JsonProperty("number")]
        public int number { get; set; }

        [JsonProperty("status")]
        public int status { get; set; }

        [JsonProperty("ross")]
        public int ross { get; set; }

        //[JsonProperty("bets")]
        //public Dictionary<int,int> bets { get; set; }
    }



    public class Result
    {
        [JsonProperty("games")]
        public Dictionary<int, Game> games { get; set; }

        [JsonProperty("bets")]
        public Dictionary<int, ZenitBet> bets { get; set; }

        [JsonProperty("odds")]
        public Dictionary<int, string> Odds { get; set; }

        [JsonProperty("html")]
        public Dictionary<int, string> Html { get; set; }
    }

    public class ZenitModel
    {

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("result")]
        public Result Result { get; set; }
    }
}
