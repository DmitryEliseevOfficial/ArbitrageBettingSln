using System.Diagnostics;

namespace ABServer.Parsers
{

    internal class LinkData
    {
        public LinkData()
        {
        }

        public LinkData(string id, string teams)
        {
            Id = id;
            Teams = teams;
        }

        public string Id { get; set; }

        public string Teams { get; set; }


        public string TimeData { get; set; }


        public int Team1Id { get; set; } = -1;


        public int Team2Id { get; set; } = -1;

        public string Score { get; set; }

    }
}
