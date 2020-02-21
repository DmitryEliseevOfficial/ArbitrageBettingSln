using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ABShared;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using HtmlAgilityPack;
using xNet;

namespace ABServer.Parsers
{
#if DEBUG

    internal class Parimatch:IParse
    {
        public bool UsingProxy { get; set; }
        public List<string> ProxyList { get; set; }

        private string _url= "https://www.parimatchbets2.com/";
        private readonly HttpRequest _req;
        public BookmakerType Bookmaker { get; } = BookmakerType.Parimatch;
        public ConcurrentStack<Bet> Bets { get; set; } = new ConcurrentStack<Bet>();

        public Parimatch()
        {
            _req = new HttpRequest
            {
                CharacterSet = Encoding.UTF8,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36 OPR/42.0.2393.94",
                ConnectTimeout = 1500,
                ReadWriteTimeout = 1500,
                KeepAliveTimeout = 1500
                
            };
        }

        public void SetUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;
            if (!url.EndsWith("/"))
                url = url + "/";
            if (!url.ToLower().StartsWith("https://") && !url.ToLower().StartsWith("http://"))
                url = "https://" + url;
            _url = url;
        }

        public List<Bet> Parse()
        {
            List<Bet> rezult = new List<Bet>();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (UsingProxy)
            {
                _req.Proxy=ProxyClient.Parse(ProxyList.GetRnd());
                _req.Proxy.ConnectTimeout = 1500;
                _req.Proxy.ReadWriteTimeout = 1500;
            }

            var response = _req.Get($"{_url}live.html").ToString();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);

            var links = doc.DocumentNode.SelectNodes("//a").Where(x => x.Attributes["href"] !=null && x.Attributes["href"].Value.StartsWith("bet.html?hl=")).Select((x=> x.Attributes["href"].Value)).ToList();

            List<Task<Bet>> tasks = new List<Task<Bet>>();

            foreach (string link in links)
            {
                Task<Bet> task = Task<Bet>.Factory.StartNew(delegate ()
                {
                    try
                    {
                        return ParsePage(link);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                                    Console.WriteLine("Parimatch.inner: " + ex.Message);
#endif
                                    return null;
                    }
                });
                tasks.Add(task);
            }
            
