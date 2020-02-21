using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using xNet;
using ABShared;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ABServer.Parsers.fonbetModel;
using ABShared.Enum;

namespace ABServer.Parsers
{
    internal class Fonbet : IParse
    {
        private string _url="https://fonbet5.com/";

        private string _apiUrl = "";

        private readonly HttpRequest _req = new HttpRequest();
        public BookmakerType Bookmaker { get; } = BookmakerType.Fonbet;

        public ConcurrentStack<Bet> Bets { get; set; } = new ConcurrentStack<Bet>();
        
        public bool UsingProxy { get; set; }
        public List<string> ProxyList { get; set; }

        private FonbetClient _fonbetClient;

        public void SetUrl(string url)
        {
            if (url == "")
                return;
            if (!url.EndsWith("/"))
                url = url + "/";
            if (!url.ToLower().StartsWith("https://") && !url.ToLower().StartsWith("http://"))
                url = "https://" + url;
            _url = url;
        }
        
        public List<Bet> Parse()
        {
            Bets.Clear();
            _req.CharacterSet = Encoding.UTF8;
            _req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";

            //_req.Proxy=ProxyClient.Parse(ProxyType.Http,"127.0.0.1:8888");

          

            Stopwatch sw = new Stopwatch();
            sw.Start();
           
            if (_fonbetClient == null)
            {
                _fonbetClient = new FonbetClient(_url);
                _fonbetClient.Start();
                
            }


            var line = _fonbetClient.GetLine();

           

            List<Bet> rez = new List<Bet>();
            var lastTime = (DateTime.Now - line.LastUpdate).TotalMilliseconds;
            if (lastTime > MainConfigurate.Configurate.FonbetMaxTime)
            {
                Logger.AddLog($"Последний раз обновляли линию {lastTime} мс назад. Это долго",Logger.LogTarget.Fonbet,Logger.LogLevel.Warn);
            }
            else
            {
                foreach (KeyValuePair<int, Event> @event in line.Events)
                {
                    var ev = @event.Value;

                    if (ev.IsBlock)
                        continue;
                    if (ev.Level != 1)
                        continue;

                    string team1 = ev.Team1;
                    string team2 = ev.Team2;
                    string sportName = line.Sports[ev.SportId].Name;

                    /*               
                     * 
                     * 974,1809,1812,1815       \   Итб1
                     * 976,1810,1813,1816       /   Итм1
                     * 
                     * 978,1854,1873,1880       \   Итб2
                     * 980,1871,1874,1881       /   Итм2
                     * 
                     * */


                    Bet bet = new Bet
                    {
                        URL = "live/?locale=ru#" + ev.Id,
                        Bookmaker = BookmakerType.Fonbet,
                        Team1 = team1,
                        Team2 = team2
                    };

                    if (ev.Team1Id == -1)
                        continue;
                    if (ev.Team2Id == -1)
                        continue;
                    bet.Team1Id = ev.Team1Id;
                    bet.Team2Id = ev.Team2Id;


                    IBTBet ibt1 = new IBTBet();
                    IBTBet ibt2 = new IBTBet();
                    ibt1.TeamName = bet.Team1;
                    ibt2.TeamName = bet.Team2;

                    bet.SportType = SportTypeHelper.Parse(sportName);

                    bet.Groupe = sportName.Split('.')[0].Trim();
                    bet.Name = team1 + " - " + team2;
                    bet.Site = "F";

                    bet = SetEvents(bet, ev);
                    if (bet == null)
                        continue;


                    if (ev.Factors.ContainsKey(974))
                    {
                        if (!ev.Factors[974].IsBlocked)
                        {
                            ibt1.Tmax = ev.Factors[974].V;
                            ibt1.Tmaxo = ev.Factors[974].E + "f974,974," + ev.Id;
                        }

                    }
                    if (ev.Factors.ContainsKey(976))
                    {
                        if (!ev.Factors[976].IsBlocked)
                        {
                            ibt1.Tmin = ev.Factors[976].V;
                            ibt1.Tmino = ev.Factors[976].E + "f976,976," + ev.Id;
                            ibt1.Coeff = SetValue(ev.Factors[976].Pt);
                        }

                    }

                    if (ev.Factors.ContainsKey(978))
                    {
                        if (!ev.Factors[978].IsBlocked)
                        {
                            ibt2.Tmax = ev.Factors[978].V;
                            ibt2.Tmaxo = ev.Factors[978].E + "f978,978," + ev.Id;
                            ibt2.Coeff = SetValue(ev.Factors[978].Pt);
                        }

                    }
                    if (ev.Factors.ContainsKey(980))
                    {
                        if (!ev.Factors[980].IsBlocked)
                        {
                            ibt2.Tmin = ev.Factors[980].V;
                            ibt2.Tmino = ev.Factors[980].E + "f980,980," + ev.Id;
                        }

                    }


                    if (ibt1.Isvalid())
                        bet.IBTList.Add(ibt1);
                    if (ibt2.Isvalid())
                        bet.IBTList.Add(ibt2);

                    //Достаем список доп. таймов к матчу
                    List<Event> times = line.GetAdditionTime(ev.Id);
                    foreach (Event time in times)
                    {

                        Bet betmini = bet.ShortCopy();
                        betmini = SetEvents(betmini, time, false);
                        if (betmini == null)
                            continue;

                        if (betmini.Games.Count != 0)
                        {
                            bet.Games.AddRange(betmini.Games);
                            betmini.Games.Clear();
                        }

                        Regex reg = new Regex("(.-)");
                        Match match = reg.Match(time.Name);
                        string partData = match.Value.Split('-').First();
                        if (time.Name.Contains("пол"))
                            partData = "1" + partData;

                        SportTimePart part = SportTimePartHelper.Parse(partData);
                        if (part == SportTimePart.Nan)
                        {
                            continue;
                        }
                        bet.Parts.Add(part, betmini);
                    }

                    rez.Add(bet);

                }
            }
            
            
            sw.Stop();
            if(sw.ElapsedMilliseconds<1000)
                Thread.Sleep(1000-(int)sw.ElapsedMilliseconds);
            Logger.AddLog($"Fonbet parse: {sw.Elapsed} мс. ставок: {rez.Count}; всего ставок: {line.Events.Count}",Logger.LogTarget.Fonbet);
            return rez;
        }

