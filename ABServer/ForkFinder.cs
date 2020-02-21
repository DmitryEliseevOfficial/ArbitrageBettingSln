using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ABShared;
using ABServer.Parsers;
using System.Threading;
using ABShared.Enum;
using StaticData.Shared.Model;

namespace ABServer
{
    internal class ForkFinder : IDisposable
    {
        private readonly ParserManager _manager;

        private HashSet<Fork> _rezult = new HashSet<Fork>();

        private readonly object _lkRezult = new object();

        private Thread _th;

        private UnicDataDecorator _bd;

        public ForkFinder(ParserManager manager)
        {
            _manager = manager;
        }


        public void Start()
        {
            _th?.Abort();
            _th = new Thread(Parse)
            {
                IsBackground = true
            };
            _th.Start();
        }

        private void Parse()
        {
            while (true)
            {
                _betCheck = 0;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var allbets = _manager.GetAllBet();

                List<Fork> forks = LookOverBets(allbets);


                //Записываем промежуточные результаты в общее поле
                lock (_lkRezult)
                {
                    foreach (Fork fork in forks)
                    {
                        _rezult.Add(fork);
                    }
                }
                sw.Stop();
                Logger.AddLog($"Вилок найденно:{_rezult.Count};", Logger.LogTarget.ForkFinder);
                Logger.AddLog($"Ставок обработанно: {_betCheck}шт", Logger.LogTarget.ForkFinder);
                Logger.AddLog($"FokrFinder: {sw.Elapsed} ms.", Logger.LogTarget.ForkFinder);
                Logger.AddLog($"---------------------------", Logger.LogTarget.ForkFinder);

                if (sw.ElapsedMilliseconds < MainConfigurate.Configurate.ForkFinderTime)
                    Thread.Sleep(MainConfigurate.Configurate.ForkFinderTime - (int)sw.ElapsedMilliseconds);


            }

        }

        internal List<Fork> LookOverBets(List<Bet> allbets)
        {
            int allCount = allbets.Count;
            allbets = allbets.Where(BetIsYoung).ToList();



            var forks = new List<Fork>();

            while (allbets.Count >= 2)
            {
                var st = allbets.First();
                allbets.Remove(st);

                foreach (var bet in allbets)
                {
                    if (bet.SportType == st.SportType)
                    {
                        forks.AddRange(CheckMain(bet, st));
                    }
                }
            }
            return forks;
        }


        private bool BetIsYoung(Bet bet)
        {
            if (bet == null)
                return false;
            var deltaTime = DateTime.Now - bet.Created;
            if (deltaTime.TotalMilliseconds < MainConfigurate.Configurate.EventMaxTime)
                return true;
            //Logger.AddLog($"Старая ставка {bet.Bookmaker} {deltaTime}",Logger.LogTarget.ForkFinberBetBad);
            return false;
        }

        private int _betCheck = 0;

        private List<Fork> CheckMain(Bet one, Bet two)
        {

            List<Fork> rezult = new List<Fork>();
            if (one.Bookmaker == two.Bookmaker)
                return rezult;


            if (_bd == null)
            {
                _bd = new UnicDataDecorator(UnicData.Load("bd.data"));
                UnicDataDecorator.UpdateBase += UnicDataDecorator_UpdateBase;
            }

            //if (one.Team1Id == -2)
            //    one.Team1Id = _bd.GetNumber(one.Team1);
            if (one.Team1Id == -1)
                return rezult;

            //if (one.Team2Id == -2)
            //    one.Team2Id = _bd.GetNumber(one.Team2);
            if (one.Team2Id == -1)
                return rezult;

            //if (two.Team1Id == -2)
            //    two.Team1Id = _bd.GetNumber(two.Team1);
            if (two.Team1Id == -1)
                return rezult;

            //if (two.Team2Id == -2)
            //    two.Team2Id = _bd.GetNumber(two.Team2);
            if (two.Team2Id == -1)
                return rezult;

            if (one.Team1Id != two.Team1Id)
                return rezult;
            if (one.Team2Id != two.Team2Id)
                return rezult;

            _betCheck++;
            rezult.AddRange(Check(one, two));
            foreach (var key in one.Parts)
            {
                if (two.Parts.ContainsKey(key.Key))
                    rezult.AddRange(Check(one.Parts[key.Key], two.Parts[key.Key]));
            }
            return rezult;
        }

        private void UnicDataDecorator_UpdateBase(List<UnicData> obj)
        {
            _bd.SetBase(obj);
        }