            //TODO:Нужно сделать нормальную обработку инфы потоками
            throw new NotImplementedException("Нужно сделать нормальную обработку инфы потоками");
            sw.Stop();
            Console.WriteLine($"Parimatch: {rezult.Count} {sw.Elapsed} мс");
            return rezult;
        }

        private static List<Bet> GetBets(Task<Bet>[] data)
        {
            return data.Where(x=>x?.Result?.Name != null ).Select(task => task.Result).ToList();
        }


        private Bet ParsePage(string href)
        {
#if DEBUGPAGE
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            HttpRequest req = new HttpRequest
            {
                CharacterSet = Encoding.GetEncoding(1251),
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36 OPR/42.0.2393.94"
            };
            if (UsingProxy)
            {
                req.Proxy = ProxyClient.Parse(ProxyList.GetRnd());
                req.Proxy.ConnectTimeout = 1500;
                req.Proxy.ReadWriteTimeout = 1500;
            }

            var response = req.Get($"{_url}{href}").ToString();

            HtmlParser parser = new HtmlParser();

            IHtmlDocument document = parser.Parse(response);
            IElement oddsList = document.GetElementById("oddsList");
            Bet bet = new Bet
            {
                Bookmaker = BookmakerType.Parimatch,
                Site = "P",
                SportType = SportTypeHelper.Parse(oddsList.Children[0].ChildNodes[1].ChildNodes[2].TextContent),
                Groupe = oddsList.Children[0].ChildNodes[1].ChildNodes[2].TextContent,
                Time = "0",
                URL = href
            };

           
            List<IElement> tables = document.QuerySelectorAll("div#oddsList div.wrapper > table > tbody").ToList();
            if (tables == null)
                throw new ArgumentNullException("Parimatch: Ошибка получения таблицы со ставками");

            IElement table = tables[1].Children.FirstOrDefault();
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            


            //тупая система. Иногда есть картинка между командами
            //иногда команды обернуты в доп. теги
            bet.Team1 = table.Children[1].ChildNodes[0].TextContent;
            if (bet.Team1.Contains("угловые"))
                throw new ArgumentException("Parimatch: статика");
            for (int i = 1; i < table.Children[1].ChildNodes.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(table.Children[1].ChildNodes[i].TextContent))
                {
                    bet.Team2 = table.Children[1].ChildNodes[i].TextContent;
                    break;
                }
            }

            bet.Name = bet.Team1 + " - " + bet.Team2;
            //получаем заголовок таблицы
            //и таблицу
            IElement tableHeader = tables[0].Children.FirstOrDefault();
            IElement tableMainDesc = tables[1].Children.FirstOrDefault();

            if (tableMainDesc != null)
            {
                tableMainDesc = FixTable(tableMainDesc);
            }


            if (tableHeader == null
                || tableMainDesc == null
                || tableHeader.ChildElementCount != tableMainDesc.ChildElementCount)
                throw new ArgumentException("Parimatch main table is corrupt");
   

            ParseBet(bet, tableHeader, tableMainDesc);



            

            if (tables.Count == 3)
            {
                IHtmlCollection<IElement> dopTables =
                    tables[2].Children.Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "bk")
                        .ToCollection();
          
                for (int i = 0; i < dopTables.Length; i++)
                {
                    var dopTable = dopTables[i];

                    if (dopTable.TextContent.Contains("Прием ставок приостановлен"))
                        continue;
                    dopTable = FixTable(dopTable);
                    if (dopTable.ChildElementCount != tableHeader.ChildElementCount)
                        throw new ArgumentException();

                    var dopBet = bet.ShortCopy();

                    var t = dopTable.Children[1].TextContent.Split('-').First();
                    if (dopBet.SportType == SportType.Баскетбол)
                    {
                        if (dopTable.Children[1].TextContent.Contains("половина"))
                        {
                            if (dopTable.Children[1].TextContent.Contains("Первая половина:"))
                                t = "11";
                            else if(dopTable.Children[1].TextContent.Contains("Вторая половина:"))
                                t = "12";

                        }
                    }

                    var timePart = SportTimePartHelper.Parse(t);
                    
                    if(timePart== SportTimePart.Nan)
                        continue;

                    ParseBet(dopBet, tableHeader, dopTable);
                    bet.Parts[timePart] = dopBet;
                }
        

      

                if (bet.SportType == SportType.Теннис)
                {
                    foreach (IElement tr in tables[2].Children)
                    {
                        Regex reg = new Regex("Сет . гейм .: победа");
                        if (reg.IsMatch(tr.TextContent))
                        {
                            var data = tr.Children[1].Children[0].TextContent.Split(' ');
                            GameBet gmBet = new GameBet();
                            gmBet.Set = SportTimePartHelper.Parse(data[1]);
                            gmBet.Team1 = bet.Team1;
                            gmBet.Team2 = bet.Team2;
                            gmBet.GameNumber = (TenisGamePart) Enum.Parse(typeof(TenisGamePart), data[3].Replace(":",""));
                            gmBet.Coef1 = TryFloatParse(tr.Children[1].Children[1].Children[0].Children.FirstOrDefault()?.Children.FirstOrDefault()?.TextContent);
                            if(gmBet.Coef1!=0)
                                gmBet.Coef1o = tr.Children[1].Children[1].Children[0].Children[0].Children[0].Id;
                            gmBet.Coef2 = TryFloatParse(tr.Children[1].Children[2].Children[0].Children.FirstOrDefault()?.Children.FirstOrDefault()?.TextContent);
                            if (gmBet.Coef2 != 0)
                                gmBet.Coef2o = tr.Children[1].Children[2].Children[0].Children[0].Children[0].Id;

                            bet.Games.Add(gmBet);
                        }
                        
                    }
                }

            }





#if DEBUGPAGE
            Console.WriteLine(bet);
            Console.WriteLine($"Parimatch pageparse: {href} {sw.Elapsed} мс");