        private Bet SetEvents(Bet bet,Event ev,bool setSportTime=true)
        {
            if (ev.IsBlock)
                return bet;
            int eventId = ev.Id;
            Dictionary<int,CustomFactor> s = ev.Factors;


            Tuple<int, float> rez;

            float _1;
            rez = SetEvent(out _1, s, 921, 3150, 3144);
            bet._1 = _1;
            bet._1o = "event" + eventId + "win1," + rez.Item1+","+ eventId;
            //bet._1o = "event" + event_id + "win1";


            float _X;
            rez = SetEvent(out _X, s, 922, 3152);
            bet._X = _X;
            //bet._Xo = "event" + event_id + "draw";
            bet._Xo = "event" + eventId + "draw," + rez.Item1 + "," + eventId;


            float _2;
            rez = SetEvent(out _2, s, 923, 3151, 3145);
            bet._2 = _2;
            bet._2o = "event" + eventId + "win2," + rez.Item1 + "," + eventId;
            //bet._2o = "event" + event_id + "win2";


            float _1X;
            rez = SetEvent(out _1X, s, 924);
            bet._1X = _1X;
            bet._1Xo = "event" + eventId + "win1draw," + rez.Item1 + "," + eventId;
            //bet._1Xo = "event" + event_id + "win1draw";


            float _12 ;
            rez = SetEvent(out _12, s, 1571);
            bet._12 = _12;
            bet._12o = "event" + eventId + "win1win2," + rez.Item1 + "," + eventId;
            //bet._12o = "event" + event_id + "win1win2";

            float _X2;
            rez = SetEvent(out _X2, s, 925);
            bet._X2 = _X2;
            bet._X2o = "event" + eventId + "win2draw," + rez.Item1 + "," + eventId;
            //bet._X2o = "event" + event_id + "win2draw";

            float _F1;
            rez = SetEvent(out _F1, s, 927, 937, 1845);
            bet._F1_Cof = rez.Item2;
            bet._F1 = _F1;
            bet._F1o = "event" + eventId + "hand1odds," + rez.Item1 + "," + eventId;
            //bet._F1o = "event" + event_id + "hand1odds";


            float _F2;
            rez = SetEvent(out _F2, s, 928, 938, 1846);
            bet._F2_Cof = rez.Item2;
            bet._F2 = _F2;
            bet._F2o = "event" + eventId + "hand2odds," + rez.Item1 + "," + eventId;
            //bet._F2o = "event" + event_id + "hand2odds";

            float _B;
            rez = SetEvent(out _B, s, 930, 940, 1848);
            bet._Tmax = _B;
            bet._Tmaxo = "event" + eventId + "over," + rez.Item1 + "," + eventId;
            //bet._Tmaxo = "event" + event_id + "over";
            bet._Total_Cof= rez.Item2;
           

            float _T;
            rez = SetEvent(out _T, s, 931, 941, 1849);
            bet._Tmin = _T;
            bet._Tmino = "event" + eventId + "under," + rez.Item1 + "," + eventId;
            //bet._Tmino = "event" + event_id + "under";
            if(bet._Total_Cof==0)
            {
                bet._Total_Cof = rez.Item2;
            }
            else if(bet._Total_Cof!=rez.Item2)
            {
                if(rez.Item2!=0)
                {
                    
                }
            }
            

            if(setSportTime)
            {
              

                string timeData = ev.EventMisc?.Comment;
                if (string.IsNullOrWhiteSpace(timeData))
                    timeData = "";


                if (bet.SportType == SportType.Баскетбол)
                {
                    Regex reg = new Regex("\\(.*\\)"); // при перерыве время не показывает
                    MatchCollection m = reg.Matches(timeData);
                    if (m.Count == 0)
                    {
                        bet.Time = "1";
                    }
                    else
                        bet.Time = m[0].Value.Replace("(", "").Replace(")", "").Split(' ').Count().ToString();
                }

                else if (bet.SportType == SportType.Футбол)
                {
                    EventMisc xtr = ev.EventMisc;
                    if (xtr?.TimerSeconds != null)
                    {
                        string timerSeconds = xtr.TimerSeconds.ToString();
                        bet.Time = (Convert.ToInt32(timerSeconds) / 60) + "'";
                    }
                    else
                    {
                        bet.Time = "90'";
                    }
                }

                else if (bet.SportType == SportType.Бадминтон
                    || bet.SportType == SportType.Настольный_теннис
                    || bet.SportType == SportType.Теннис
                    || bet.SportType == SportType.Волейбол)
                {
                    Regex reg = new Regex("\\(.*\\)");
                    var m = reg.Matches(timeData);
                    if (m.Count == 0)
                        bet.Time = "1";
                    else
                        bet.Time = m[0].Value.Replace("(", "").Replace(")", "").Split(' ').Count().ToString();
                }

                else if (bet.SportType == SportType.Гандбол)
                {
                    var xtr = ev.EventMisc;
                    if (xtr != null)
                    {
                        var timerSeconds = xtr.TimerSeconds?.ToString();
                        var timeDirection = xtr.TimerDirection?.ToString();
                        if (timeDirection == "1")
                            bet.Time = "П";
                        else
                            bet.Time = (Convert.ToInt32(timerSeconds) / 60) + "'";
                    }
                }


                else if (bet.SportType == SportType.Хоккей)
                {
                    EventMisc xtr = ev.EventMisc;
                    if (xtr != null)
                    {
                        string timerSeconds = xtr.TimerSeconds?.ToString();

                        bet.Time = (Convert.ToInt32(timerSeconds) / 60)+ "'";
                    }
                    else
                    {
                       
                    }

                }
                else
                {
                    bet.Time = "-1";
                }

      
            }
            
            if (bet.SportType == SportType.Теннис)
            {
                
                var gameBet=ParseGameRezult(bet, eventId, s, 1609, 1610);   
                if(gameBet != null)
                {
                    bet.Games.Add(gameBet);
                }
                gameBet=ParseGameRezult(bet, eventId, s, 1747, 1748);
                if (gameBet != null)
                {
                    bet.Games.Add(gameBet);

                }
                gameBet =ParseGameRezult(bet, eventId, s, 1750, 1751);
                if (gameBet != null)
                {
                    bet.Games.Add(gameBet);
                }
                gameBet =ParseGameRezult(bet, eventId, s, 1753, 1754);
                if (gameBet != null)
                {
                    bet.Games.Add(gameBet);
                }
            }

            foreach (KeyValuePair<int, CustomFactor> factor in s)
            {
                if(factor.Value.IsBlocked)
                    continue;
                var stake = new Stake();

                
                if (factor.Key== 931
                    || factor.Key == 941
                    || factor.Key == 1849)
                {
                    stake.StakeType= StakeType.Tmin;
                    stake.ParametrO = $"event{eventId}under,{factor.Key},{eventId}";
                }
                else if (factor.Key == 930
                    || factor.Key == 940
                    || factor.Key == 1848)
                {
                    stake.StakeType = StakeType.Tmax;
                    stake.ParametrO = $"event{eventId}over,{factor.Key},{eventId}";
                }

                else if (factor.Key == 1697
                    || factor.Key == 1728
                    || factor.Key == 1731
                    || factor.Key == 1734
                    || factor.Key == 1737)
                {
                    stake.StakeType = StakeType.Tmin;
                    stake.ParametrO =  $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }

                else if (factor.Key == 1696
                    || factor.Key == 1727
                    || factor.Key == 1730
                    || factor.Key == 1733
                    || factor.Key == 1736)
                {
                    stake.StakeType = StakeType.Tmax;
                    stake.ParametrO = $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }

                else if (factor.Key == 927
                    || factor.Key == 937
                    || factor.Key == 1845)
                {
                    stake.StakeType = StakeType.Fora1;
                    stake.ParametrO = $"event{eventId}hand1odds,{factor.Key},{eventId}";
                }
                else if (factor.Key == 2427
                    || factor.Key == 2433
                    || factor.Key == 2436)
                {
                    if (factor.Key == 2427 && bet.SportType == SportType.Волейбол)
                        continue;
                    stake.StakeType = StakeType.Fora1;
                    stake.ParametrO = $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }

                
                else if (factor.Key == 928
                    || factor.Key == 938
                    || factor.Key == 1846)
                {
                    stake.StakeType = StakeType.Fora2;
                    stake.ParametrO = $"event{eventId}hand2odds,{factor.Key},{eventId}";
                }
                else if (factor.Key == 2428
                    || factor.Key == 2434
                    || factor.Key == 2437)
                {
                    if(factor.Key == 2428 && bet.SportType==SportType.Волейбол) 
                        continue;
                    stake.StakeType = StakeType.Fora2;
                    stake.ParametrO = $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }
               

                else if (factor.Key == 974
                         || factor.Key == 1809
                         || factor.Key == 1812
                         || factor.Key == 1815)
                {
                    stake.StakeType=StakeType.ITmax;
                    stake.Team=ETeam.Team1;
                    stake.ParametrO = $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }
                else if (factor.Key == 976
                         || factor.Key == 1810
                         || factor.Key == 1813
                         || factor.Key == 1816)
                {
                    stake.StakeType = StakeType.ITmin;
                    stake.Team = ETeam.Team1;
                    stake.ParametrO = $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }
                else if (factor.Key == 978
                         || factor.Key == 1854
                         || factor.Key == 1873
                         || factor.Key == 1880)
                {
                    stake.StakeType = StakeType.ITmax;
                    stake.Team = ETeam.Team2;
                    stake.ParametrO = $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }
                else if (factor.Key == 980
                         || factor.Key == 1871
                         || factor.Key == 1874
                         || factor.Key == 1881)
                {
                    stake.StakeType = StakeType.ITmin;
                    stake.Team = ETeam.Team2;
                    stake.ParametrO = $"{eventId}f{factor.Value.F},{factor.Key},{eventId}";
                }
                else
                {
                    continue;
                }

                stake.Coef = factor.Value.V;
                if (!String.IsNullOrWhiteSpace(factor.Value.Pt))
                    stake.Parametr = Convert.ToSingle(factor.Value.Pt.Replace(".",","));
                if(stake.StakeType==StakeType.Fora1
                    ||stake.StakeType==StakeType.Fora2)
                    bet.Foras.Add(stake);
                else if(stake.StakeType == StakeType.Tmax
                    || stake.StakeType == StakeType.Tmin)
                    bet.Totals.Add(stake);
                else if (stake.StakeType == StakeType.ITmax
                    || stake.StakeType == StakeType.ITmin)
                        bet.ITotals.Add(stake);
            }

            return bet;
        }

