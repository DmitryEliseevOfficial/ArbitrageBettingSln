using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using xNet;
using ABShared;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using ABShared.Enum;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using StaticData.Shared.Model;

namespace ABServer.Parsers
{
    internal class Olimp : IParse
    {
        private static string _url = "https://olimp.com/";
        private HttpRequest _req;


        private Thread _thUpdate;


        private List<LinkData> _currentGames = new List<LinkData>();


        private readonly object _lockObj = new object();


        public ConcurrentStack<Bet> Bets { get; set; } = new ConcurrentStack<Bet>();

        public bool UsingProxy { get; set; }
        public List<string> ProxyList { get; set; }
        public BookmakerType Bookmaker { get; } = BookmakerType.Olimp;

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
            Stopwatch sw = new Stopwatch();
            sw.Start();

            InitReq();

            if (_thUpdate == null)
            {
                _thUpdate = new Thread(Update)
                {
                    IsBackground = true
                };
                _thUpdate.Start();
            }

            List<Bet> rezult = new List<Bet>();
            foreach (KeyValuePair<string, Bet> key in _bets)
            {
                if (key.Value == null)
                    continue;
                var deltaTime = DateTime.Now - key.Value.Created;
                if (deltaTime.TotalMilliseconds < MainConfigurate.Configurate.OlimpMaxTime)
                    rezult.Add(key.Value);
            }
            sw.Stop();
            Logger.AddLog(
                "На парсинг времени затраченно: " + sw.ElapsedMilliseconds + ". Собранно вилок: " + rezult.Count,
                Logger.LogTarget.Olimp);
            Thread.Sleep(1000);

            return rezult;
        }

        private void InitReq()
        {
            if (_req == null)
            {
                //if (UsingProxy)
                //{
                //    _req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
                //    _req.Proxy.ConnectTimeout = 3000;
                //    _req.Proxy.ReadWriteTimeout = 3000;
                //}
                _req = new HttpRequest();
                _req.KeepAlive = false;
                _req.CharacterSet = Encoding.UTF8;
                _req.UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36 OPR/41.0.2353.46";
            }
        }



        private Bet GetRezult(LinkData linkData, HttpRequest request)
        {
            Bet bet = new Bet();


#if !DEBUG
            try
            {
#endif

                string url = _url + "index.php?page=line&action=2&live[]=" + linkData.Id;
                request.AddHeader("X-Requested-With", "XMLHttpRequest");

                //делаем запрос и парсим
                string respone = request.Get(url).ToString();

                var teams = linkData.Teams.Replace(" - ", "|").Split('|');
                if (teams.Length < 2)
                    throw new ArgumentException($"{nameof(teams)}.Count!=2");
                bet.Team1 = teams[0];
                bet.Team2 = teams[1];
                bet.Team1Id = linkData.Team1Id;
                bet.Team2Id = linkData.Team2Id;
                bet.Name = teams[0] + " - " + teams[1];


                ConverthtmlToBet(respone, bet);



#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
#endif
            return bet;
        }

        internal static Bet ConverthtmlToBet(string respone, Bet bet)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);


            /*
             * Достаем основную таблицу
             * в ней лежат элементы
             * <tr class="m_c"> - хранит названи группы
             * <tr>..<tr> - содержит таблицу с играми иставками
            */
            var table = doc.DocumentNode.SelectSingleNode("//table[@class=\"smallwnd2 totalizator\"]");
            if (table == null)
                throw new ArgumentException("Не удалось получить таблицу");

            //Создаем результат


