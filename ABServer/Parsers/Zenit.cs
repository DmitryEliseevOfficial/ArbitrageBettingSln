using HtmlAgilityPack;
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
using ABServer.Parsers.MarafonModel;
using ABShared.Enum;
using Newtonsoft.Json;
using StaticData.Shared.Model;

namespace ABServer.Parsers
{
    internal class Zenit : IParse
    {

        private string _url = "https://55zenit.win/";
        private readonly HttpRequest _req = new HttpRequest();

        
        public BookmakerType Bookmaker { get; } = BookmakerType.Zenit;
        public ConcurrentStack<Bet> Bets { get; set; } = new ConcurrentStack<Bet>();

        public bool UsingProxy { get; set; }
        public List<string> ProxyList { get; set; }

        public Zenit()
        {
            _req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36 OPR/41.0.2353.46";
            _req.CharacterSet = Encoding.UTF8;        
        }

        public void SetUrl(string url)
        {
            if(url=="")
                return;
            if (!url.EndsWith("/"))
                url = url + "/";
            if (!url.ToLower().StartsWith("https://") && !url.ToLower().StartsWith("http://"))
                url = "https://" + url;
            _url = url;
        }

        public List<Bet> Parse()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            //if (UsingProxy)
            //{
            //    _req.Proxy = ProxyClient.Parse(ProxyList.GetRnd());
            //    _req.Proxy.ConnectTimeout = 1500;
            //    _req.Proxy.ReadWriteTimeout = 1500;
            //}

            if (_thUpdate == null)
            {
                _thUpdate=new Thread(Update);
                _thUpdate.IsBackground = true;
                _thUpdate.Start();
            }

            List<LinkData> lives;
            lock (_lockLive)
            {
                lives = _livesList.ToList();
            }

            if (lives.Count == 0)
            {
                Thread.Sleep(1000);
                Logger.AddLog("Zenit parse time: Нечего парсить. Ставок: " + Bets.Count, Logger.LogTarget.Zenit);
                return Bets.ToList();
            }
            Stack<Bet> betsStore = new Stack<Bet>();
            for (int i = 0; i < lives.Count+10; i++)
            {
                betsStore.Push(new Bet());
            }

            var ids = String.Join("-",lives.Select(x => x.Id));

            //Загружаем дополнительные росписи
            ZenitModel zenitModel = GetAdditionData();

            if(zenitModel.Result==null)
                zenitModel.Result= new Result();
            if(zenitModel.Result.Html==null)
                zenitModel.Result.Html= new Dictionary<int, string>();

            //Загружаем основную таблицу
            string respone = _req.Get($"{_url}ajax/live/get/0?ross=0&onlyview=0&all=0&&games={ids}").ToString();


            Bets.Clear();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);
            HtmlNodeCollection nodes = doc.DocumentNode.ChildNodes;

            
            for (int i=2;i< nodes.Count -2;i=i+2)
            {
                if (nodes[i].Name == "a")
                    i++;

                /**
                 * в каждой таблице содержиться в ChildNode[0] - Лига и название спорта
                 * а дальше идет по 3 tr. 
                 * в tr[1] - header - таблицы
                 * в tr[2] - основная роспись
                 *   tr[3] - пустота. отделитель 
                 * */

                for(int j =1;j<nodes[i].ChildNodes.Count;j++)
                {
                    //по факту в пределах одной таблицы данные могут чередоваться каждые 2 или 3 раза
                    //ЗАкономерность не понятна. Так обходим этот недочет
                    if(nodes[i].ChildNodes[j].InnerText.Contains("Событие"))
                    {
                        j++;
                    }
                    else
                    {
                        continue;
                    }

                    //Получаем лигу,  и  команды
                    string gameGroup = nodes[i].ChildNodes[0].ChildNodes[1].ChildNodes[1].InnerText.Trim();
                    var teams = nodes[i].ChildNodes[j].ChildNodes[2].ChildNodes[1].InnerText.Trim().Replace(" - ", "|").Split('|');
                    
   
                    Bet bet = betsStore.Pop();

                    bet.URL = nodes[i].ChildNodes[j].Id.Trim().Split('-').Last();
                    var gameId = Int32.Parse(bet.URL);

                    var linkData = lives.FirstOrDefault(x => x.Id == bet.URL);
                    if (linkData == null)
                    {
                        Logger.AddLog($"Ахтунг! Этой игры тут не должно быть! {gameId}",Logger.LogTarget.Zenit,Logger.LogLevel.Warn);
                        continue;
                    }

                    bet.Site = "Z";
                    bet.Bookmaker = BookmakerType.Zenit;
                    bet.SportType = SportTypeHelper.Parse(gameGroup);
                    bet.Groupe = gameGroup.Split('.')[1].Trim();
                    bet.Team1 = teams[0];
                    bet.Team2 = teams[1];
                    bet.Name = bet.Team1 + " - " + bet.Team2;
                    bet.Team1Id = linkData.Team1Id;
                    bet.Team2Id = linkData.Team2Id;

                    //Если это статика то пропускаем
                    if (bet.Name.ToUpper().Contains("ЖК") || bet.Name.ToUpper().Contains("УГЛ"))
                        continue;

                   
                    
                    bet.Time = nodes[i].ChildNodes[j].ChildNodes[1].InnerText.Trim();

     
                    bet = NewParseTable(nodes[i].ChildNodes[j], bet);
                    

                    string timeData = nodes[i].ChildNodes[j].ChildNodes[2].ChildNodes[2].InnerText;
                    ParseAndSetTime(bet, timeData);

                    
                                       
                    //Если есть доп.данные со ставками
                    if (zenitModel.Result.Html.ContainsKey(gameId))
                    { 
                        ParseAdditonData(bet, zenitModel,gameId, timeData);                       
                    }
                    int deltaTime = (int) (DateTime.Now - bet.Created).TotalMilliseconds;
                    if (deltaTime < MainConfigurate.Configurate.ZenitMaxTime)
                        Bets.Push(bet);
                }
            }

