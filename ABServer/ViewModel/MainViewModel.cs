using ABServer.Model;
using ABServer.Parsers;
using ABServer.Protocol;
using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ABServer.Commands;
using System.Collections.ObjectModel;
using Microsoft.Win32;

namespace ABServer.ViewModel
{
    internal class MainViewModel:INotifyPropertyChanged
    {
        private readonly ServerManager _server;
        private ForkFinder _forkFinder;
        private ParserManager _manager;
        private readonly Settings _set;
       
        public MainViewModel()
        {
            _set = Settings.Load();            
           
            StarListenerCommand = new RealyCommand(StarListener);
            StartParsingCommand = new RealyCommand(StartParsing);
            StopParsingCommand = new RealyCommand(StopParsing);
            UpdateBaseCommand = new RealyCommand(UpdateBase);

            List<Bookmaker> bookmakers = new List<Bookmaker>
            {
                new Bookmaker() {Name = "Olimp", Site = _set.OlimpUrl},
                new Bookmaker() {Name = "Fonbet", Site = _set.FonbetUrl},
                new Bookmaker() {Name = "Marafon", Site = _set.MarafonUrl},
                new Bookmaker() {Name = "Zenit", Site = _set.ZenitUrl},
                new Bookmaker() {Name = "Parimatch", Site = _set.PariMatchUrl}
            };

            Bookmakers = new ObservableCollection<Bookmaker>(bookmakers);

            _manager = new ParserManager(_set.OlimpUrl, _set.FonbetUrl, _set.MarafonUrl, _set.ZenitUrl,_set.PariMatchUrl, _set.UsingProxy);
            _forkFinder = new ForkFinder(_manager);
            _server = new ServerManager(_forkFinder);

            //Проверяем был ли сервер запущен после обновления
            string[] args = Environment.GetCommandLineArgs();
            bool isUpdate = false;
            foreach (string arg in args)
            {
                if (arg != "update") continue;
                isUpdate = true;
                break;
            }
            if (isUpdate)
            {
                StarListenerCommand.Execute(null);
#if !DEBUG
                StartParsingCommand.Execute(null);
                
#endif
            }
        }

        public ObservableCollection<Bookmaker> Bookmakers { get; set; }

        public RealyCommand StarListenerCommand { get; set; }
        public RealyCommand StartParsingCommand { get; set; }
        public RealyCommand StopParsingCommand { get; set; }
        public RealyCommand UpdateBaseCommand { get; set; }

        private bool _listen;
        public bool Listen
        {
            get { return _listen; }
            set
            {
                if(_listen!=value)
                {
                    _listen = value;
                    PropChanged();
                }
            }
        }

        private void StarListener()
        {
            if(!Listen)
            {
                try
                {
                    _server.StartListen(_set.Host, _set.Port);
                    Logger.Write("Подключение клиентов запущенно!");
                    Listen = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Сервер небыл развернут. Проверьте настройки и попытайтесь снова " + ex.Message);
                }
            }
            else
            {
                _server.StopListen();
                Logger.Write("Подключение клиентов остановленно!");
                Listen = false;
            }
            
        }

        private bool _start;
        public bool Start
        {
            get { return _start; }
            set
            {
                if (_start == value) return;
                _start = value;
                PropChanged();
            }
        }

        private void StartParsing()
        {
            if(!Start)
            {
                if(_manager==null)
                {
                    _manager = new ParserManager(_set.OlimpUrl, _set.FonbetUrl, _set.MarafonUrl,_set.ZenitUrl,_set.PariMatchUrl, _set.UsingProxy);
                }
                _manager.Start();
                if(_forkFinder==null)
                {
                    _forkFinder = new ForkFinder(_manager);
                }
                _forkFinder.Start();
                foreach(var key in Bookmakers)
                {
                    key.Status = WorkStatus.Work;
                }
                Logger.Write("Сбор данных успешно запущен!");
                Start = true;
            }
            else
            {
                MessageBox.Show("Парсинг уже запущен!");
            }
            
        }

        private void StopParsing()
        {
            if(Start)
            {
                _manager?.Dispose();
                _forkFinder?.Dispose();
                foreach (Bookmaker key in Bookmakers)
                {
                    key.Status = WorkStatus.Stop;
                }

                Logger.Write("Парсинг остановлен!");
                Start = false;
            }
            else
            {
                Logger.Write("Парсинг не был запущен!");
            }
        }


        private void UpdateBase()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Укажите файл с базой",
                Filter = "Файл базы данных(*.data) |*.data"
            };
            dlg.ShowDialog();
            if (String.IsNullOrEmpty(dlg.FileName))
                return;
            try
            {
                var bd=StaticData.Shared.Model.UnicData.Load(dlg.FileName);
               
                StaticData.Shared.Model.UnicData.Save("bd.data", bd);
                UnicDataDecorator.SetUpdateBase(bd);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Выбранный файл поврежден или содержит записей"+ex.Message);               
                return;
            }
           

            MessageBox.Show("Данные успешно обновленны!");
           
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void PropChanged([CallerMemberName] string name="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
