using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StaticData.Shared.Model;
using xNet;

namespace ABServer.Parsers.fonbetModel
{

    public class FonbetClient
    {

        public int FromVersion { get; private set; }

        private CurrentLine _line = new CurrentLine();

        private UnicDataDecorator _bd;


        public bool IsFirstLoad { get; set; } = true;


        private bool IsChanged { get; set; }


        private readonly object _lockObj = new object();

        private string _url;
        private string _apiUrl;
        private HttpRequest _req;

        public FonbetClient(string url)
        {
            _url = url;
        }



        internal void ParseData(FonbetResponse data)
        {
            if (_bd == null)
            {
                _bd = new UnicDataDecorator(UnicData.Load("bd.data"));
                UnicDataDecorator.UpdateBase += UnicDataDecorator_UpdateBase;
            }

            lock (_lockObj)
            {
                if (IsFirstLoad)
                {
                    LoadLink(data);
                }
                else
                {
                    if (FromVersion != data.FromVersion)
                    {
                        IsFirstLoad = true;
                        throw new ArgumentException($"Пришел не тот пакет обновления {FromVersion} != {data.FromVersion}");
                    }
                    UpdateLink(data);
                }
                _line.LastUpdate = DateTime.Now;
            }

            FromVersion = data.PacketVersion;
        }

        private void UnicDataDecorator_UpdateBase(List<UnicData> obj)
        {
            _bd.SetBase(obj);
        }


        private void UpdateLink(FonbetResponse data)
        {
#if DEBUG
            //Делаем замер, за сколько наша линия перестраиваеться
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            bool changed = false;
            //Дописываем новый вид спорта
            foreach (var sport in data.Sports)
            {
                _line.Sports[sport.Id] = sport;
            }

            //добавляем новые события
            foreach (var dataEvent in data.Events)
            {
                if (dataEvent.Place == "live")
                {
                    if (!_line.Events.ContainsKey(dataEvent.Id))
                    {
                        dataEvent.Factors = new Dictionary<int, CustomFactor>();
                        if (dataEvent.Level == 1)
                        {
                            dataEvent.Team1Id = _bd.GetNumber(dataEvent.Team1);
                            dataEvent.Team2Id = _bd.GetNumber(dataEvent.Team2);
                        }
                        _line.Events.Add(dataEvent.Id, dataEvent);
                        changed = true;
                    }
                }
                else if (dataEvent.Place == "notActive")
                {
                    _line.Events.Remove(dataEvent.Id);
                    changed = true;
                }
                else
                {

                }

            }

            //Обновляем доп информацию о событиях
            foreach (EventMisc eventMisc in data.EventMiscs)
            {
                if (!_line.Events.ContainsKey(eventMisc.Id))
                {
                    Console.WriteLine($"Нет такого id {eventMisc.Id} для eventMisc");
                    continue;
                }
                _line.Events[eventMisc.Id].EventMisc = eventMisc;
                changed = true;
            }

            //Добавляем/обновляем новые ставкви
            foreach (CustomFactor factor in data.CustomFactors)
            {
                if (!_line.Events.ContainsKey(factor.E))
                {
                    Console.WriteLine($"Нет такого id {factor.E} для Factora");
                    continue;
                }
                var ev = _line.Events[factor.E];

                if (factor.V == 0 && ev.Factors.ContainsKey(factor.F))
                {
                    ev.Factors.Remove(factor.F);
                    changed = true;
                }
                else
                {
                    if (factor.V != 0)
                    {
                        ev.Factors[factor.F] = factor;
                        changed = true;
                    }
                }
            }

            //Блокируем/разблокируем события
            foreach (var block in data.EventBlocks)
            {
                if (!_line.Events.ContainsKey(block.EventId))
                {
                    Console.WriteLine($"Странно. нет такого события: {block.EventId}");
                    continue;
                }
                if (block.State == "unblocked")
                {
                    _line.Events[block.EventId].IsBlock = false;
                    foreach (var customFactor in _line.Events[block.EventId].Factors)
                    {
                        customFactor.Value.IsBlocked = false;
                    }
                    changed = true;
                }
                else if (block.State == "blocked")
                {
                    _line.Events[block.EventId].IsBlock = true;
#if DEBUG
                    Console.WriteLine($"Событие №{block.EventId} было заблокированно. {_line.Events[block.EventId].Team1}-{_line.Events[block.EventId].Team2}");
#endif
                    var bl = _line.Events[block.EventId].Factors;
                    foreach (var factor in bl)
                    {
                        factor.Value.IsBlocked = true;
                    }
                    changed = true;
                }
                else if (block.State == "partial")
                {
                    foreach (var blockFactor in block.Factors)
                    {
                        if (_line.Events[block.EventId].Factors.ContainsKey(blockFactor))
                        {
#if DEBUG
                            Console.WriteLine($"Ставка №{blockFactor} было заблокированна. {_line.Events[block.EventId].Team1}-{_line.Events[block.EventId].Team2}");
#endif
                            _line.Events[block.EventId].Factors[blockFactor].IsBlocked = true;
                        }
                        else
                            Console.WriteLine($" в {block.EventId} нет такого фактора {blockFactor}. Не могу заблокировать");

                    }
                    changed = true;
                }
                else
                {

                }
            }

            IsChanged = changed;
#if DEBUG
            sw.Stop();
            Console.WriteLine($"Линия была обновленна за {sw.Elapsed}");
            System.Threading.Thread.Sleep(1000);
#endif
        }