        private List<Fork> Check(Bet one, Bet two)
        {
            List<Fork> rezult = new List<Fork>();


            //Новый Ит
            if (one.ITotals.Count != 0 && two.ITotals.Count != 0)
            {
                foreach (Stake oneTotal in one.ITotals)
                {
                    foreach (Stake twoTotal in two.ITotals)
                    {
                        if (oneTotal.Team != twoTotal.Team)
                            continue;
                        if (oneTotal.StakeType == twoTotal.StakeType)
                            continue;
                        if (oneTotal.Parametr == twoTotal.Parametr)
                        {
                            if (CheckFork(oneTotal.Coef, twoTotal.Coef))
                            {
                                rezult.Add(CreateFork(one, two, oneTotal.Coef, twoTotal.Coef, oneTotal.ToString(), twoTotal.ToString(), oneTotal.ParametrO, twoTotal.ParametrO));
                            }
                        }
                        else if (oneTotal.Parametr > twoTotal.Parametr && twoTotal.StakeType == StakeType.ITmax)
                        {
                            if (CheckFork(oneTotal.Coef, twoTotal.Coef))
                            {
                                rezult.Add(CreateFork(one, two, oneTotal.Coef, twoTotal.Coef, oneTotal.ToString(), twoTotal.ToString(), oneTotal.ParametrO, twoTotal.ParametrO));
                            }
                        }

                        else if (oneTotal.Parametr < twoTotal.Parametr && twoTotal.StakeType == StakeType.ITmin)
                        {
                            if (CheckFork(oneTotal.Coef, twoTotal.Coef))
                            {
                                rezult.Add(CreateFork(one, two, oneTotal.Coef, twoTotal.Coef, oneTotal.ToString(), twoTotal.ToString(), oneTotal.ParametrO, twoTotal.ParametrO));
                            }
                        }
                    }
                }
            }


            if (one.Games.Count != 0 && two.Games.Count != 0)
            {
                foreach (var gameOne in one.Games)
                {
                    foreach (var gameTwo in two.Games)
                    {
                        if (gameOne.GameNumber == gameTwo.GameNumber)
                        {
                            if (CompareTeamsName(gameOne.Team1, gameTwo.Team1) && CompareTeamsName(gameOne.Team2, gameTwo.Team2))
                            {
                                if (CheckFork(gameOne.Coef1, gameTwo.Coef2))
                                {
                                    rezult.Add(CreateFork(one, two, gameOne.Coef1, gameTwo.Coef2, gameOne.GameNumber.ToString(), gameTwo.GameNumber.ToString(), gameOne.Coef1o, gameTwo.Coef2o));

                                }

                                if (CheckFork(gameOne.Coef2, gameTwo.Coef1))
                                    rezult.Add(CreateFork(one, two, gameOne.Coef2, gameTwo.Coef1, gameOne.GameNumber.ToString(), gameTwo.GameNumber.ToString(), gameOne.Coef2o, gameTwo.Coef1o));

                            }
                        }
                    }
                }
            }



            if (one.SportType == SportType.Теннис
                || one.SportType == SportType.Снукер
                || one.SportType == SportType.Бейсбол
                || one.SportType == SportType.Волейбол
                || one.SportType == SportType.Бадминтон
                || one.SportType == SportType.Настольный_теннис)
            {
                rezult.AddRange(GetFork(one, two, BetNumber._1, BetNumber._2));

                foreach (Stake oneFora in one.Foras)
                {
                    if (oneFora.Parametr >= 0)
                    {
                        if (oneFora.StakeType == StakeType.Fora1)
                        {
                            if (CheckFork(oneFora.Coef, two._2))
                                rezult.Add(CreateFork(one, two, oneFora.Coef, two._2, oneFora.ToString(), two.ToMyString(BetNumber._2),
                                    oneFora.ParametrO, two._2o));
                        }
                        else
                        {
                            if (CheckFork(oneFora.Coef, two._1))
                                rezult.Add(CreateFork(one, two, oneFora.Coef, two._1, oneFora.ToString(), two.ToMyString(BetNumber._1),
                                    oneFora.ParametrO, two._1o));
                        }
                    }

                }

                foreach (Stake twoFora in two.Foras)
                {
                    if (twoFora.Parametr >= 0)
                    {
                        if (twoFora.StakeType == StakeType.Fora1)
                        {
                            if (CheckFork(twoFora.Coef, one._2))
                                rezult.Add(CreateFork(two, one, twoFora.Coef, one._2, twoFora.ToString(), one.ToMyString(BetNumber._2),
                                    twoFora.ParametrO, one._2o));
                        }
                        else
                        {
                            if (CheckFork(twoFora.Coef, one._1))
                                rezult.Add(CreateFork(two, one, twoFora.Coef, one._1, twoFora.ToString(), one.ToMyString(BetNumber._1),
                                    twoFora.ParametrO, one._1o));
                        }
                    }

                }

            }



            else
            {

                foreach (Stake oneFora in one.Foras)
                {
                    if (oneFora.Parametr >= 0.5)
                    {
                        if (oneFora.StakeType == StakeType.Fora1)
                        {
                            if (CheckFork(oneFora.Coef, two._2))
                                rezult.Add(CreateFork(one, two, oneFora.Coef, two._2, oneFora.ToString(), two.ToMyString(BetNumber._2),
                                    oneFora.ParametrO, two._2o));
                        }
                        else
                        {
                            if (CheckFork(oneFora.Coef, two._1))
                                rezult.Add(CreateFork(one, two, oneFora.Coef, two._1, oneFora.ToString(), two.ToMyString(BetNumber._1),
                                    oneFora.ParametrO, two._1o));
                        }
                    }
                    else if (oneFora.Parametr > 0)
                    {
                        if (oneFora.StakeType == StakeType.Fora1)
                        {
                            if (CheckFork(oneFora.Coef, two._X2))
                                rezult.Add(CreateFork(two, one, oneFora.Coef, two._X2, oneFora.ToString(), two.ToMyString(BetNumber._X2),
                                    oneFora.ParametrO, one._X2o));
                        }
                        else
                        {
                            if (CheckFork(oneFora.Coef, two._1X))
                                rezult.Add(CreateFork(two, one, oneFora.Coef, two._1X, oneFora.ToString(), two.ToMyString(BetNumber._1X),
                                    oneFora.ParametrO, one._1X));
                        }
                    }

                }

                foreach (Stake twoFora in two.Foras)
                {
                    if (twoFora.Parametr >= 0.5)
                    {
                        if (twoFora.StakeType == StakeType.Fora1)
                        {
                            if (CheckFork(twoFora.Coef, one._2))
                                rezult.Add(CreateFork(two, one, twoFora.Coef, one._2, twoFora.ToString(), one.ToMyString(BetNumber._2),
                                    twoFora.ParametrO, one._2o));
                        }
                        else
                        {
                            if (CheckFork(twoFora.Coef, one._1))
                                rezult.Add(CreateFork(two, one, twoFora.Coef, one._1, twoFora.ToString(), one.ToMyString(BetNumber._1),
                                    twoFora.ParametrO, one._1o));
                        }
                    }
                    else if (twoFora.Parametr > 0)
                    {
                        if (twoFora.StakeType == StakeType.Fora1)
                        {
                            if (CheckFork(twoFora.Coef, one._X2))
                                rezult.Add(CreateFork(two, one, twoFora.Coef, one._X2, twoFora.ToString(), one.ToMyString(BetNumber._X2),
                                    twoFora.ParametrO, one._X2o));
                        }
                        else
                        {
                            if (CheckFork(twoFora.Coef, one._1X))
                                rezult.Add(CreateFork(two, one, twoFora.Coef, one._1X, twoFora.ToString(), one.ToMyString(BetNumber._1X),
                                    twoFora.ParametrO, one._1X));
                        }
                    }

                }



            }



            //Если в игре только 2 исхода, то мы не получим тут вилок, т.к. Х или 12 будет равно 0;
            //Они общие потому что например в баскете победитель матча всегда один. но в половинах или четвертях
            //может быть ничья

            rezult.AddRange(GetFork(one, two, BetNumber._12, BetNumber._X));

            rezult.AddRange(GetFork(one, two, BetNumber._1X, BetNumber._2));

            rezult.AddRange(GetFork(one, two, BetNumber._1, BetNumber._X2));

            rezult.AddRange(GetFork(one, two, BetNumber._1X, BetNumber._X2));





            foreach (Stake oneFora in one.Foras)
            {
                foreach (Stake twoFora in two.Foras)
                {
                    if (oneFora.StakeType != twoFora.StakeType)
                    {
                        if (twoFora.Parametr != oneFora.Parametr &&
                            Math.Abs(twoFora.Parametr) == Math.Abs(oneFora.Parametr))
                        {
                            if (CheckFork(oneFora.Coef, twoFora.Coef))
                                rezult.Add(CreateFork(one, two, oneFora.Coef, twoFora.Coef, oneFora.ToString(),
                                    twoFora.ToString(), oneFora.ParametrO, twoFora.ParametrO));
                        }
                        else if (oneFora.Parametr > 0 && twoFora.Parametr < 0
                                 && Math.Abs(oneFora.Parametr) > Math.Abs(twoFora.Parametr)
                                 ||
                                 twoFora.Parametr > 0 && oneFora.Parametr < 0 &&
                                 Math.Abs(twoFora.Parametr) > Math.Abs(oneFora.Parametr))
                            if (CheckFork(oneFora.Coef, twoFora.Coef))
                                rezult.Add(CreateFork(one, two, oneFora.Coef, twoFora.Coef, oneFora.ToString(),
                                    twoFora.ToString(), oneFora.ParametrO, twoFora.ParametrO));
                    }
                }
            }

            foreach (Stake oneTotal in one.Totals)
            {
                foreach (Stake twoTotal in two.Totals)
                {
                    if (oneTotal.StakeType != twoTotal.StakeType)
                    {
                        if (oneTotal.Parametr == twoTotal.Parametr)
                        {
                            if (CheckFork(oneTotal.Coef, twoTotal.Coef))
                                rezult.Add(CreateFork(one, two, oneTotal.Coef, twoTotal.Coef, oneTotal.ToString(),
                                    twoTotal.ToString(), oneTotal.ParametrO, twoTotal.ParametrO));
                        }
                        else if (oneTotal.StakeType == StakeType.Tmax
                            && twoTotal.StakeType == StakeType.Tmin
                            && oneTotal.Parametr < twoTotal.Parametr)
                        {
                            if (CheckFork(oneTotal.Coef, twoTotal.Coef))
                                rezult.Add(CreateFork(one, two, oneTotal.Coef, twoTotal.Coef, oneTotal.ToString(),
                                    twoTotal.ToString(), oneTotal.ParametrO, twoTotal.ParametrO));
                        }

                        else if (oneTotal.StakeType == StakeType.Tmin
                            && twoTotal.StakeType == StakeType.Tmax
                            && oneTotal.Parametr > twoTotal.Parametr)
                        {
                            if (CheckFork(oneTotal.Coef, twoTotal.Coef))
                                rezult.Add(CreateFork(one, two, oneTotal.Coef, twoTotal.Coef, oneTotal.ToString(),
                                    twoTotal.ToString(), oneTotal.ParametrO, twoTotal.ParametrO));
                        }
                    }
                }
            }

            return rezult;

        }