            //в таблице матчи по группам. каждай группа, чере 3 элемента
            for (int i = 0; i < table.ChildNodes.Count - 1; i = i + 3)
            {
                /* tr #text tr*/
                var dr = table.ChildNodes[i];


                //получаем название группы с групировки
                string category = dr.ChildNodes[1].InnerText.Trim();

                /*
                 * получаем члены table в последний tr в которой игры      
                 * tr #text tr                 * 
                 * */

                //data - 1 событие в группе
                var data = dr.NextSibling.NextSibling.ChildNodes[0].ChildNodes[2].ChildNodes;

                for (int j = 0; j < data.Count - 1; j = j + 3)
                {
                    var gameHtml = new HtmlDocument();
                    gameHtml.LoadHtml(data[j].InnerHtml);

                    var game = gameHtml.DocumentNode;


                    bet.Groupe = category.Split('.')[1].Trim();

                    var fav = game.SelectNodes("//a[@class=\"fav\"]");

                    bet.URL = "index.php?page=line&action=2&live[]=" + fav[0].Attributes["data-favid"].Value;

                    bet.Site = "O";
                    bet.Bookmaker = BookmakerType.Olimp;
                    bet.SportType = SportTypeHelper.Parse(bet.Groupe);

                    /* betFild содержит сначала основную роспись. каждая ставка в <nobr>
                     * А последний div содержит доп.ставки
                     */
                    var betFild = data[j + 2].ChildNodes[0].ChildNodes[0].ChildNodes;


                    //если прием ставок остановлен, то идем к следующей
                    if (betFild.Count == 1)
                        continue;

                    var betFiles = betFild.Where(x => x.Name == "nobr").ToList();

                    //Ставки идут плавающие
                    foreach (var field in betFiles)
                    {
                        #region Получение основных ставок

                        if (field.InnerText.Contains("П1"))
                        {
                            bet._1 = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._1o = field.ChildNodes[1].Id;
                        }

                        else if (field.InnerText.Contains("Х") && !field.InnerText.Contains("1Х") &&
                                 !field.InnerText.Contains("Х2"))
                        {
                            bet._X = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._Xo = field.ChildNodes[1].Id;
                        }
                        else if (field.InnerText.Contains("П2"))
                        {
                            bet._2 = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._2o = field.ChildNodes[1].Id;
                        }
                        else if (field.InnerText.Contains("1Х"))
                        {
                            bet._1X = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._1Xo = field.ChildNodes[1].Id;
                        }
                        else if (field.InnerText.Contains("Х2"))
                        {
                            bet._X2 = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._X2o = field.ChildNodes[1].Id;
                        }
                        else if (field.InnerText.Contains("Ф1"))
                        {
                            bet._F1_Cof = SetValue(field.ChildNodes[1].Attributes["data-v1"].Value.Trim());
                            bet._F1 = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._F1o = field.ChildNodes[1].Id;
                        }
                        else if (field.InnerText.Contains("Ф2"))
                        {
                            bet._F2_Cof = SetValue(field.ChildNodes[1].Attributes["data-v1"].Value.Trim());
                            bet._F2 = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._F2o = field.ChildNodes[1].Id;
                        }

                        else if (field.InnerText.Contains("Тот"))
                        {
                            bet._Total_Cof = SetValue(field.ChildNodes[1].Attributes["data-v1"].Value.Trim());
                            bet._Tmin = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._Tmax = SetValue(field.ChildNodes[3].InnerText.Trim());
                            bet._Tmino = field.ChildNodes[1].Id;
                            bet._Tmaxo = field.ChildNodes[3].Id;
                        }
                        else if (field.InnerText.Contains("12"))
                        // ставим после форы и тоталов. т.к. банально могут совпадать коэф
                        {
                            bet._12 = SetValue(field.ChildNodes[1].InnerText.Trim());
                            bet._12o = field.ChildNodes[1].Id;
                        }

                        #endregion
                    }

                    if (betFiles.Count != 0 && betFild.Last().Name == "div")
                    {
                        var dt = betFild.Last().ChildNodes;


                        for (int iter = 0; iter < dt.Count - 1; iter++)
                        {
                            var key = dt[iter];

                            if (key.InnerText.Contains("Инд.тотал:"))
                            {
                                iter = ParseIBT(bet, iter, dt);
                            }

                            if (key.InnerText.Contains("Ставки по сетам")
                                || key.InnerText.Contains("Ставки по партиям")
                                || key.InnerText.Contains("Исходы по таймам")
                                || key.InnerText.Contains("Исходы по четвертям")
                                || key.InnerText.Contains("Исходы по половинам")
                                || key.InnerText.Contains("Исходы по периодам"))
                            {
                                iter = ParseDop(bet, iter, dt);
                            }

                            if (bet.SportType == SportType.Теннис)
                            {
                                if (key.InnerText.Contains("й гейм:"))
                                {
                                    if (key.ChildNodes.Count == 1)
                                    {
                                        iter = ParseGame(bet, iter, dt);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                    }

                    var allbets = doc.DocumentNode.SelectNodes("//span[@class='bet_sel koefs']");

                    if (allbets != null)
                    {
                        foreach (HtmlNode htmlNode in allbets)
                        {
                            var inBet = new SharingId(htmlNode.Id);
                            if (!inBet.IsValid)
                            {
                                continue;
                            }
                            Stake st = new Stake();
                            st.Parametr = inBet.Coef;
                            st.ParametrO = htmlNode.Id;


                            if (inBet.Id == 4)
                            {
                                if (inBet.Team == 1)
                                    st.StakeType = StakeType.Fora1;
                                else if (inBet.Team == 2)
                                    st.StakeType = StakeType.Fora2;
                                else
                                    throw new ArgumentException(
                                        $"Такой номер команды мы не знаем {nameof(inBet.Team)}");
                                st.Coef = Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                bet.Foras.Add(st);
                            }
                            else if (inBet.Id == 5)
                            {
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.Tmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.Tmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                bet.Totals.Add(st);
                            }


                            else if (inBet.Id == 11)
                            {
                                if (inBet.Team == 1)
                                    st.StakeType = StakeType.Fora1;
                                else if (inBet.Team == 2)
                                    st.StakeType = StakeType.Fora2;
                                else
                                    throw new ArgumentException(
                                        $"Такой номер команды мы не знаем {nameof(inBet.Team)}");
                                st.Coef = Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                if (!bet.Parts.ContainsKey(SportTimePart.Time1))
                                    bet.Parts[SportTimePart.Time1] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time1].Foras.Add(st);
                            }
                            else if (inBet.Id == 12)
                            {
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.Tmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.Tmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time1))
                                    bet.Parts[SportTimePart.Time1] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time1].Totals.Add(st);
                            }

                            else if (inBet.Id == 14)
                            {
                                if (inBet.Team == 1)
                                    st.StakeType = StakeType.Fora1;
                                else if (inBet.Team == 2)
                                    st.StakeType = StakeType.Fora2;
                                else
                                    throw new ArgumentException(
                                        $"Такой номер команды мы не знаем {nameof(inBet.Team)}");
                                st.Coef = Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                if (!bet.Parts.ContainsKey(SportTimePart.Time2))
                                    bet.Parts[SportTimePart.Time2] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time2].Foras.Add(st);
                            }
                            else if (inBet.Id == 15)
                            {
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.Tmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.Tmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time2))
                                    bet.Parts[SportTimePart.Time2] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time2].Totals.Add(st);
                            }