        private void LoadLink(FonbetResponse data)
        {
#if DEBUG
            //Делаем замер, за сколько наша линия перестраиваеться
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            _line = new CurrentLine();

            foreach (Event dataEvent in data.Events)
            {
                dataEvent.Factors = new Dictionary<int, CustomFactor>();
                if (dataEvent.Level == 1)
                {
                    dataEvent.Team1Id = _bd.GetNumber(dataEvent.Team1);
                    dataEvent.Team2Id = _bd.GetNumber(dataEvent.Team2);
                }
                _line.Events.Add(dataEvent.Id, dataEvent);
            }
            foreach (EventMisc eventMisc in data.EventMiscs)
            {
                var ev = _line.Events[eventMisc.Id];
                if (ev != null)
                {
                    ev.EventMisc = eventMisc;
                }
            }
            foreach (CustomFactor factor in data.CustomFactors)
            {
                var ev = _line.Events[factor.E];
                if (ev != null)
                {
                    if (ev.Factors == null)
                        ev.Factors = new Dictionary<int, CustomFactor>();

                    ev.Factors[factor.F] = factor;
                }
            }

            _line.Sports = new Dictionary<int, Sport>();
            foreach (var sport in data.Sports)
            {
                _line.Sports.Add(sport.Id, sport);
            }

            //Блокируем/разблокируем события

            foreach (var block in data.EventBlocks)
            {
                if (!_line.Events.ContainsKey(block.EventId))
                {
                    Console.WriteLine($"Странно. нет такого события: {block.EventId}");
                    continue;
                }
                if (block.State == "unblocked")
                {
                    _line.Events[block.EventId].IsBlock = false;
                    foreach (var customFactor in _line.Events[block.EventId].Factors)
                    {
                        customFactor.Value.IsBlocked = false;
                    }
                }
                else if (block.State == "blocked")
                {
                    _line.Events[block.EventId].IsBlock = true;

                }
                else if (block.State == "partial")
                {
                    foreach (var blockFactor in block.Factors)
                    {
                        if (_line.Events[block.EventId].Factors.ContainsKey(blockFactor))
                            _line.Events[block.EventId].Factors[blockFactor].IsBlocked = true;
                        else
                            Console.WriteLine($" в {block.EventId} нет такого фактора {blockFactor}");
                    }
                }
                else
                {

                }
            }

            IsFirstLoad = false;
#if DEBUG
            sw.Stop();
            Console.WriteLine($"Линия была впервые загруженна за {sw.Elapsed}");
#endif
        }


        internal CurrentLine GetLine()
        {
            lock (_lockObj)
            {
                return _line;
            }
        }


        private Thread _thUpdate;

        public void Start()
        {
            if (_req == null)
            {
                _req = new HttpRequest();
                _req.CharacterSet = Encoding.UTF8;
                _req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36 OPR/41.0.2353.46";
            }
            try
            {
                if (string.IsNullOrEmpty(_apiUrl))
                    _apiUrl = GetUrlApi();
            }
            catch (Exception e)
            {
                Logger.AddLog($"Не удалось получить URL API. {e.Message}", Logger.LogTarget.FonbetClient, Logger.LogLevel.Critical);
            }
            if (_thUpdate == null)
            {
                _thUpdate = new Thread(Update);
                _thUpdate.IsBackground = true;
                _thUpdate.Start();
            }
        }

        private void Update()
        {
            Stopwatch sw = new Stopwatch();
            while (true)
            {
                try
                {
                    sw.Restart();
                    string url;
                    if (IsFirstLoad)
                        url = _apiUrl + "live/currentLine/ru/";
                    else
                        url = _apiUrl + $"live/updatesFromVersion/{FromVersion}/ru/";
                    var response = _req.Get(url).ToString();
                    FonbetResponse fonbetResponse = JsonConvert.DeserializeObject<FonbetResponse>(response);
                    ParseData(fonbetResponse);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds < 1000)
                        Thread.Sleep(1000 - (int)sw.ElapsedMilliseconds);
                    Logger.AddLog($"Линия была загружена за  {sw.ElapsedMilliseconds}", Logger.LogTarget.FonbetClient);
                }
                catch (Exception e)
                {
                    Logger.AddLog($"Ошибка загрузки линии: {e.Message}", Logger.LogTarget.FonbetClient, Logger.LogLevel.Critical);
                    IsFirstLoad = false;
                    Thread.Sleep(1000);
                }


            }
        }


        //делаем запрос и получаем адресс сайта с апи
        private string GetUrlApi()
        {
            string respone = _req.Get($"{_url}urls.json").ToString();
            if (respone.StartsWith("<"))
                throw new ArgumentException("Fonbet забанен");
            JObject json = JObject.Parse(respone);
#if DEBUG
            return "https:" + json["line"]?.Last + "/";
#endif

            return "https:" + json["line"]?.Last + "/";

        }
    }
}