        /// <summary>
        /// Получить список актуальных вилок на данный момент
        /// </summary>
        public List<Fork> GetAllFork()
        {
            List<Fork> rez;
            lock (_lkRezult)
            {
                rez = _rezult.Where(x => x.Profit < 30 && ((DateTime.Now - x.Created).TotalSeconds < 10)).ToList();
                _rezult = new HashSet<Fork>(rez);
            }
#if DEBUG
            Console.WriteLine($"ForkFinder.GetAllFork: {rez.Count} шт.");
#endif
            return rez;
        }


        /// <summary>
        /// Создает вилку
        /// </summary>
        /// <param name="one">ставка 1</param>
        /// <param name="two">ставка 2</param>
        /// <param name="bt1">нужный коэфицент</param>
        /// <param name="bt2">нужный коэфицент</param>
        /// <param name="to1">Навзание коэфицента</param>
        /// <param name="to2">Навзание коэфицента</param>
        /// <param name="data1">Данные для открытия первой ставки</param>
        /// <param name="data2">Данные для открытия второй ставки</param>
        /// <param name="forktype">Тип вилки</param>
        /// <returns></returns>
        private static Fork CreateFork(Bet one, Bet two, float bt1, float bt2, string to1, string to2, object data1, object data2, ForkType forktype = ForkType.Main)
        {
            Fork fr = new Fork();

            fr.ForkType = forktype;
            fr.SportName = one.Groupe;
            fr.Teams = one.Name;

            fr.Cof1 = bt1;
            fr.Cof2 = bt2;

            fr.SiteOne = one.Site;
            fr.SiteTwo = two.Site;

            fr.To1 = to1;
            fr.To2 = to2;


            fr.Url1 = one.URL;
            fr.Url2 = two.URL;

            //if (one.Time == two.Time)
            //    fr.Time = one.Time;
            //else
            //    fr.Time = one.Time + " " + two.Time;

            if (one.Time != null && one.Time != "-1")
            {
                if (one.Time.Contains(":"))
                {
                    fr.Time = one.Time.Split(':').First() + "'";
                }
                else
                {
                    fr.Time = one.Time;
                }
            }
            else
            {
                if (two.Time != null && two.Time != "-1")
                {
                    if (two.Time.Contains(":"))
                    {
                        fr.Time = two.Time.Split(':').First() + "'";
                    }
                    else
                    {
                        fr.Time = two.Time;
                    }
                }
            }

            if (one.SportType == two.SportType)
                fr.SportType = one.SportType;
            else
                fr.SportType = SportType.Other;

            fr.Data1 = data1;
            fr.Data2 = data2;

            fr.TypeOne = one.Bookmaker;
            fr.TypeTwo = two.Bookmaker;

            fr.Profit = (1 / ((1 / bt1) + (1 / bt2)) - 1) * 100;

            return fr;
        }

