using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ABShared;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ABServer.Parsers
{
    internal class ParserManager:IDisposable
    {
        private Thread _thParsing;

        private readonly bool _usingProxy;

        private readonly List<IParse> _parsersList = new List<IParse>();
        private readonly List<string> _mirorsList = new List<string>();
        readonly ConcurrentDictionary<BookmakerType, List<Bet>> _currentBets = new ConcurrentDictionary<BookmakerType, List<Bet>>();

        public ParserManager(string olimpUrl,string fonbetUrl,string marafonUrl,string zenitUrl,string pariMacthUrl,bool usingProxy=false)
        {
            _usingProxy = usingProxy;

            _parsersList.Add(new Olimp());
            _parsersList.Add(new Zenit());
            _parsersList.Add(new Fonbet());
            _parsersList.Add(new NewMarafon());

            _mirorsList.Add(olimpUrl);
            _mirorsList.Add(zenitUrl);
            _mirorsList.Add(fonbetUrl);
            _mirorsList.Add(marafonUrl);

            if(_parsersList.Count!=_mirorsList.Count)
                throw new ArgumentException($"Количество парсеров, не равно количеству зеркал!");
        }


        public void Start()
        {
            List<string> proxyList = new List<string>();

            if (_usingProxy)
                proxyList = File.ReadAllLines("proxy.txt").ToList();

            for (int i = 0; i < _parsersList.Count; i++)
            {
                _parsersList[i].UsingProxy = _usingProxy;
                if (_usingProxy)
                    _parsersList[i].ProxyList = proxyList;
                _parsersList[i].SetUrl(_mirorsList[i]);
                _currentBets[_parsersList[i].Bookmaker]=new List<Bet>();
            }

            _thParsing = new Thread(Update);
            _thParsing.IsBackground = true;
            _thParsing.Start();
        }

        private List<Thread> _threads;
        private void Update()
        {
            Logger.AddLog($"ParserManger.Updater запущен", Logger.LogTarget.ParserManager);
            while (true)
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    if (_threads == null)
                    {
                        _threads = new List<Thread>();
                        foreach (IParse parse in _parsersList)
                        {
                            var th = new Thread(Parsing);
                            th.IsBackground = true;
                            _threads.Add(th);
                            th.Start(parse);
                            Logger.AddLog($"Запустили {parse.GetType()} прошло времени: {sw.ElapsedMilliseconds} мс", Logger.LogTarget.ParserManagerTimingStart, Logger.LogLevel.Info);
                        }
                    }



                }
                catch (Exception e)
                {
                    Logger.AddLog($"Произошло что-то страшное при запуске парсеров {e.Message}", Logger.LogTarget.ParserManager, Logger.LogLevel.Fatal);
                    
                }

                return;
            }
        }


        private void Parsing(object paring)
        {
            IParse parser = paring as IParse;
            if(parser==null)
                return;
            //Stopwatch sw = new Stopwatch();
           
            while (true)
            {
                try
                {
                   // sw.Start();
                    var rezult = parser.Parse();
                    _currentBets[parser.Bookmaker] = rezult;
              
                }
                catch (ThreadAbortException)
                {
                    Logger.AddLog(
                        $"{parser.GetType()} Успели спарсить только {_currentBets[parser.Bookmaker].Count} ставок. Нехватило времени.",
                        Logger.LogTarget.ParserManager, Logger.LogLevel.Warn);
                }
                catch (Exception ex)
                {
                    Logger.AddLog(
                        $"{parser.GetType()} не спарсили все ставки, а только {_currentBets[parser.Bookmaker].Count} ставки. И вот почему: {ex.Message}",
                        Logger.LogTarget.ParserManager, Logger.LogLevel.Epic);
                }
                finally
                {
                    //var workTime = (int)sw.ElapsedMilliseconds;
                    //if (workTime < 2500)
                    //    Thread.Sleep(2500 - workTime);
                    //sw.Reset();
                }
            }
        }

     
        public List<Bet> GetAllBet()
        {

            return _currentBets.Values.SelectMany(x => x).ToList();
        }

        public void Dispose()
        {
            _thParsing?.Abort();
            if (_threads != null)
            {
                foreach (Thread thread in _threads)
                {
                    thread.Abort();
                }
            }
        }

        /// <summary>
        /// For Test
        /// </summary>
        /// <returns></returns>
        public List<Bet> FakeParse(bool reload = false)
        {
            if (!reload)
                return FakeLoad();
            var rezult = new List<Bet>();
            var mr = new NewMarafon();
            mr.GetBetsTest();
            rezult.AddRange(mr.Bets);
            Console.WriteLine($"NewMarafon. Bets:{mr.Bets.Count}");

            var olimp = new Olimp();
            olimp.SetUrl("https://noji8227sewvp4j.bmkolimp.xyz");
            olimp.GetBetTest();
            rezult.AddRange(olimp.Bets);
            Console.WriteLine($"Olimp. Bets:{olimp.Bets.Count}");


            var fonbet = new Fonbet();
            fonbet.Parse();
            Thread.Sleep(3000);
            var r = fonbet.Parse();
            rezult.AddRange(r);
            Console.WriteLine($"Fonbet. Bets:{r.Count}");

            Console.WriteLine($"Всего спарсенно ставок: {rezult.Count}");

            var memoryStream = new MemoryStream();
            var bf = new BinaryFormatter();
            bf.Serialize(memoryStream, rezult);
            File.WriteAllBytes("Forsk.dt", memoryStream.ToArray());

            return rezult;
        }
        
        /// <summary>
        /// For Test
        /// </summary>
        /// <returns></returns>
        private List<Bet> FakeLoad()
        {
            var data = File.ReadAllBytes("Forsk.dt");
            var mr = new MemoryStream(data);
            var bf = new BinaryFormatter();
            return (List<Bet>)bf.Deserialize(mr);
        }
    }
}
