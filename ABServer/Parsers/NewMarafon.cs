using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ABServer.Parsers.MarafonModel;
using ABShared;
using xNet;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StaticData.Shared.Model;
using ThreadState = System.Threading.ThreadState;

namespace ABServer.Parsers
{
    class NewMarafon : IParse
    {
        private string _url = "https://www.mthbet27.com";
        public BookmakerType Bookmaker { get; } = BookmakerType.Marafon;
        public ConcurrentStack<Bet> Bets { get; set; } = new ConcurrentStack<Bet>();

        private UnicDataDecorator _bd;

        private HttpRequest _req;


        public bool UsingProxy { get; set; }
        public List<string> ProxyList { get; set; }

        public void SetUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return;
            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);
            if (url.EndsWith("/su"))
                url = url.Substring(0, url.Length - 3);
            _url = url;
        }

        public List<Bet> Parse()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Bets.Clear();
            if (_thUpdate == null)
            {
                _thUpdate = new Thread(Update);
                _thUpdate.IsBackground = true;
                _thUpdate.Start();
                Logger.AddLog($"Запустили Updater. Ушло времени {sw.ElapsedMilliseconds} мс", Logger.LogTarget.Marafon, Logger.LogLevel.Info);
            }

            List<Bet> rezult = new List<Bet>();
            foreach (KeyValuePair<string, Bet> key in _bets)
            {
                if (key.Value == null)
                    continue;
                var deltaTime = DateTime.Now - key.Value.Created;
                if (deltaTime.TotalMilliseconds < MainConfigurate.Configurate.MarafonMaxTime)
                    rezult.Add(key.Value);
            }
            sw.Stop();
            Logger.AddLog("На парсинг времени затраченно: " + sw.ElapsedMilliseconds + ". Собранно вилок: " + rezult.Count, Logger.LogTarget.Marafon);
            Thread.Sleep(1000);

            return rezult;
        }




        internal List<LinkData> GetLives()
        {
            var rezult = new List<LinkData>();
            if (_req == null)
            {
                _req = new HttpRequest();
                _req.CharacterSet = Encoding.UTF8;
                _req.UserAgent =
                        "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36 OPR/41.0.2353.46";
                //if (UsingProxy)
                //    _req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
            }

            string respone = _req.Get($"{_url}/su/live/popular").ToString();
            var pr = new HtmlParser();
            var doc = pr.Parse(respone);
            var lives = doc.QuerySelectorAll("div.event-info");


            foreach (IElement element in lives)
            {
                var id = element.Attributes["data-event-treeid"].Value;
                var linkdata = new LinkData();
                linkdata.Id = id;
                try
                {
                    linkdata.Teams = element.Children[2].TextContent.Replace(" @ ", " - ");
                    linkdata.Score = element.Children[element.ChildElementCount - 2].FirstElementChild.TextContent;
                    if (element.Children[element.ChildElementCount - 2].ChildElementCount == 2)
                    {
                        linkdata.TimeData = element.Children[element.ChildElementCount - 2].LastElementChild.TextContent;
                    }
                }
                catch (Exception e)
                {
                    continue;

                }

                rezult.Add(linkdata);
            }


            if (_bd == null)
            {
                _bd = new UnicDataDecorator(UnicData.Load("bd.data"));
                UnicDataDecorator.UpdateBase += UnicDataDecorator_UpdateBase;
            }




            var dt = new List<LinkData>();
            foreach (LinkData linkData in rezult)
            {
                var teams = linkData.Teams.Replace(" - ", "|").Split('|');
                if (teams.Length != 2)
                    continue;
                linkData.Team1Id = _bd.GetNumber(teams[0]);
                if (linkData.Team1Id == -1)
                    continue;
                linkData.Team2Id = _bd.GetNumber(teams[1]);
                if (linkData.Team2Id == -1)


                    dt.Add(linkData);
            }

            Logger.AddLog($"Всего lives: {rezult.Count}. Из них отобранно {dt.Count}", Logger.LogTarget.Marafon);

            return dt;
        }

        private void UnicDataDecorator_UpdateBase(List<UnicData> obj)
        {
            _bd.SetBase(obj);
        }


        private Thread _thUpdate;
        private List<LinkData> _livesList = new List<LinkData>();
        private readonly object _livesLock = new object();

        private readonly Dictionary<string, Thread> _thsLoad = new Dictionary<string, Thread>();

        private void Update()
        {
            while (true)
            {
                try
                {
                    var rezult = GetLives();
                    lock (_livesLock)
                    {
                        _livesList = rezult;
                    }
                    Logger.AddLog($"Полученно lives: {rezult.Count}", Logger.LogTarget.Marafon);
                    foreach (LinkData data in rezult)
                    {
                        if (!_thsLoad.ContainsKey(data.Id))
                        {

                            var th = new Thread(UpdateBetNew);
                            th.IsBackground = true;
                            th.Start(data);
                            _thsLoad.Add(data.Id, th);

                        }
                        else
                        {
                            if (_thsLoad[data.Id].ThreadState == ThreadState.Stopped
                                || _thsLoad[data.Id].ThreadState == ThreadState.Aborted)
                            {
                                var th = new Thread(UpdateBetNew);
                                th.IsBackground = true;
                                th.Start(data);
                                _thsLoad[data.Id] = th;
                            }
                        }
                    }

                    var ids = rezult.Select(x => x.Id).ToList();
                    List<string> removeList = new List<string>();
                    foreach (KeyValuePair<string, Thread> key in _thsLoad)
                    {
                        if (!ids.Contains(key.Key))
                        {
                            try
                            {
                                key.Value.Abort();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            removeList.Add(key.Key);
                        }
                    }

                    foreach (string s in removeList)
                    {
                        _thsLoad.Remove(s);
                    }
                }

                catch (Exception ex)
                {
                    if (UsingProxy)
                        _req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
                    Logger.AddLog($"Не удалось получить lives: {ex.Message}", Logger.LogTarget.Marafon,
                        Logger.LogLevel.Critical);
                    continue;
                }

                finally
                {
                    Thread.Sleep(10000);
                }
            }
        }



        //Метод для теста. Без прокси
        internal void GetBetsTest()
        {
            var ids = GetLives();
            foreach (LinkData linkData in ids)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                HttpRequest req = new HttpRequest();

                var response = req.Get($"{_url}/su/live/{linkData.Id}").ToString();
                var bet = new Bet();
                bet.Bookmaker = BookmakerType.Marafon;
                bet.Site = "M";
                bet.Name = linkData.Teams;
                bet.URL = $"su/live/{linkData.Id}";
                bet.Team1Id = linkData.Team1Id;
                bet.Team2Id = linkData.Team2Id;
                var teams = linkData.Teams.Replace(" - ", "|").Split('|');
                if (teams.Length < 2)
                    throw new ArgumentException($"{nameof(teams)}.Lenght!=2");

                bet.Team1 = teams[0];
                bet.Team2 = teams[1];


                var doc = CreteDocument(response);
                bet = ParseBet(doc, bet);
                _bets[linkData.Id] = bet;
                sw.Stop();
                Console.WriteLine($"NewMarafon Parse: {sw.ElapsedMilliseconds}");
            }
        }

        ConcurrentDictionary<string, Bet> _bets = new ConcurrentDictionary<string, Bet>();



        internal void UpdateBet(object data)
        {
            var link = data as LinkData;
            if (link == null)
                return;

            Stopwatch sw = new Stopwatch();

            HttpRequest req = new HttpRequest();
            req.CharacterSet = Encoding.UTF8;
            if (UsingProxy)
                req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());

            while (true)
            {

                try
                {

                    var response = req.Get($"{_url}/su/live/{link.Id}").ToString();
                    sw.Reset();
                    sw.Start();
                    var bet = new Bet();
                    bet.Bookmaker = BookmakerType.Marafon;
                    bet.Site = "M";
                    bet.Name = link.Teams;
                    bet.URL = $"su/live/{link.Id}";
                    bet.Team1Id = link.Team1Id;
                    bet.Team2Id = link.Team2Id;
                    var teams = link.Teams.Replace(" - ", "|").Split('|');
                    if (teams.Length < 2)
                        throw new ArgumentException($"{nameof(teams)}.Lenght!=2");

                    bet.Team1 = teams[0];
                    bet.Team2 = teams[1];


                    var doc = CreteDocument(response);
                    bet = ParseBet(doc, bet);

                    var time = (int)sw.ElapsedMilliseconds;
                    sw.Stop();
                    _bets[link.Id] = bet;
                    if (time > MainConfigurate.Configurate.MarafonMaxTime)
                        throw new ArgumentException("Долго парсили");
                }
                catch (ThreadAbortException ex)

                {
                    break;
                }
                catch (Exception ex)
                {

                    Logger.AddLog($"{link.Id};URl: {_url}/su/live/{link.Id} Время {sw.ElapsedMilliseconds};Ошибка парсинга: {ex.Message}", Logger.LogTarget.MarafonThread, Logger.LogLevel.Warn);
                }

                if (UsingProxy)
                {
                    req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
                    req.ReadWriteTimeout = 3000;
                    req.ConnectTimeout = 3000;
                    req.KeepAliveTimeout = 3000;
                }

            }
        }


        internal void UpdateBetNew(object data)
        {
            var link = data as LinkData;
            if (link == null)
                return;

            Stopwatch sw = new Stopwatch();

            HttpRequest req = new HttpRequest();
            req.CharacterSet = Encoding.UTF8;
            if (UsingProxy)
                req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
            int offset = 0;
            try
            {
                offset = GetServerOffset(req);
            }
            catch (Exception ex)
            {
                Logger.AddLog($"Не смогли получить смещение времи. {ex.Message}", Logger.LogTarget.MarafonThread, Logger.LogLevel.Error);
            }

            string prefix = GeneratorPrefix();
            string updated = null;
            IHtmlDocument doc = null;
            bool reload = true;
            while (true)
            {
                sw.Reset();
                sw.Start();

                try
                {
                    if (reload)
                    {
                        string response = req.Get($"{_url}/su/live/{link.Id}").ToString();
                        reload = false;
                        var bet = new Bet();
                        bet.Bookmaker = BookmakerType.Marafon;
                        bet.Site = "M";
                        bet.Name = link.Teams;
                        bet.URL = $"su/live/{link.Id}";
                        bet.Team1Id = link.Team1Id;
                        bet.Team2Id = link.Team2Id;
                        var teams = link.Teams.Replace(" - ", "|").Split('|');
                        if (teams.Length < 2)
                            throw new ArgumentException($"{nameof(teams)}.Lenght!=2");

                        bet.Team1 = teams[0];
                        bet.Team2 = teams[1];


                        doc = CreteDocument(response);
                        bet = ParseBet(doc, bet);

                        var time = (int)sw.ElapsedMilliseconds;
                        sw.Stop();
                        _bets[link.Id] = bet;
                        if (time > MainConfigurate.Configurate.MarafonMaxTime)
                            throw new ArgumentException($"Долго парсили {sw.ElapsedMilliseconds} мс ");
                        Console.WriteLine("Событие загруженно!");
                    }
                    else
                    {
                        if (!_bets.ContainsKey(link.Id))
                        {
                            Console.WriteLine("Почему то нет такого ключа в базе.");
                            reload = true;
                            continue;
                        }
                        if (doc == null)
                        {
                            Console.WriteLine($"Почему то {nameof(doc)}==null");
                            reload = true;
                            continue;
                        }
                        if (String.IsNullOrWhiteSpace(updated))
                            updated = (ConvertToUnixTimestamp() - offset).ToString();
                        string url = $"https://liveupdate.marathonbookmakers.com/su/livestreamupdate/{link.Id}?callback={prefix}&markets={link.Id}&available=&updated={updated}&oddsType=Decimal&siteStyle=MULTIMARKETS&_={ConvertToUnixTimestamp()}";
                        var response = req.Get(url).ToString().Trim();
                        var responseData = response.Remove(response.Length - 1).Replace($"{prefix}(", "");
                        if (responseData.Contains("refreshPage"))
                        {
                            Console.WriteLine("Необходимо перезагрузить странницу");
                            reload = true;
                            updated = null;
                            continue;
                        }
                        var json = JsonConvert.DeserializeObject<MarafonPingResponse>(responseData);
                        updated = json.Updated.ToString();
                        if (json.Modified == null)
                        {
                            Console.WriteLine("Обновление ставки не требуется!");
                            _bets[link.Id].Update();
                        }
                        else
                        {

                            foreach (Modified modified in json.Modified)
                            {
                                if (modified.Type == "mutableUpdates")
                                {
                                    try
                                    {
                                        UpdateDocument(doc, modified.Updates);
                                    }
                                    catch (Exception e)
                                    {
                                        reload = true;
                                        updated = null;
                                        throw new ArgumentException($"Не удалось сделать частичное обновлеение. и вот почему: {e.Message}");
                                    }

                                }
                            }

                            var bet = _bets[link.Id].ShortCopy();
                            bet = ParseBet(doc, bet);
                            _bets[link.Id] = bet;
                            Console.WriteLine("Сделали частичное обновление!");
                        }


                        Thread.Sleep(1500);
                    }
                }
                catch (Exception e)
                {
                    Logger.AddLog($"Не удалось спарсить {link.Id}. И вот почему: {e.Message}", Logger.LogTarget.MarafonThread, Logger.LogLevel.Warn);
                    Debug.WriteLine($"Не удалось спарсить {link.Id}. И вот почему: {e.Message}");
                    Thread.Sleep(5000);
                    if (UsingProxy)
                        req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
                }

            }
        }

        HtmlParser parser = new HtmlParser();

        internal void UpdateDocument(IHtmlDocument doc, Dictionary<string, UpdateData> updateData)
        {
            foreach (var key in updateData)
            {
                if (key.Key == "shortcuts")
                    continue;
                string query = $"[data-mutable-id='{key.Key}']";
                var element = doc.QuerySelectorAll(query).FirstOrDefault();
                if (element == null)
                    throw new ArgumentException("Данные устарели. Нужно перезагружаться");

                var dt = parser.Parse(key.Value.Html);

                element.Replace(dt.Body.FirstChild);
                Debug.WriteLine($"Обновили {key.Key}");
            }
        }


        internal IHtmlDocument CreteDocument(string htmlSource)
        {
            var parser = new HtmlParser();
            return parser.Parse(htmlSource);
        }

        internal Bet ParseBet(IHtmlDocument doc, Bet bet)
        {



            var groupe = doc.QuerySelector("a.sport-category-label");
            if (groupe == null)
                throw new ArgumentException($"{nameof(groupe)}==null");
            bet.Groupe = groupe.TextContent;
            bet.SportType = SportTypeHelper.Parse(bet.Groupe);


            var stakes = doc.QuerySelectorAll("span.selection-link");
            if (bet.SportType == SportType.Футбол)
            {
                MarafonParserHelper.Football(stakes, bet);
            }
            else if (bet.SportType == SportType.Хоккей)
            {
                MarafonParserHelper.Hockey(stakes, bet);
            }
            else if (bet.SportType == SportType.Баскетбол)
            {
                MarafonParserHelper.Backetball(stakes, bet);
            }
            else if (bet.SportType == SportType.Теннис)
            {
                MarafonParserHelper.Tennis(stakes, bet);
            }
            else if (bet.SportType == SportType.Волейбол)
            {
                MarafonParserHelper.Voleyball(stakes, bet);
            }
            else if (bet.SportType == SportType.Гандбол)
            {
                MarafonParserHelper.Ganball(stakes, bet);
            }
            else if (bet.SportType == SportType.Водное_поло)
            {
                MarafonParserHelper.Polo(stakes, bet);
            }
            else
            {
                MarafonParserHelper.CommonTemp(stakes, bet);
            }
            return bet;
        }

        internal static void UpdateEvent()
        {
            string id = "5115965";
            var req = new HttpRequest();
            //req.AddHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            RequestParams par = new RequestParams();
            par["t"] = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;


            int offset = GetServerOffset(req);
            string updated = (ConvertToUnixTimestamp() - offset).ToString();
            string prefix = GeneratorPrefix();
            while (true)
            {
                string url = $"https://liveupdate.marathonbookmakers.com/su/livestreamupdate/{id}?callback={prefix}&markets={id}&available=&updated={updated}&oddsType=Decimal&siteStyle=MULTIMARKETS&_={ConvertToUnixTimestamp()}";
                Console.WriteLine(url);
                var response = req.Get(url).ToString().Trim();
                var data = response.Remove(response.Length - 1).Replace($"{prefix}(", "");
                var json = JsonConvert.DeserializeObject<MarafonPingResponse>(data);
                updated = json.Updated.ToString();
                if (json.Modified == null)
                    Console.WriteLine(response);
                else
                {

                    foreach (Modified modified in json.Modified)
                    {
                        Console.WriteLine(modified.Type);
                    }
                }
                Thread.Sleep(1500);
            }
        }


        private static string GeneratorPrefix()
        {
            var rnd = new Random();
            return $"jQuery{rnd.Next(100000000, 999999999)}_{ConvertToUnixTimestamp()}";
        }


        public static double ConvertToUnixTimestamp()
        {
            var rez = DateTime.UtcNow
                .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalMilliseconds;
            return Math.Floor(rez);
        }

        private static int GetServerOffset(HttpRequest req)
        {
            var response = req.Post($"https://www.marathonbet.com/su/utils/synctime", $"t={ConvertToUnixTimestamp()}", "application/x-www-form-urlencoded; charset=UTF-8").ToString();
            var json = JObject.Parse(response);
            return Convert.ToInt32(json["offset"].ToString());
        }

    }

}