                            else if (inBet.Id == 17)
                            {
                                if (inBet.Team == 1)
                                    st.StakeType = StakeType.Fora1;
                                else if (inBet.Team == 2)
                                    st.StakeType = StakeType.Fora2;
                                else
                                    throw new ArgumentException(
                                        $"Такой номер команды мы не знаем {nameof(inBet.Team)}");
                                st.Coef = Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                if (!bet.Parts.ContainsKey(SportTimePart.Time3))
                                    bet.Parts[SportTimePart.Time3] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time3].Foras.Add(st);
                            }
                            else if (inBet.Id == 18)
                            {
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.Tmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.Tmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time3))
                                    bet.Parts[SportTimePart.Time3] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time3].Totals.Add(st);
                            }


                            else if (inBet.Id == 20)
                            {
                                if (inBet.Team == 1)
                                    st.StakeType = StakeType.Fora1;
                                else if (inBet.Team == 2)
                                    st.StakeType = StakeType.Fora2;
                                else
                                    throw new ArgumentException(
                                        $"Такой номер команды мы не знаем {nameof(inBet.Team)}");
                                st.Coef = Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                if (!bet.Parts.ContainsKey(SportTimePart.Time4))
                                    bet.Parts[SportTimePart.Time4] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time4].Foras.Add(st);
                            }
                            else if (inBet.Id == 21)
                            {
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.Tmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.Tmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time4))
                                    bet.Parts[SportTimePart.Time4] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time4].Totals.Add(st);
                            }

                            else if (inBet.Id == 716)
                            {
                                if (inBet.Team == 1)
                                    st.StakeType = StakeType.Fora1;
                                else if (inBet.Team == 2)
                                    st.StakeType = StakeType.Fora2;
                                else
                                    throw new ArgumentException(
                                        $"Такой номер команды мы не знаем {nameof(inBet.Team)}");
                                st.Coef = Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                if (!bet.Parts.ContainsKey(SportTimePart.Half1))
                                    bet.Parts[SportTimePart.Half1] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Half1].Foras.Add(st);
                            }
                            else if (inBet.Id == 714)
                            {
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.Tmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.Tmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Half1))
                                    bet.Parts[SportTimePart.Half1] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Half1].Totals.Add(st);
                            }


                            else if (inBet.Id == 717)
                            {
                                if (inBet.Team == 1)
                                    st.StakeType = StakeType.Fora1;
                                else if (inBet.Team == 2)
                                    st.StakeType = StakeType.Fora2;
                                else
                                    throw new ArgumentException(
                                        $"Такой номер команды мы не знаем {nameof(inBet.Team)}");
                                st.Coef = Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                if (!bet.Parts.ContainsKey(SportTimePart.Half2))
                                    bet.Parts[SportTimePart.Half2] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Half2].Foras.Add(st);
                            }
                            else if (inBet.Id == 715)
                            {
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.Tmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.Tmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Half2))
                                    bet.Parts[SportTimePart.Half2] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Half2].Totals.Add(st);
                            }


                            else if (inBet.Id == 7)
                            {
                                st.Team = ETeam.Team1;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                bet.ITotals.Add(st);
                            }
                            else if (inBet.Id == 8)
                            {
                                st.Team = ETeam.Team2;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                bet.ITotals.Add(st);
                            }

                            else if (inBet.Id == 43)
                            {
                                st.Team = ETeam.Team1;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time1))
                                    bet.Parts[SportTimePart.Time1] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time1].ITotals.Add(st);
                            }

                            else if (inBet.Id == 44)
                            {
                                st.Team = ETeam.Team2;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time1))
                                    bet.Parts[SportTimePart.Time1] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time1].ITotals.Add(st);
                            }


                            else if (inBet.Id == 45)
                            {
                                st.Team = ETeam.Team1;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time2))
                                    bet.Parts[SportTimePart.Time2] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time2].ITotals.Add(st);
                            }

                            else if (inBet.Id == 46)
                            {
                                st.Team = ETeam.Team2;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time2))
                                    bet.Parts[SportTimePart.Time2] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time2].ITotals.Add(st);
                            }


                            else if (inBet.Id == 47)
                            {
                                st.Team = ETeam.Team1;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time3))
                                    bet.Parts[SportTimePart.Time3] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time3].ITotals.Add(st);
                            }

                            else if (inBet.Id == 48)
                            {
                                st.Team = ETeam.Team2;
                                if (inBet.UnderOver == 2)
                                {
                                    st.StakeType = StakeType.ITmin;
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v2"].Value.Replace(".", ","));
                                }
                                else if (inBet.UnderOver == 3)
                                {
                                    st.Coef =
                                        Convert.ToSingle(htmlNode.Attributes["data-v3"].Value.Replace(".", ","));
                                    st.StakeType = StakeType.ITmax;
                                }
                                else
                                    throw new ArgumentException($"Такой тотал мы не знаем {nameof(inBet.UnderOver)}");

                                if (!bet.Parts.ContainsKey(SportTimePart.Time3))
                                    bet.Parts[SportTimePart.Time3] = bet.ShortCopy();
                                bet.Parts[SportTimePart.Time3].ITotals.Add(st);
                            }
                        }
                    }
                }
            }

            return bet;
        }


        private static int ParseGame(Bet bet, int iter, HtmlNodeCollection data)
        {
            GameBet gmBet = null;
            int commanda = 1;


            for (int i = iter; i < data.Count; i++)
            {
                iter = i;

                var key = data[i];
                if (key.Name == "b")
                {
                    if (gmBet != null)
                        break;


                    gmBet = new GameBet();
                    var dt = key.InnerText.Replace("й", "").Split(' ');
                    gmBet.GameNumber = (TenisGamePart)Enum.Parse(typeof(TenisGamePart), dt[2]);
                    gmBet.Set = SportTimePartHelper.Parse(dt[0]);
                }

                if (key.Name == "nobr")
                {
                    if (commanda == 1)
                    {
                        gmBet.Team1 = key.ChildNodes[0].InnerText.Replace(" -&nbsp;&nbsp;", "");
                        gmBet.Coef1 = SetValue(key.ChildNodes[1].InnerText);
                        gmBet.Coef1o = key.ChildNodes[1].Id;


                        commanda++;
                    }
                    else
                    {
                        gmBet.Team2 = key.ChildNodes[0].InnerText.Replace(" -&nbsp;&nbsp;", "");
                        gmBet.Coef2 = SetValue(key.ChildNodes[1].InnerText);
                        gmBet.Coef2o = key.ChildNodes[1].Id;


                        iter++;
                        break;
                    }
                }
            }

            bet.Games.Add(gmBet);


            return iter;
        }

        private readonly Dictionary<string, Thread> _thsLoad = new Dictionary<string, Thread>();
        private HashSet<string> _raningsEvent = new HashSet<string>();

        internal void Update()
        {
            while (true)
            {
                try
                {
                    var tmp = GetLives();
                    lock (_lockObj)
                    {
                        _currentGames = tmp;
                    }

                    foreach (LinkData data in tmp)
                    {
                        if (!_thsLoad.ContainsKey(data.Id))
                        {
                            var th = new Thread(BetUpdates);
                            th.IsBackground = true;
                            th.Start(data);
                            _thsLoad.Add(data.Id, th);
                        }
                    }
                    var ids = tmp.Select(x => x.Id).ToList();
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
                    {
                        _req.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
                    }
                    Console.WriteLine($"Olimp: не удалось получить список игр");
                    Thread.Sleep(5000);
                    continue;
                }
                finally
                {
                    Thread.Sleep(10000);
                }
            }
        }


        private List<string> GetID()
        {
            //Задаем заголовки
            _req.KeepAlive = true;
            _req.AddHeader("Pragma", "no-cache");
            _req.AddHeader("Cache-Control", "no-cache");
            _req.AddHeader("Upgrade-Insecure-Requests", "1");
            _req.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _req.Referer = _url + "index.php";
            _req.AddHeader("Accept-Encoding", "gzip, deflate, sdch, br");
            _req.AddHeader("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4");

            //Получаем ответ и парсим
            var respone = _req.Get(_url + "betting").ToString();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);

            //Берем таблицу с сылками и начинаем перебирать ее поэлементно
            //В таблице есть часть ссылок не текущие(в начале) и на будующие
            //Так же необходимо пропускать если ставки приостановленны
            //или матч только начался
            var table = doc.DocumentNode.SelectNodes("//table[@class=\"smallwnd3\"]").First();
            var rezult = new List<string>();

            //счетк для пропуска первого заголовка(название сопорта)
            int m_c = 0;

            foreach (var key in table.ChildNodes)
            {
                //отсекаем #text
                if (!key.Name.Contains("tr"))
                    continue;

                //if (key.InnerText.Contains("Приостановлен") || key.InnerText.Contains("Матч не начался"))
                //    continue;

                if (key.Attributes["class"] != null && key.Attributes["class"].Value.Trim() == "m_c")
                {
                    if (m_c++ != 0)
                        break;
                    continue;
                }

                if (key.InnerHtml.Contains("set_lshow"))
                    continue;
                //Есть лишнии записи в таблице. от этого и защищаемся
                try
                {
                    var id = key.ChildNodes[5].ChildNodes[1].Attributes["href"].Value.Split('=').Last();
                    rezult.Add(id);
                }
                catch
                {
                }
            }


            return rezult;
        }

        private static float SetValue(string value)
        {
            float rezult;
            float.TryParse(value.Replace(".", ","), out rezult);
            return rezult;
        }

        static Tuple<SportTimePart, BetNumber> ParseTime(string data)
        {
            BetNumber stavka = BetNumberParse(data);

            SportTimePart part = SportTimePartParse(data);

            return new Tuple<SportTimePart, BetNumber>(part, stavka);
        }

        private static BetNumber BetNumberParse(string data)
        {
            var stavka = BetNumber.Nan;

            if (data.Contains("П1") || data.Contains("Победа первой"))
            {
                stavka = BetNumber._1;
            }

            else if (data.Contains("2X") || data.Contains("Х2"))
            {
                stavka = BetNumber._X2;
            }

            else if (data.Contains("1X") || data.Contains("1Х"))
            {
                stavka = BetNumber._1X;
            }

            else if (data.Contains("Х ") || data.StartsWith("Н "))
            {
                stavka = BetNumber._X;
            }

            else if (data.Contains("П2") || data.Contains("Победа второй"))
            {
                stavka = BetNumber._2;
            }
            else if (data.Contains("12"))
            {
                stavka = BetNumber._12;
            }

            else if (data.Contains("Тотал"))
            {
                if (data.Contains("мен"))
                {
                    stavka = BetNumber._Tmin;
                }
                else if (data.Contains("бол"))
                {
                    stavka = BetNumber._Tmax;
                }
                else
                {
                    throw new ArgumentException($"Невалидное значение {stavka}. Не знаем такого тотала");
                }
            }
            else
            {
                throw new ArgumentException($"Невалидное значение {stavka}. Незнаем такую ставку");
            }
            return stavka;
        }


        private static SportTimePart SportTimePartParse(string data)
        {
            Regex reg = new Regex(".[0-9]-");
            var match = reg.Match(" " + data);
            var dt = match.Value.Replace("-", "");
            if (data.Contains("пол"))
                dt = "1" + dt.Trim();
            var part = (SportTimePart)Enum.Parse(typeof(SportTimePart), dt);
            return part;
        }


        static int ParseIBT(Bet bet, int iter, HtmlNodeCollection data)
        {
            iter++;

            for (int i = iter; i < data.Count; i++)
            {
                iter = i;
                var key = data[i];

                if (key.Name == "nobr")
                {
                    var ibt = new IBTBet();

                    ibt.TeamName = key.ChildNodes[0].InnerText.Replace(" (", "|").Split('|').First();
                    ibt.Coeff = SetValue(key.ChildNodes[1].Attributes["data-v1"].Value);
                    ibt.Tmin = SetValue(key.ChildNodes[1].Attributes["data-v2"].Value);
                    ibt.Tmino = key.ChildNodes[1].Id;
                    if (key.ChildNodes.Count == 5)
                    {
                        ibt.Tmax = SetValue(key.ChildNodes[3].Attributes["data-v3"].Value);
                        ibt.Tmaxo = key.ChildNodes[3].Id;
                    }

                    else if (key.ChildNodes.Count == 9)
                    {
                        ibt.Tmax = SetValue(key.ChildNodes[5].Attributes["data-v3"].Value);
                        ibt.Tmaxo = key.ChildNodes[5].Id;
                    }
                    else
                    {
                    }
                    if (ibt.Isvalid())
                        bet.IBTList.Add(ibt);
                }
                if (key.Name == "b")
                    break;
            }

            return iter;
        }

        static int ParseDop(Bet mainBet, int iter, HtmlNodeCollection data)
        {
            string hockeyTimeData = "";
            if (mainBet.SportType == SportType.Хоккей)
            {
                hockeyTimeData = data[iter].InnerText;
            }
            iter++;

            SportTimePart lastTime = SportTimePart.Nan;
            Bet bet = null;

            for (int i = iter; i < data.Count; i++)
            {
                iter++;
                var key = data[i];
                if (key.Name == "nobr")
                {
                    if (key.InnerText.ToLower().Contains("форой"))
                        continue;
                    BetNumber betNumber = BetNumberParse(key.ChildNodes[0].InnerText);

                    if (betNumber == BetNumber.Nan)
                    {
                        continue;
                    }
                    SportTimePart timePart = SportTimePart.Nan;

                    if (mainBet.SportType != SportType.Хоккей)
                    {
                        timePart = SportTimePartParse(key.ChildNodes[0].InnerText);
                    }
                    else
                    {
                        timePart = (SportTimePart)Enum.Parse(typeof(SportTimePart), hockeyTimeData.Split(' ').First());
                    }
                    //Проверяем не сменился ли у нас тайм
                    if (timePart != lastTime)
                    {
                        if (bet != null)
                            mainBet.Parts.Add(lastTime, bet);
                        lastTime = timePart;
                        bet = mainBet.ShortCopy();
                    }
                    bet[betNumber] = SetValue(key.ChildNodes[1].Attributes["data-v1"].Value.Trim());
                    bet.SetData(betNumber, key.ChildNodes[1].Id);
                    if (betNumber == BetNumber._Tmax || betNumber == BetNumber._Tmin)
                    {
                        bet[BetNumber._Tmin] = SetValue(key.ChildNodes[1].Attributes["data-v2"].Value.Trim());
                        bet.SetData(BetNumber._Tmin, key.ChildNodes[1].Id);
                        bet._Total_Cof = SetValue(key.ChildNodes[1].Attributes["data-v1"].Value.Trim());

                        if (key.ChildNodes.Count == 5)
                        {
                            bet[BetNumber._Tmax] = SetValue(key.ChildNodes[3].Attributes["data-v3"].Value.Trim());
                            bet.SetData(BetNumber._Tmax, key.ChildNodes[3].Id);
                        }
                        else if (key.ChildNodes.Count == 9)
                        {
                            bet[BetNumber._Tmax] = SetValue(key.ChildNodes[5].Attributes["data-v3"].Value.Trim());
                            bet.SetData(BetNumber._Tmax, key.ChildNodes[5].Id);
                        }
                        else
                        {
                        }
                    }
                }

                if (key.Name == "b")
                    break;
            }

            if (bet != null)
                mainBet.Parts.Add(lastTime, bet);

            return iter;
        }

        //Обновленный вариант получения списка Live
        internal List<LinkData> GetLives()
        {
            var datas = new List<LinkData>();
            InitReq();
            var respone = _req.Get(_url + "betting").ToString();
            var parser = new HtmlParser();
            var doc = parser.Parse(respone);
            var link =
                doc.GetElementsByTagName("a")
                    .Where(
                        x =>
                            x.Attributes["href"]?.Value != null &&
                            x.Attributes["href"].Value.Contains("index.php?page=line&action=2&live[]="))
                    .ToList();
            foreach (IElement element in link)
            {
                var parent = element.Parent?.Parent;
                if (parent == null)
                    continue;
                if (parent.TextContent.Contains("Приостановлен")
                    || parent.TextContent.Contains("ЖК ")
                    || parent.TextContent.Contains("УГЛ ")
                    || parent.TextContent.Contains("штанги и перекладины"))
                    continue;
                if (string.IsNullOrWhiteSpace(element.TextContent))
                    continue;

                var id = element.Attributes["href"].Value.Split('=').Last();
                var linkData = new LinkData(id, element.TextContent);
                linkData.TimeData = element.Parent.ChildNodes[element.Parent.ChildNodes.Length - 2].TextContent.Trim();
                datas.Add(linkData);
            }


            if (_bd == null)
            {
                _bd = new UnicDataDecorator(UnicData.Load("bd.data"));
                UnicDataDecorator.UpdateBase += UnicDataDecorator_UpdateBase;
            }


            var dt = new List<LinkData>();
            foreach (LinkData linkData in datas)
            {
                var teams = linkData.Teams.Replace(" - ", "|").Split('|');
                linkData.Team1Id = _bd.GetNumber(teams[0]);
                if (linkData.Team1Id == -1)
                    continue;
                linkData.Team2Id = _bd.GetNumber(teams[1]);
                if (linkData.Team2Id == -1)
                    continue;
                dt.Add(linkData);
            }

            Logger.AddLog($"Всего lives: {datas.Count}. Из них отобранно {dt.Count}", Logger.LogTarget.Olimp);

            return dt;
        }

        private void UnicDataDecorator_UpdateBase(List<UnicData> obj)
        {
            _bd.SetBase(obj);
        }

        //Служит только для теста без прокси
        internal void GetBetTest()
        {
            Bets.Clear();
            List<LinkData> ids = GetLives();
            lock (_lockObj)
            {
                ids.AddRange(_currentGames);
            }

            foreach (LinkData linkData in ids)
            {
                //GetRezult(linkData);
            }
        }

        ConcurrentDictionary<string, Bet> _bets = new ConcurrentDictionary<string, Bet>();
        private UnicDataDecorator _bd;

        internal void BetUpdates(object data)
        {
            LinkData link = data as LinkData;
            if (link == null)
                return;

            Stopwatch sw = new Stopwatch();

            HttpRequest request = new HttpRequest
            {
                CharacterSet = Encoding.UTF8,
                KeepAlive = true
            };
            if (UsingProxy)
            {
                request.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
            }

            while (true)
            {
                sw.Reset();
                sw.Start();
                try
                {
                    var bet = GetRezult(link, request);
                    var time = (int)sw.ElapsedMilliseconds;
                    sw.Stop();
                    _bets[link.Id] = bet;
                    if (time > MainConfigurate.Configurate.OlimpMaxTime)
                        throw new ArgumentException("Долго парсили");

                    //Logger.AddLog($"{link.Id}; Спарсен за {sw.ElapsedMilliseconds}",Logger.LogTarget.OlimpThread);
                }
                catch (Exception e)
                {
                    Logger.AddLog($"{link.Id}; Время {sw.ElapsedMilliseconds};Ошибка парсинга: {e.Message}",
                        Logger.LogTarget.OlimpThread, Logger.LogLevel.Warn);
                }

                if (UsingProxy)
                {
                    request.Proxy = ProxyClient.Parse(ProxySingleManager.GetProxy());
                    request.ReadWriteTimeout = 3000;
                    request.ConnectTimeout = 3000;
                    request.KeepAliveTimeout = 3000;
                    request.KeepAlive = false;
                }
            }
        }
    }

    /*Исходы по таймам:
     * N-м N-го футбик  
     * 
     * Ставки по сетам:
     * 1-м 2-м сете ТЕнис
     * 
     * 1 период: Исходы по периодам: хокейы
     * 
     * Исходы по четвертям:
     * 
     * 
     * (.)й сет (.{0,2})й гейм: - ищет на совпадение для геймов
     * */


    /* 
     * 
     * 4 - Фора матча
     * 5 - Тотал
     * 7 - Ит1
     * 8 - Ит2
     * 43 - Ит1(1го сета)
     * 44 - Ит2(1го сета)
     * 45 - Ит1 2й переод
     * 46 - Ит2 2й переод
     * 47 - Ит1 3й переод
     * 48 - Ит2 3й переод
     * 11 - фора 1го периода
     * 12 - тотал 1го периода
     * 14 - фора 2го периода
     * 15 - тотал 2го периода 
     * 17 - фора 3го периода
     * 18 - тотал 3го периода
     * 20 - фора 4го периода
     * 21 - тотал 4го сета
     * 
     * 714- тотал 1й половины
     * 716- фора 1й половины
     * 715 - тотал 2й половины
     * 717- фора 2й половины
     * 
     * */

    internal class SharingId
    {
        public bool IsValid { get; set; }
        public int Team { get; set; }
        public int Id { get; set; }
        public float Coef { get; set; }
        public int UnderOver { get; set; }

        public SharingId(string data)
        {
            var dt = data.Split(':');
            if (dt.Length < 6)
                return;
            UnderOver = Convert.ToInt32(dt[2]);
            Id = Convert.ToInt32(dt[3]);
            Coef = Convert.ToSingle(dt[4].Replace(".", ","));
            Team = Convert.ToInt32(dt[5]);
            IsValid = true;
        }
    }
}
