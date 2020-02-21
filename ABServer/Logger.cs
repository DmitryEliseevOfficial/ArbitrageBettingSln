#define GRUBLOG
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ABServer
{
    //Простой многопоточный логер
    public static class Logger
    {
        private static Thread _thWorker;
        private static readonly ConcurrentQueue<Work> _works = new ConcurrentQueue<Work>();

        private static bool IsInit { get; set; }
        private static readonly object _locker = new object();

        public static event Action<string> GetLogData;

        public static void Write(object data)
        {
            if (GetLogData != null)
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, GetLogData, data);
        }

        private static void Worker()
        {
            string path = $"{Environment.CurrentDirectory}\\Logs";
            
            while (true)
            {
                try
                {
                    while (_works.Count!=0)
                    {
                        Work currentWork;
                        _works.TryDequeue(out currentWork);
                        if(currentWork==null)
                            continue;

                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        string fileName = $"{path}\\{currentWork.Target}.txt";
                        if(currentWork.Target==LogTarget.Marafon
                            || currentWork.Target == LogTarget.MarafonThread
                            || currentWork.Target == LogTarget.OlimpThread
                            || currentWork.Target == LogTarget.ServerManager)
                        {
                            File.AppendAllText(fileName, $"{currentWork.GetMessage()}{Environment.NewLine}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                Thread.Sleep(1000);
            }
        }


        private static void Init()
        { 
            if (IsInit) return;
            lock (_locker)
            {
                _thWorker = new Thread(Worker) {IsBackground = true};
                _thWorker.Start();
                IsInit = true;
                AddLog("Logger запущен!");
            }
           
        }

        public static void AddLog(string message)
        {
            AddLog(message, LogTarget.Common, LogLevel.Info);
        }


        public static void AddLog(string message,LogLevel level)
        {
            AddLog(message, LogTarget.Common, level);

        }


        public static void AddLog(string message, LogTarget target)
        {
            AddLog(message, target, LogLevel.Info);
        }


        public static void AddLog(string message, LogTarget target, LogLevel level)
        {
#if !GRUBLOG
            return;
#endif
            if (!IsInit)
                Init();
            _works.Enqueue(new Work(message,target,level));
        }

        private class Work
        {
            private LogLevel LogLevel { get; }
            private string Message { get; }
            private DateTime Time { get; }
            public LogTarget Target { get; }

            public string GetMessage()
            {
                return $"{LogLevel};{Time:u}; {Message}";
            }


            public Work(string message)
            {
                LogLevel= LogLevel.Info;
                Time= DateTime.Now;
                Message = message;
                Target=LogTarget.Common;
            }


            public Work(string message,LogLevel level)
            {
                LogLevel = level;
                Time = DateTime.Now;
                Message = message;
                Target = LogTarget.Common;
            }


            public Work(string message, LogTarget target)
            {
                LogLevel = LogLevel.Info;
                Time = DateTime.Now;
                Message = message;
                Target = target;
            }

            public Work(string message, LogTarget target, LogLevel level)
            {
                LogLevel = level;
                Time = DateTime.Now;
                Message = message;
                Target = target;
            }
        }


        public enum LogLevel
        {
            Info,
            Critical,
            Error,
            Epic,
            Fatal,
            Warn
        }


        public enum LogTarget
        {

            Common,
            Olimp,
            OlimpThread,
            Fonbet,
            Zenit,
            Marafon,
            ServerManager,
            ParserManager,
            ParserManagerTimingStart,
            ForkFinder,
            ForkFinberBetBad,
            MarafonThread,
            FonbetClient
        }
    }
}