            sw.Stop();

            Logger.AddLog("Zenit parse time: " + sw.Elapsed + "мс. Ставок: " + Bets.Count, Logger.LogTarget.Zenit);
            return Bets.ToList();
        }

        //Заполняет Ставку данными из таблицы
        private Bet NewParseTable(HtmlNode node,Bet bet)
        {
            HtmlNode header = node.PreviousSibling;

            // делаем -2, т.к. послежний столбец роспись
            for (int i = header.ChildNodes.Count-2; i > 0; i = i - 1)
            {
                if(header.ChildNodes[i].InnerText=="Бол")
                {
                    bet._Tmax = CheckAndSet(node.ChildNodes[i]);
                    if (bet._Tmax != 0)
                    {
                        bet._Tmaxo = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }
                else if(header.ChildNodes[i].InnerText == "Тот")
                {
                    if (node.ChildNodes[i].ChildNodes.Count != 0)
                    {
                        bet._Total_Cof = SetValue(node.ChildNodes[i].ChildNodes[0].InnerText);
                    }
                }
                else if (header.ChildNodes[i].InnerText == "Мен")
                {
                    bet._Tmin = CheckAndSet(node.ChildNodes[i]);
                    if (bet._Tmin != 0)
                    {
                        bet._Tmino = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }

                //Сами ставки на фору
                else if (header.ChildNodes[i].InnerText == "К1"
                    || header.ChildNodes[i].InnerText == "Кф1")
                {
                    bet._F1 = CheckAndSet(node.ChildNodes[i]);
                    if (bet._F1 != 0)
                    {
                        bet._F1o = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }
                else if (header.ChildNodes[i].InnerText == "К2"
                    || header.ChildNodes[i].InnerText == "Кф2")
                {
                    bet._F2 = CheckAndSet(node.ChildNodes[i]);
                    if (bet._F2 != 0)
                    {
                        bet._F2o = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }

                //значения форы
                else if (header.ChildNodes[i].InnerText == "Ф1")
                {
                    if (node.ChildNodes[i].ChildNodes.Count != 0)
                    {
                        bet._F1_Cof = SetValue(node.ChildNodes[i].ChildNodes.First().InnerText);
                    }
                }

                else if (header.ChildNodes[i].InnerText == "Ф2")
                {
                    if(node.ChildNodes[i].ChildNodes.Count != 0)
                    {
                        bet._F2_Cof = SetValue(node.ChildNodes[i].ChildNodes.First().InnerText);
                    }
                }                
                
                //ставки на исход
                else if (header.ChildNodes[i].InnerText == "Х2")
                {
                    bet._X2 = CheckAndSet(node.ChildNodes[i]);
                    if (bet._X2 != 0)
                    {
                        bet._X2o = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }               
                else if (header.ChildNodes[i].InnerText == "12")
                {
                    bet._12 = CheckAndSet(node.ChildNodes[i]);
                    if (bet._12 != 0)
                    {
                        bet._12o = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }
                else if (header.ChildNodes[i].InnerText == "1Х")
                {
                    bet._1X = CheckAndSet(node.ChildNodes[i]);
                    if (bet._1X != 0)
                    {
                        bet._1Xo = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }

                else if (header.ChildNodes[i].InnerText == "П2")
                {
                    bet._2 = CheckAndSet(node.ChildNodes[i]);
                    if (bet._2 != 0)
                    {
                        bet._2o = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }
                else if (header.ChildNodes[i].InnerText == "Х")
                {
                    bet._X = CheckAndSet(node.ChildNodes[i]);
                    if (bet._X != 0)
                    {
                        bet._Xo = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }
                else if (header.ChildNodes[i].InnerText == "П1")
                {
                    bet._1 = CheckAndSet(node.ChildNodes[i]);
                    if(bet._1!=0)
                    {
                        bet._1o = node.ChildNodes[i].FirstChild.Attributes["href"].Value.Trim();
                    }
                }

                else if(header.ChildNodes[i].InnerText == "Событие")
                {
                    break;
                }

                else
                {

                }
            }

            return bet;
        }


        //Парсим остальные ставки
        private void ParseAdditonData(Bet bet,ZenitModel model,int gameId ,string timeData)
        {
            var htmlData = model.Result.Html[gameId];
            if(String.IsNullOrWhiteSpace(htmlData))
                return;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlData);
            HtmlNode node = doc.DocumentNode;

            //получаем список всех событий
            HtmlNodeCollection dt = node.ChildNodes[0].ChildNodes[0].ChildNodes.First().ChildNodes;

            foreach(HtmlNode key in dt)
            {                
                if (key.Name == "table")
                {
                    ParseAdditionBet(key, bet);
                }  
                else if (key.InnerText.Contains("-м гейме  выиграет:"))
                {
                    ParseTennisGame(bet, timeData, key);
                }
            }

            if(model.Result?.bets==null)
                return;

            var bets = model.Result.bets.Where(x => x.Value.GameId == gameId).Select(x=>x.Value).ToList();



            foreach (ZenitBet keyBet in bets)
            {
                Stake st = new Stake();
                st.Coef = keyBet.cf;
                if(String.IsNullOrWhiteSpace(keyBet.d1))
                    continue;
                if(keyBet.Status!=1)
                    continue;
                st.Parametr = Convert.ToSingle(keyBet.d1.Replace(".",","));
                st.ParametrO = "#" + keyBet.Odd;

                if (keyBet.Odd == 300
                    || keyBet.Odd == 331
                    || keyBet.Odd == 332
                    || keyBet.Odd == 333
                    || keyBet.Odd == 337
                    || keyBet.Odd == 338
                    || keyBet.Odd == 400
                    || keyBet.Odd == 401
                    || keyBet.Odd == 402
                    || keyBet.Odd == 403
                    || keyBet.Odd == 4040)
                {
                    st.StakeType = StakeType.Fora1;
                    bet.Foras.Add(st);
                }
                else if (keyBet.Odd == 301
                    || keyBet.Odd == 334
                    || keyBet.Odd == 335
                    || keyBet.Odd == 336
                    || keyBet.Odd == 339
                    || keyBet.Odd == 348
                    || keyBet.Odd == 4044)
                {
                    st.StakeType = StakeType.Fora2;
                    bet.Foras.Add(st);

                }
                else if (keyBet.Odd == 310
                    || keyBet.Odd == 312
                    || keyBet.Odd == 314
                    || keyBet.Odd == 316
                    || keyBet.Odd == 418
                    || keyBet.Odd == 340
                    || keyBet.Odd == 341
                    || keyBet.Odd == 342)
                {
                    st.StakeType= StakeType.Tmin;
                    bet.Totals.Add(st);
                }
                else if (keyBet.Odd == 311
                    || keyBet.Odd == 313
                    || keyBet.Odd == 315
                    || keyBet.Odd == 317
                    || keyBet.Odd == 319
                    || keyBet.Odd == 344
                    || keyBet.Odd == 345
                    || keyBet.Odd == 346
                    || keyBet.Odd == 347)
                {
                    st.StakeType = StakeType.Tmax;
                    bet.Totals.Add(st);
                }

                else if (keyBet.Odd == 87
                    || keyBet.Odd == 30087
                    || keyBet.Odd == 4112
                    || keyBet.Odd == 4113
                    || keyBet.Odd == 4114
                    || keyBet.Odd == 4115)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team=ETeam.Team1;
                    bet.ITotals.Add(st);
                }
                else if (keyBet.Odd == 88
                    || keyBet.Odd == 30088
                    || keyBet.Odd == 4116
                    || keyBet.Odd == 4117
                    || keyBet.Odd == 4118
                    || keyBet.Odd == 4119)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team1;
                    bet.ITotals.Add(st);
                }

                else if (keyBet.Odd == 89
                    || keyBet.Odd == 30089
                    || keyBet.Odd == 4120
                    || keyBet.Odd == 4121
                    || keyBet.Odd == 4122
                    || keyBet.Odd == 4123)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team2;
                    bet.ITotals.Add(st);
                }
                else if (keyBet.Odd == 90
                    || keyBet.Odd == 30090
                    || keyBet.Odd == 4124
                    || keyBet.Odd == 4125
                    || keyBet.Odd == 4126
                    || keyBet.Odd == 4127)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team2;
                    bet.ITotals.Add(st);
                }
   
                else if (keyBet.Odd == 2631
                    || keyBet.Odd == 2632
                    || keyBet.Odd == 2633)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet,st,SportTimePart.Time1);
                }
                else if (keyBet.Odd == 2634
                    || keyBet.Odd == 2635
                    || keyBet.Odd == 2636)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time1);
                }

                else if (keyBet.Odd == 2637
                    || keyBet.Odd == 2638
                    || keyBet.Odd == 2639)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time1);
                }
                else if (keyBet.Odd == 2640
                    || keyBet.Odd == 2641
                    || keyBet.Odd == 2642)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time1);
                }

                else if (keyBet.Odd == 7924)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }
                else if (keyBet.Odd == 7925)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }

                else if (keyBet.Odd == 7926)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }
                else if (keyBet.Odd == 7927)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }

                else if (keyBet.Odd == 4558
                    || keyBet.Odd == 7944)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }
                else if (keyBet.Odd == 4559
                    || keyBet.Odd == 7945)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }

                else if (keyBet.Odd == 4560
                    || keyBet.Odd == 7946)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }
                else if (keyBet.Odd == 4561
                    || keyBet.Odd == 7947)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }
                

                else if (keyBet.Odd == 4562)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }
                else if (keyBet.Odd == 4563)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }

                else if (keyBet.Odd == 4564)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }
                else if (keyBet.Odd == 4565)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }

                else if (keyBet.Odd == 4566)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half1);
                }
                else if (keyBet.Odd == 4567)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half1);
                }

                else if (keyBet.Odd == 4568)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half1);
                }
                else if (keyBet.Odd == 4569)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half1);
                }

                else if (keyBet.Odd == 4570)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half2);
                }
                else if (keyBet.Odd == 4571)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half2);
                }

                else if (keyBet.Odd == 4572)
                {
                    st.StakeType = StakeType.ITmin;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half2);
                }
                else if (keyBet.Odd == 4573)
                {
                    st.StakeType = StakeType.ITmax;
                    st.Team = ETeam.Team2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Half2);
                }

                else if (keyBet.Odd == 1860
                    || keyBet.Odd == 4400
                    || keyBet.Odd == 3201
                    || keyBet.Odd == 3202
                    || keyBet.Odd == 4350
                    || keyBet.Odd == 4351
                    || keyBet.Odd == 4352)
                {
                    st.StakeType = StakeType.Fora1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time1);
                }
                else if (keyBet.Odd == 1861
                    || keyBet.Odd == 4401
                    || keyBet.Odd == 3204
                    || keyBet.Odd == 3205
                    || keyBet.Odd == 4353
                    || keyBet.Odd == 4354
                    || keyBet.Odd == 4355)
                {
                    st.StakeType = StakeType.Fora2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time1);
                }
                else if (keyBet.Odd == 922
                    || keyBet.Odd == 1862
                    || keyBet.Odd == 4402
                    || keyBet.Odd == 4414
                    || keyBet.Odd == 4422
                    || keyBet.Odd == 4432
                    || keyBet.Odd == 4442
                    || keyBet.Odd == 7920
                    || keyBet.Odd == 4356)
                {
                    st.StakeType = StakeType.Fora1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }
                else if (keyBet.Odd == 923
                    || keyBet.Odd == 1863
                    || keyBet.Odd == 4403
                    || keyBet.Odd == 4413
                    || keyBet.Odd == 4423
                    || keyBet.Odd == 4433
                    || keyBet.Odd == 4443
                    || keyBet.Odd == 7921
                    || keyBet.Odd == 4359)
                {
                    st.StakeType = StakeType.Fora2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }
                
                else if (keyBet.Odd == 4362
                    || keyBet.Odd == 4363
                    || keyBet.Odd == 4364)
                {
                    st.StakeType = StakeType.Fora1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }
                else if (keyBet.Odd == 4365
                    || keyBet.Odd == 4366
                    || keyBet.Odd == 4367)
                {
                    st.StakeType = StakeType.Fora2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }
                
                else if (keyBet.Odd == 4368
                    || keyBet.Odd == 4369
                    || keyBet.Odd == 4370)
                {
                    st.StakeType = StakeType.Fora1;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }
                else if (keyBet.Odd == 4371
                    || keyBet.Odd == 4372
                    || keyBet.Odd == 4373)
                {
                    st.StakeType = StakeType.Fora2;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }
                else if (keyBet.Odd == 670
                    || keyBet.Odd == 674
                    || keyBet.Odd == 685
                    || keyBet.Odd == 2958
                    || keyBet.Odd == 1930
                    || keyBet.Odd == 1931
                    || keyBet.Odd == 4131
                    || keyBet.Odd == 4132
                    || keyBet.Odd == 4153)
                {
                    st.StakeType = StakeType.Tmin;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time1);
                }
                else if (keyBet.Odd == 671
                    || keyBet.Odd == 686
                    || keyBet.Odd == 687
                    || keyBet.Odd == 2960
                    || keyBet.Odd == 1936
                    || keyBet.Odd == 1937
                    || keyBet.Odd == 4133
                    || keyBet.Odd == 4134
                    || keyBet.Odd == 4156)
                {
                    st.StakeType = StakeType.Tmax;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time1);
                }
                
                else if (keyBet.Odd == 940
                    || keyBet.Odd == 1942
                    || keyBet.Odd == 1943
                    || keyBet.Odd == 1944
                    || keyBet.Odd == 1945)
                {
                    st.StakeType = StakeType.Tmin;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }
                else if (keyBet.Odd == 942
                    || keyBet.Odd == 1948
                    || keyBet.Odd == 1949
                    || keyBet.Odd == 1950
                    || keyBet.Odd == 1951)
                {
                    st.StakeType = StakeType.Tmax;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time2);
                }
                else if (keyBet.Odd == 1955
                    || keyBet.Odd == 1956
                    || keyBet.Odd == 1957
                    || keyBet.Odd == 4139
                    || keyBet.Odd == 4140
                    || keyBet.Odd == 4163)
                {
                    st.StakeType = StakeType.Tmin;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }
                else if (keyBet.Odd == 1961
                    || keyBet.Odd == 1962
                    || keyBet.Odd == 1963
                    || keyBet.Odd == 4141
                    || keyBet.Odd == 4142
                    || keyBet.Odd == 4166)
                {
                    st.StakeType = StakeType.Tmax;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time3);
                }


                else if (keyBet.Odd == 4143
                    || keyBet.Odd == 4144
                    || keyBet.Odd == 4169)
                {
                    st.StakeType = StakeType.Tmin;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }
                else if (keyBet.Odd == 4145
                    || keyBet.Odd == 4146
                    || keyBet.Odd == 4172)
                {
                    st.StakeType = StakeType.Tmax;
                    MarafonParserHelper.AddStake(bet, st, SportTimePart.Time4);
                }
                else
                {
                    
                }

                /* ИТ Хокей С мячом:
                 *      87,30087,4112,4113,4114,4115 - ИТм1
                 *      88,30088,4116,4117,4118,4119 - ИТб1
                 *      89,30089,4120,4121,4122,4123 - ИТм2
                 *      90,30090,4124,4125,4126,4127 - ИТб2
                 *      
                 *      2631,2632,2633 - ИТм1 - 1тайм
                 *      2634,2635,2636 - ИТб1 - 1тайм  
                 *      2637,2638,2639 - ИТм2 - 1тайм
                 *      2640,2641,2642 - ИТб2 - 1тайм
                 *      
                 *      7924    ИТм1    2й период
                 *      7925    ИТб1    2й Переод
                 *      7926    ИТм2    2й переод
                 *      7927    ИТб2    2й переод
                 *      
                 *      4558,7944    ИТм1    3я четверть
                 *      4559,7945    ИТб2    3я четверть      
                 *      4560,7946    ИТм2    3я четверть
                 *      4561,7947    ИТб2    3я четверть
                 *      
                 *      4562    ИТм1    4я четверть
                 *      4563    ИТб1    4я четверть
                 *      4564    ИТм2    4я четверть
                 *      4565    ИТб2    4я четверть
                 *      
                 *      4566    ИТм1    1половина
                 *      4567    Итб1    1половина
                 *      4568    Итм2    1половина
                 *      4569    Итб2    1половина
                 *      
                 *      4570    ИТм1    2я половина
                 *      4571    ИТб1    2я половина
                 *      4572    ИТм2    2я половина
                 *      4573    ИТб2    2я половина
                 *     
                 *     -------Тоталы----------
                 *  Тотал матча:
                 *      310,312,314,316,318,340,341,342    Тм
                 *      311,313,315,317,319,344,345,346,347    Тб   
                 *      
                 *  Доп. Тотал 1го сета:
                 *      670,674,685,2958,1930,1931,4131,4132,4153    Тм
                 *      671,686,687,2960,1936,1937,4133,4134,4156    Тб
                 *      
                 *      
                 *  Доп. Тотал 2го сета:
                 *      940,1942,1943,1944,1945    Тм
                 *      942,1948,1949,1950,1951    Тб
                 *      
                 *      
                 *  Доп. Тотал 3го сета:
                 *      1955,1956,1957,4139,4140,4163    Тм
                 *      1961,1962-1963,4141,4142,4166    Тб
                 *  
                 *  Доп. Тотал 4го сета:
                 *      4143,4144,4169      Тм
                 *      4145,4146,4172      Тб
                 *      
                 *      -------Форы---------
                 *  Фора матча:
                 *      300,331,332,333,337,338,4040    Ф1
                 *      301,334,335,336,339,348,4044    Ф2
                 *      
                 *  Доп. Фора 1го сета:
                 *      1860,4400,3201,3202,4350,4351,4352    Ф1
                 *      1861,4401,3204,3205,4353,4354,4355    Ф2
                 *      
                 *  Доп. Форы 2го сета:  
                 *      922,1862,4402,4414,4422,4432,4442,7920,4356    Ф1
                 *      923,1863,4403,4413,4423,4433,4443,7921,4359    Ф2
                 *      
                 *  Доп форы 3го сета:
                 *      4362,4363,4364    Ф1
                 *      4365,4366,4367    Ф2
                 *      
                 *  Доп форы 4го сета:  
                 *      4368,4369,4370    Ф1
                 *      4371,4372,4373    Ф2
                 *      
                 *          По геймам:
                 *      6201-6202   1
                 *      6203-6204   2
                 *      6205-6206   3
                 *      6207-6208   4
                 *      6209-6210   5
                 *      6211-6212   6
                 *      6213-6214   7
                 *      6215-6216   8
                 *      6217-6218   9
                 *      6219-6220   10
                 *      6221-6222   11
                 *      6223-6224   12
                 *      6225-6226   13
                 *      6227-6228   14
                 *      6229-6230   15
                 *      6231-6232   16
                 *      6233-6234   17
                 *      6235-6236   18
                 *      6237-6238   19
                 *      6239-6240   20
                 *      6241-6242   21
                 *      6243-6244   22
                 *      6245-6246   23
                 * */

            }
        }

        private void ParseTennisGame(Bet bet, string timeData, HtmlNode key)
        {
            //пытаемся вычилисть гейм сета.
            TenisGamePart gameNumber;
            int set;
            int gameCurentNumber;

            { 
                //вытаскиваем гейм по данным зенита
                Regex reg = new Regex(@"\d{1,2}");
                Match match = reg.Match(key.InnerText);
                gameCurentNumber = int.Parse(match.Groups[0].Value);
            }
            {

                Regex reg = new Regex(@"\((.*)\)");
                MatchCollection match = reg.Matches(timeData);
                string str = match[0].Value.Replace("(", "").Replace(")", "").Replace(",", "").Trim();

                //получаем количество сетов
                string[] numbers = str.Split(' ');
                set = numbers.Length;

                for (int i = 0; i < numbers.Length - 1; i++)
                {
                    //проходимся по тем, что точно завершились.
                    //и отнимаем их количество от Данных зенита
                    string[] parsArray = numbers[i].Split(':');
                    gameCurentNumber -= int.Parse(parsArray[0]);
                    gameCurentNumber -= int.Parse(parsArray[1]);
                }

                //это если гейм на следующий сет
                if (gameCurentNumber == 14)
                {                     
                    gameCurentNumber = 1;
                    set++;
                }

     
                gameNumber = (TenisGamePart)Enum.Parse(typeof(TenisGamePart), gameCurentNumber.ToString());
            }

            /* 
             * 
             * потом ставки у нас идут по 4 элемента в key.ChildNodes
             * b text (span или a) text
             * 
             * одна ставка это 4*2 - т.к. две команды 
             **/
            for (int i = 4; i < key.ChildNodes.Count; i = i + 8)
            {
                if (key.ChildNodes[i].InnerText.Contains("будет ровно"))
                    break;

                GameBet gameBet = new GameBet {Team1 = key.ChildNodes[i].InnerText};
                //парсим ставку на 1го игрока
                if (key.ChildNodes[i + 2].Name == "a")
                {
                    gameBet.Coef1 = SetValue(key.ChildNodes[i + 2].InnerText);
                    gameBet.Coef1o = key.ChildNodes[i + 2].Attributes["href"].Value;
                }

                //парсим ставку на 2го игрока
                gameBet.Team2 = key.ChildNodes[i + 4].InnerText;
                if (key.ChildNodes[i + 6].Name == "a")
                {
                    gameBet.Coef2 = SetValue(key.ChildNodes[i + 6].InnerText);
                    gameBet.Coef2o = key.ChildNodes[i + 6].Attributes["href"].Value;
                }
                gameBet.GameNumber = gameNumber;
                gameBet.Set = SportTimePartHelper.Parse(set.ToString());

                bet.Games.Add(gameBet);
            }
        }

        
        private void ParseITot(Bet bet, HtmlNode key)
        {
            string teamName = new string(key.ChildNodes.Where(x => x.Name == "b").SelectMany(x => x.InnerText).ToArray()).Replace("Индивидуальные тоталы", "").Replace(":", "").Trim();
            
            Queue<IBTBet> buffer = new Queue<IBTBet>();

            /*
             * что бы вычленить данные нужно идти с конца. т.к. начало бывает плавающее.
             * В зависимости от названия команды
             * 
             * ставка состоит из 4х элементов.
             * 1)span 2)text 3)(span или a) 4)text
             * в 1 - коэф
             * в 3 -сама ставка
             * в 4 - ;  или же {больше (} или {меньше (}
             * 
             * У некоторых может быть сразу много Ит на одну команду. И сначала идут через ; все Тмин а потом Тмах
             * Для этого заводим флаг, что бы вовремя отследить
             *  
             * */
            bool big = true;
            for (int i = key.ChildNodes.Count - 1; i > 4; i = i - 4)
            {
                if (key.ChildNodes[i].InnerText.Contains("больше ("))
                    big = false;
                IBTBet ibt;
                if (big)
                {
                    ibt = new IBTBet();
                    ibt.TeamName = teamName;

                    if (key.ChildNodes[i - 1].Name != "span")
                        ibt.Tmax = SetValue(key.ChildNodes[i - 1].InnerText);
                    if (ibt.Tmax != 0)
                        ibt.Tmaxo = key.ChildNodes[i - 1].Attributes["href"].Value;
                    ibt.Coeff = SetValue(key.ChildNodes[i - 3].InnerText);
                    buffer.Enqueue(ibt);
                }
                else
                {
                    ibt = buffer.Dequeue();
                    if (key.ChildNodes[i - 1].Name != "span")
                        ibt.Tmin = SetValue(key.ChildNodes[i - 1].InnerText);
                    if (ibt.Tmin != 0)
                        ibt.Tmino = key.ChildNodes[i - 1].Attributes["href"].Value;
                    var coeff = SetValue(key.ChildNodes[i - 3].InnerText);
                    if (coeff != ibt.Coeff)
                    {
                        //throw new ArgumentException("Ошибка парсинга ИТ");
                    }

                    if (ibt.Isvalid())
                        bet.IBTList.Add(ibt);
                }

            }
        }
        
        //Парсит доп росписи по таймам/матчам/сетам и прочему
        private void ParseAdditionBet(HtmlNode nodeTable,Bet betMain)
        {            

            for(int i=1;i<nodeTable.ChildNodes.Count;i++)
            {
                Bet bet = betMain.ShortCopy();
                var node = nodeTable.ChildNodes[i];   
                 
                for(int j=1;j<node.ChildNodes.Count;j++)
                {
                    //текущая столбец 
                    var currentNode = node.ChildNodes[j];
                    if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("П1")) //проверяем заголовок таблицы
                    {                       
                        bet._1 = CheckAndSet(currentNode);
                        if(bet._1!=0)
                            bet._1o = currentNode.FirstChild.Attributes["href"].Value;             
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("П2"))
                    {
                        bet._2 = CheckAndSet(currentNode);
                        if (bet._2 != 0)
                            bet._2o = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("X"))
                    {
                        bet._X = CheckAndSet(currentNode);
                        if (bet._X != 0)
                            bet._Xo = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("1Х"))
                    {
                        bet._1X = CheckAndSet(currentNode);
                        if (bet._1X != 0)
                            bet._1Xo = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("12"))
                    {
                        bet._12 = CheckAndSet(currentNode);
                        if (bet._12 != 0)
                            bet._12o = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("X2"))
                    {
                        bet._X2 = CheckAndSet(currentNode);
                        if (bet._X2 != 0)
                            bet._X2o = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("Ф1"))
                    {
                        bet._F1_Cof = SetValue(currentNode.InnerText.Trim());   
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("Кф1")
                        || nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("К1"))
                    {
                        bet._F1 = CheckAndSet(currentNode);
                        if (bet._F1 != 0)
                            bet._F1o = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("Ф2"))
                    {
                        bet._F2_Cof = SetValue(currentNode.InnerText.Trim());
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("Кф2")
                        || nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("К2"))
                    {
                        bet._F2 = CheckAndSet(currentNode);
                        if (bet._F2 != 0)
                            bet._F2o = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("Мен"))
                    {
                        bet._Tmin = CheckAndSet(currentNode);
                        if (bet._Tmin != 0)
                            bet._Tmino = currentNode.FirstChild.Attributes["href"].Value;
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("Тот"))
                    {
                        bet._Total_Cof = SetValue(currentNode.InnerText.Trim());
                    }

                    else if (nodeTable.ChildNodes[0].ChildNodes[j].InnerText.Contains("Бол"))
                    {
                        bet._Tmax = CheckAndSet(currentNode);
                        if (bet._Tmax != 0)
                            bet._Tmaxo = currentNode.FirstChild.Attributes["href"].Value;
                    }
                }
                              
                if (bet._F1 != 0)
                {
                    Stake st = new Stake();
                    st.StakeType = StakeType.Fora1;
                    st.Coef = bet._F1;
                    st.Parametr = bet._F1_Cof;
                    st.ParametrO = bet._F1o;
                    bet.Foras.Add(st);
                }
                if (bet._F2 != 0)
                {
                    Stake st = new Stake();
                    st.StakeType = StakeType.Fora2;
                    st.Coef = bet._F2;
                    st.Parametr = bet._F2_Cof;
                    st.ParametrO = bet._F2o;
                    bet.Foras.Add(st);
                }

                if (bet._Tmax != 0)
                {
                    Stake st = new Stake();
                    st.StakeType = StakeType.Tmax;
                    st.Coef = bet._Tmax;
                    st.Parametr = bet._Total_Cof;
                    st.ParametrO = bet._Tmaxo;
                    bet.Totals.Add(st);
                }
                if (bet._Tmin != 0)
                {
                    Stake st = new Stake();
                    st.StakeType = StakeType.Tmin;
                    st.Coef = bet._Tmin;
                    st.Parametr = bet._Total_Cof;
                    st.ParametrO = bet._Tmino;
                    bet.Totals.Add(st);
                }

                var partName = nodeTable.ChildNodes[i].ChildNodes[0].InnerText;

                Regex reg = new Regex("(.-)");
                var match = reg.Match(partName);
                var partData = match.Value.Split('-').First();

                if (partName.Contains("половина"))
                    partData = "1" + partData;
                var part = SportTimePartHelper.Parse(partData);
                betMain.Parts.Add(part, bet);
            }
            
        }

        

        private static void ParseAndSetTime(Bet bet, string timeData)
        {
            if (bet.SportType == SportType.Баскетбол)
            {
                Regex reg = new Regex("\\(.*\\)"); // при перерыве время не показывает
                MatchCollection m = reg.Matches(timeData);
                if (m.Count == 0)
                {
                    bet.Time = timeData.Contains("0:0") ? "0'" : "end";
                }
                else
                    bet.Time = m[0].Value.Replace("(", "").Replace(")", "").Split(',').Count().ToString();
            }
            else if (bet.SportType == SportType.Футбол)
            {
                if (timeData.Contains("П"))
                    bet.Time = "45'";
                else if (timeData.Contains("90+"))
                    bet.Time = "90'";
                else
                {
                    Regex reg = new Regex("(.{4}\')");
                    MatchCollection m = reg.Matches(timeData);
                    if (m.Count == 0 && timeData != "0:0")
                    {
                        Regex reg1 = new Regex("\\(.*\\)");
                        MatchCollection m1 = reg1.Matches(timeData);
                        bet.Time = m1.Count == 0 ? "end" : "0'";
                    }
                    else if (timeData == "0:0")
                        bet.Time = "0'";
                    else
                    {
                        bet.Time = m[0].Value.Split(' ').Last();
                    }
                }
            }
            else if (bet.SportType == SportType.Бадминтон
                     || bet.SportType == SportType.Настольный_теннис
                     || bet.SportType == SportType.Теннис
                     || bet.SportType == SportType.Волейбол)
            {
                Regex reg = new Regex("\\(.*\\)");
                MatchCollection m = reg.Matches(timeData);
                bet.Time = m.Count == 0 ? "1" : m[0].Value.Replace("(", "").Replace(")", "").Split(',').Count().ToString();
            }
            else if (bet.SportType == SportType.Гандбол)
            {
                Regex reg = new Regex("(.{4}\')");
                MatchCollection m = reg.Matches(timeData);
                if (timeData.Contains("П"))
                    bet.Time = "П";

                else if (m.Count == 0)
                {
                    bet.Time = timeData.Contains("0:0") ? "0'" : "end";
                }
                else
                    bet.Time = m[0].Value.Split(' ').Last();
            }
            else if (bet.SportType == SportType.Хоккей)
            {
                Regex reg = new Regex("(.{4}\')"); // при перерыве время не показывает
                MatchCollection m = reg.Matches(timeData);
                if (timeData.Contains("П")) // на перерыве время = тайм*20
                {
                    Regex reg1 = new Regex("\\(.*\\)");
                    MatchCollection m1 = reg1.Matches(timeData);
                    bet.Time = (m1[0].Value.Replace("(", "").Replace(")", "").Split(',').Count() * 20).ToString();
                }
                else if (m.Count == 0)
                {
                    bet.Time = timeData.Contains("0:0") ? "0'" : "end";
                }
                else
                    bet.Time = m[0].Value.Split(' ').Last();
            }
            else
            {
                bet.Time = "-1";
            }
        }

        private ZenitModel GetAdditionData()
        {
            List<string> ids = new List<string>();
            lock (_lockLive)
            {
                ids.AddRange(_livesList.Select(x=>x.Id));
            }

            if (ids.Count <= 0) return new ZenitModel() {Code = "Error"};
            string idsUrl = string.Join("-",ids);

            string dopTables = _req.Get($"{_url}ajax/live/load_ross/0?gid={idsUrl}&onlyview=0").ToString();
            var zm = JsonConvert.DeserializeObject<ZenitModel>(dopTables);
                

            if (zm.Code != "OK")
                throw new ArgumentException("Не удалось получить всю роспись.");

            if (zm .Result.Html== null)
                return new ZenitModel() { Code = "Error" };

            return zm;
        }

        //Получает список Id event`s
        private List<LinkData> GetLives()
        {
            var req=new HttpRequest();
            req.CharacterSet= Encoding.UTF8;
            

            string respone = req.Get(_url + "live").ToString();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);

            HtmlNodeCollection nodes=doc.DocumentNode.SelectNodes("//tr[@class=\"live-index-game\"]");
            if (nodes == null)
                throw new ArgumentException("Неудалось получить ID event`ов");

            if (_bd == null)
            {
                _bd = new UnicDataDecorator(UnicData.Load("bd.data"));
                UnicDataDecorator.UpdateBase += UnicDataDecorator_UpdateBase;
            }
            List<LinkData> rezult = new List<LinkData>();

            foreach(HtmlNode key in nodes)
            {
                var node = key;
                var linkData = new LinkData();
                linkData.Team1Id = _bd.GetNumber(node.ChildNodes[3].InnerText.Trim());
                if(linkData.Team1Id==-1)
                    continue;
                linkData.Team2Id = _bd.GetNumber(node.ChildNodes[7].InnerText.Trim());
                if(linkData.Team2Id==-1)
                    continue;

                linkData.Id = node.ChildNodes[1].ChildNodes[1].Attributes["data-gid"].Value.Trim();
                linkData.Teams = node.ChildNodes[3].InnerText.Trim() + " - " + node.ChildNodes[7].InnerText.Trim();

                rezult.Add(linkData);
            }
            var delta = nodes.Count - rezult.Count;
            Logger.AddLog($"Разница в ставках: {delta} шт. Всего {rezult.Count} шт. А было {nodes.Count}",Logger.LogTarget.Zenit);
            return rezult;
        }

        private void UnicDataDecorator_UpdateBase(List<UnicData> obj)
        {
            _bd.SetBase(obj);
        }


        private Thread _thUpdate;

        private List<LinkData> _livesList = new List<LinkData>();

        private readonly object _lockLive = new object();

        private UnicDataDecorator _bd;


        private void Update()
        {
            while (true)
            {
                try
                {
                    var rez = GetLives();
                    lock (_lockLive)
                    {
                        _livesList = rez;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"zenit: не удалось получить список Live. {e.Message}");
                    Thread.Sleep(5000);
                    continue;
                }
                Thread.Sleep(30000);
            }
        }


        private static float SetValue(string value)
        {
            float rezult;
            float.TryParse(value.Replace(".", ","), out rezult);
            return rezult;
        }

        private float CheckAndSet(HtmlNode nod)
        {
            if (nod.ChildNodes.Count == 0)
                return 0;
            if (nod.ChildNodes.First().Name != "a")
                return 0;
            return SetValue(nod.ChildNodes.First().InnerText);
        }
        
        
    }


}