        /// <summary>
        /// Проверяет есть ли вилка с такими коэффициентами
        /// </summary>
        /// <param name="kofOne"></param>
        /// <param name="kofTwo"></param>
        /// <returns></returns>
        private bool CheckFork(float kofOne, float kofTwo)
        {
            if (kofOne != 0 || kofTwo != 0)
            {
                var rez = ((1 / kofOne) + (1 / kofTwo));
                if (rez < 1)
                {
                    return true;
                }
            }

            return false;
        }


        private List<Fork> GetFork(Bet one, Bet two, BetNumber bet1, BetNumber bet2, bool invers = true)
        {
            List<Fork> rezult = new List<Fork>();
            var coeff1 = one[bet1];
            var coeff2 = two[bet2];
            if (CheckFork(coeff1, coeff2))
            {
                var fork = CreateFork(one, two, coeff1, coeff2, one.ToMyString(bet1), two.ToMyString(bet2), one.GetData(bet1), two.GetData(bet2));

                rezult.Add(fork);
            }
            if (invers)
            {
                coeff1 = two[bet1];
                coeff2 = one[bet2];

                if (CheckFork(coeff1, coeff2))
                {
                    var fork = CreateFork(two, one, coeff1, coeff2, two.ToMyString(bet1), one.ToMyString(bet2), two.GetData(bet1), one.GetData(bet2));
                    rezult.Add(fork);
                }
            }

            return rezult;
        }


