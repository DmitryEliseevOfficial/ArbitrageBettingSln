using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ABShared;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using xNet;

namespace ABServer.Parsers.MarafonModel
{
    internal class MarafonClient
    {
        private readonly string _url;
        private readonly HttpRequest _req;
        private readonly bool _usingProxy;
        private readonly List<string> _proxyList;


        private List<int> _currentGames = new List<int>();


        private readonly object _updateobj = new object(); 


        private readonly Thread _updateThread;


        private ConcurrentStack<Bet> _bets = new ConcurrentStack<Bet>();

        public MarafonClient(string url, bool usingProxy, List<string> proxyList)
        {
            _url = url;
            _usingProxy = usingProxy;
            _proxyList = proxyList;
            _req = new HttpRequest
            {
                CharacterSet = Encoding.UTF8
            };
            _updateThread = new Thread(Update)
            {
                IsBackground = true
            };
            _updateThread.Start();
        }


 
        private void Update()
        {
            while (true)
            {
                GetCurrentLive();
                Thread.Sleep(5000);
            }
        }


        private void GetCurrentLive()
        {
            try
            {
                if (_usingProxy)
                {
                    _req.Proxy = ProxyClient.Parse(_proxyList.GetRnd());
                    _req.Proxy.ReadWriteTimeout = 1500;
                    _req.Proxy.ConnectTimeout = 1500;
                }

                string respone = _req.Get($"{_url}/su/live/popular").ToString();
                var parser = new HtmlParser();
                var doc = parser.Parse(respone);
                var lives = doc.QuerySelectorAll("div.event-info");

                List<int> eventsId = new List<int>();
                foreach (IElement element in lives)
                {
                    var id = element.Attributes["data-event-treeid"].Value;
                    int correctid;
                    if (Int32.TryParse(id, out correctid))
                        eventsId.Add(correctid);
                }

                lock (_updateobj)
                {
                    _currentGames = eventsId;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Marafon: не удолось получить список live.");
                // _currentGames = new List<int>();
            }
        }


        

        internal List<Bet> GetBets()
        {
            _bets=new ConcurrentStack<Bet>();

            List<int> ids=new List<int>();
            lock (_updateobj)
            {
                ids.AddRange(_currentGames);
            }
            List<Thread> ths = new List<Thread>();
            foreach (int id in ids)
            {
                var th = new Thread(ParsePage)
                {
                    IsBackground = true
                };
                th.Start(id);

                ths.Add(th);
            }

            Thread.Sleep(2500);

            foreach (Thread th in ths)
            {
                try
                {
                    th.Abort();
                }
                catch
                {
                    
                }
            }

            return _bets.ToList();
        }


        private void ParsePage(object o)
        {
            var internalBet = new Bet();

            int eventId = Convert.ToInt32(o);
            var req = new HttpRequest();
            req.CharacterSet = Encoding.UTF8;
            string respone = req.Get($"{_url}/su/live/{eventId}").ToString();
            var parser = new HtmlParser();
            var doc = parser.Parse(respone);

           
            var teams = doc.QuerySelectorAll("div.live-today-member-name.nowrap");
            if (teams.Length != 2)
            {
#if DEBUG
                Console.WriteLine("marafon: неудолсь получить команды");
#endif
            }

            internalBet.Team1 = teams[0].TextContent;
            internalBet.Team2 = teams[1].TextContent;

            var bets = doc.QuerySelectorAll("td.price.height-column-with-price");



            foreach (IElement bet in bets)
            {
                var html = bet.InnerHtml;

                if (html.Contains("Result.1"))
                {
                    //Победа
                    internalBet._1 = GetCorectValue(bet.Children[0]);
                    internalBet._1o = bet.Children[0].Attributes["data-selection-key"].Value;
                }
                else if (html.Contains("Result.draw"))
                {
                    //X
                    internalBet._X = GetCorectValue(bet.Children[0]);
                    internalBet._Xo = bet.Children[0].Attributes["data-selection-key"].Value;
                }
                else if (html.Contains("Result.3"))
                {
                    internalBet._2 = GetCorectValue(bet.Children[0]);
                    internalBet._2o = bet.Children[0].Attributes["data-selection-key"].Value;
                }
                else if (html.Contains("Result0.HD"))
                {
                    internalBet._1X = GetCorectValue(bet.Children[0]);
                    internalBet._1Xo = bet.Children[0].Attributes["data-selection-key"].Value;
                }
                else if (html.Contains("Result0.HA"))
                {
                    internalBet._12 = GetCorectValue(bet.Children[0]);
                    internalBet._12o = bet.Children[0].Attributes["data-selection-key"].Value;
                }
                else if (html.Contains("Result0.AD"))
                {
                    internalBet._X2 = GetCorectValue(bet.Children[0]);
                    internalBet._X2o = bet.Children[0].Attributes["data-selection-key"].Value;
                }

                else if (html.Contains("To_Win_Match_With_Handicap"))
                {
                    if (bet.Attributes["data-market-type"]?.Value == "HANDICAP")
                    {
                        if (html.Contains("HB_H"))
                        {
                            internalBet._F1 = GetCorectValue(bet.Children[1]);
                            internalBet._F1o = bet.Children[1].Attributes["data-selection-key"].Value;
                            internalBet._F1_Cof = GetValue(bet.ChildNodes[0].TextContent);
                        }
                        else
                        {
                            internalBet._F2 = GetCorectValue(bet.Children[1]);
                            internalBet._F2o = bet.Children[1].Attributes["data-selection-key"].Value;
                            internalBet._F2_Cof = GetValue(bet.ChildNodes[0].TextContent);
                        }

                    }

                }

                else if (html.Contains("Total_Goals"))
                {
                    if (bet.Attributes["data-market-type"]?.Value == "TOTAL")
                    {
                        if (html.Contains("Under"))
                        {
                            internalBet._Tmin = GetCorectValue(bet.Children[1]);
                            internalBet._Tmino = bet.Children[1].Attributes["data-selection-key"].Value;
                            internalBet._Total_Cof = GetValue(bet.ChildNodes[0].TextContent);
                        }
                        else
                        {
                            internalBet._Tmax = GetCorectValue(bet.Children[1]);
                            internalBet._Tmaxo = bet.Children[1].Attributes["data-selection-key"].Value;
                            internalBet._Total_Cof = GetValue(bet.ChildNodes[0].TextContent);
                        }
                    }

                }

            }

            _bets.Push(internalBet);
        }

        private static float GetCorectValue(IElement node)
        {
            if (node.Attributes["data-selection-price"] == null)
                return 0;
            else
            {
                string temp = node.Attributes["data-selection-price"].Value.Trim().Replace(".", ",");
                return Convert.ToSingle(temp);
            }
        }

        private static float GetValue(string text)
        {
            text = text.Replace("(", "").Replace(")", "").Trim().Replace(".", ",");
            return Convert.ToSingle(text);
        }
    }
}