#endif

            return bet;
        }


        private static void ParseBet(Bet bet, IElement tableHeader, IElement tableMainDesc)
        {
            for (int i = 2; i < tableHeader.ChildElementCount; i++)
            {
                if (tableMainDesc.TextContent.Contains("Прием ставок приостановлен"))
                    break;
                var headerString = tableHeader.ChildNodes[i].TextContent;
                if (headerString == "Фора")
                {
                    if (tableMainDesc.Children[i].ChildElementCount == 1)
                    {
                        bet._F1_Cof = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                        bet._F1 = TryFloatParse(tableMainDesc.Children[i + 1].Children.FirstOrDefault()?.TextContent);
                        bet._F1o = tableMainDesc.Children[i + 1].Children.FirstOrDefault()?.Id;
                        if (bet._F1o == null)
                            bet._F1 = 0;

                    }
                    else if (tableMainDesc.Children[i].ChildElementCount == 2)
                    {
                        bet._F1_Cof = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                        bet._F1 = TryFloatParse(tableMainDesc.Children[i + 1].Children.FirstOrDefault()?.TextContent);
                        bet._F1o = tableMainDesc.Children[i + 1].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                        if (bet._F1o == null)
                            bet._F1 = 0;

                        bet._F2_Cof = TryFloatParse(tableMainDesc.Children[i].Children[1].TextContent);
                        bet._F2 = TryFloatParse(tableMainDesc.Children[i + 1].Children[1].TextContent);
                        bet._F2o = tableMainDesc.Children[i + 1].Children[1].Children[0].Children.FirstOrDefault()?.Id;
                        if (bet._F2o == null)
                            bet._F2 = 0;
                    }
                    i++;
                }
                else if (headerString == "Т")
                {
                    bet._Total_Cof = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    bet._Tmax = TryFloatParse(tableMainDesc.Children[i + 1].Children.FirstOrDefault()?.TextContent);
                    if(bet._Tmax!=0)
                        bet._Tmaxo = tableMainDesc.Children[i + 1].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._Tmaxo == null)
                        bet._Tmax = 0;


                    bet._Tmin = TryFloatParse(tableMainDesc.Children[i + 2].Children.FirstOrDefault()?.TextContent);
                    if(bet._Tmin!=0)
                        bet._Tmino = tableMainDesc.Children[i + 2].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._Tmino == null)
                        bet._Tmin = 0;

                    i = i + 2;
                }
                else if (headerString == "П1")
                {
                    bet._1 = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    if (bet._1 != 0)
                        bet._1o = tableMainDesc.Children[i].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._1o == null)
                        bet._1 = 0;
                }
                else if (headerString == "X")
                {
                    bet._X = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    if (bet._X != 0)
                        bet._Xo = tableMainDesc.Children[i].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._Xo == null)
                        bet._X = 0;
                }
                else if (headerString == "П2")
                {
                    bet._2 = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    if (bet._2 != 0)
                        bet._2o = tableMainDesc.Children[i].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._2o == null)
                        bet._2 = 0;
                }

                else if (headerString == "1X")
                {
                    bet._1X = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    if (bet._1X != 0)
                        bet._1Xo = tableMainDesc.Children[i].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._1Xo == null)
                        bet._1X = 0;
                }

                else if (headerString == "12")
                {
                    bet._12 = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    if (bet._12 != 0)
                        bet._12o = tableMainDesc.Children[i].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._12o == null)
                        bet._12 = 0;
                }

                else if (headerString == "X2")
                {
                    bet._X2 = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    if (bet._X2 != 0)
                        bet._X2o = tableMainDesc.Children[i].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (bet._X2o == null)
                        bet._X2 = 0;
                }
                else if (headerString == "iТ")
                {
                    IBTBet ibet = new IBTBet();
                    ibet.TeamName = bet.Team1;

                    ibet.Coeff = TryFloatParse(tableMainDesc.Children[i].Children.FirstOrDefault()?.TextContent);
                    ibet.Tmax = TryFloatParse(tableMainDesc.Children[i + 1].Children.FirstOrDefault()?.TextContent);
                    if (ibet.Tmax != 0)
                        ibet.Tmaxo = tableMainDesc.Children[i + 1].Children[0].Children[0].Children.FirstOrDefault()?.Id;

                    ibet.Tmin = TryFloatParse(tableMainDesc.Children[i + 2].Children.FirstOrDefault()?.TextContent);
                    if (ibet.Tmin != 0)
                        ibet.Tmino = tableMainDesc.Children[i + 2].Children[0].Children[0].Children.FirstOrDefault()?.Id;
                    if (ibet.Isvalid())
                        bet.IBTList.Add(ibet);

                    if (tableMainDesc.Children[i].Children.Length >= 2)
                    {
                        IBTBet ibet2 = new IBTBet();

                        ibet2.TeamName = bet.Team2;
                        ibet2.Coeff = TryFloatParse(tableMainDesc.Children[i].Children[1]?.TextContent);
                        ibet2.Tmax = TryFloatParse(tableMainDesc.Children[i + 1].Children[1].TextContent);
                        if (ibet2.Tmax != 0)
                            ibet2.Tmaxo = tableMainDesc.Children[i + 1].Children[1].Children[0].Children[0].Id;

                        ibet2.Tmin = TryFloatParse(tableMainDesc.Children[i + 2].Children[1]?.TextContent);
                        if (ibet2.Tmin != 0)
                            ibet2.Tmino = tableMainDesc.Children[i + 2].Children[1].Children[0].Children[0].Id;
                        if (ibet2.Isvalid())
                            bet.IBTList.Add(ibet2);
                    }

                }
            }
        }

        private IElement FixTable(IElement rowTable)
        {
            var rezult =  rowTable.Clone(false);
            
            foreach (IElement rw in rowTable.Children)
            {
                if (rw.Attributes["colspan"] != null)
                {
                    int count = Int32.Parse(rw.Attributes["colspan"].Value);
                    var elNew = rw.Clone();
                    
                    for (int i = 0; i < count; i++)
                    {
                        rezult.AppendChild(rw.Clone());
                    }
                }
                else
                {
                    rezult.AppendChild(rw.Clone());
                }
            }
            return rezult as IElement;
        } 

        private static float TryFloatParse(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return 0;
            data = data.Replace("–", "-");
            float rez;
            float.TryParse(data, NumberStyles.Any,CultureInfo.InvariantCulture,out rez);
            return rez;
        }
    }
#endif
}