        private List<Fork> GetForkIbt(Bet one, Bet two, IBTBet ibtOne, IBTBet ibtTwo, bool inverse = true)
        {
            var rezult = new List<Fork>();

            float coeff1 = ibtOne.Tmin;
            float coeff2 = ibtTwo.Tmax;
            if (CheckFork(coeff1, coeff2))
            {
                var fork = CreateFork(one, two, coeff1, coeff2, ibtOne.ToMystring(BetNumber._Tmin), ibtTwo.ToMystring(BetNumber._Tmax), ibtOne.GetData(BetNumber._Tmin), ibtTwo.GetData(BetNumber._Tmax), ForkType.IT);
                fork.Teams = ibtOne.TeamName;
                rezult.Add(fork);
            }

            if (inverse)
            {
                coeff1 = ibtTwo.Tmin;
                coeff2 = ibtOne.Tmax;
                if (CheckFork(coeff1, coeff2))
                {
                    var fork = CreateFork(two, one, coeff1, coeff2, ibtTwo.ToMystring(BetNumber._Tmin), ibtOne.ToMystring(BetNumber._Tmax), ibtTwo.GetData(BetNumber._Tmin), ibtOne.GetData(BetNumber._Tmax), ForkType.IT);
                    fork.Teams = ibtOne.TeamName;
                    rezult.Add(fork);
                }
            }

            return rezult;
        }



        private bool CompareTeamsName(string str1, string str2)
        {
#if !DEBUG
            return _bd.StringCompare(str1, str2);
#endif
            var dt1 = str1.Trim().Split(' ');
            var dt2 = str2.Trim().Split(' ');

            if (dt1[0] == dt2[0])
                return true;

            return false;
        }

        public void Dispose()
        {
            _th?.Abort();
        }


    }

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    internal class ForkParsingCompare : IEqualityComparer<Fork>
    {
        public bool Equals(Fork x, Fork y)
        {
            if (x == null || y == null)
                return false;
            if (x.SportType != y.SportType)
                return false;
            if (x.Cof1 != y.Cof1)
                return false;
            if (x.Cof2 != y.Cof2)
                return false;
            if (x.To1 != y.To1)
                return false;
            if (x.To2 != y.To2)
                return false;

            return true;
        }

        public int GetHashCode(Fork obj)
        {
            if (obj == null)
                return 0;
            return obj.GetHashCode();
        }
    }
}
