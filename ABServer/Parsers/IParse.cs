using System.Collections.Concurrent;
using System.Collections.Generic;
using ABShared;

namespace ABServer.Parsers
{
    internal interface IParse
    {

        bool UsingProxy { get; set; }

        BookmakerType Bookmaker { get; }

        ConcurrentStack<Bet> Bets { get; }

        List<string> ProxyList { get; set; }


        List<Bet> Parse();


        void SetUrl(string url);

    }
}