        private GameBet ParseGameRezult(Bet bet, int eventId, Dictionary<int,CustomFactor> s,int one,int two)
        {
            List<CustomFactor> gt = s.Where(x => (x.Key == one || x.Key == two) && !x.Value.IsBlocked).Select(x=>x.Value).OrderBy(x=>x.F).ToList();

            if (gt.Count == 2)
            {
                GameBet gameBet = new GameBet
                {
                    Team1 = bet.Team1,
                    Team2 = bet.Team2,
                    GameNumber = (TenisGamePart) Enum.Parse(typeof(TenisGamePart), gt[0].Pt),
                    Coef1 = gt[0].V,
                    Coef1o = eventId + "f" + gt[0].F + "," + gt[0].F+ "," + eventId,
                    Coef2 =gt[1].V,
                    Coef2o = eventId + "f" + gt[1].F + "," + gt[1].F + "," + eventId
                };


                return gameBet;
            }
            return null;
        }

       
        private static Tuple<int,float> SetEvent(out float param, Dictionary<int,CustomFactor> data,params int[] numbers)
        {
            param = 0;
            foreach (int key in numbers)
            {
                if (!data.ContainsKey(key))
                    continue;
                if(data[key].IsBlocked)
                    continue;
                param = data[key].V;
                if (data[key].Pt != null)
                    return new Tuple<int, float>(key, SetValue(data[key].Pt));
               return new Tuple<int, float>(key, 0);
            }
            return new Tuple<int, float>(0, 0);
        }


        private static float SetValue(string value)
        {
            float rezult;
            float.TryParse(value.Replace(".", ","), out rezult);
            return rezult;
        }

    }
    
}
