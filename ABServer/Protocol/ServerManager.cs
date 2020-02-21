using ABServer.Model.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ABServer.Protocol
{
    /// <summary>
    /// Авторизовывает пользователя и отправляет ему данные с вилками
    /// </summary>
    internal class ServerManager
    {
        private readonly ForkFinder _finder;
        private Thread _thListen;
        private Thread _thSending;
        private TcpListener _listener;

        private readonly Dictionary<User, DServer> _clients = new Dictionary<User, DServer>();


        public ServerManager(ForkFinder finder)
        {
            _finder = finder;
        }

        /// <summary>
        /// Запускает фоновые процессы, для рассылки вилок и принятия новых подключений
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void StartListen(string ip, int port)
        {
            IPAddress ipAddr = IPAddress.Parse(ip);
            _listener = new TcpListener(ipAddr, port);
            _listener.Start();

            _thListen = new Thread(AcceptConection);
            _thListen.IsBackground = true;
            _thListen.Start();

            _thSending = new Thread(SendingData);
            _thSending.IsBackground = true;
            _thSending.Start();

            Logger.AddLog($"ServerManager успешно запущенпараметры запуска: {ip}:{port}", Logger.LogTarget.ServerManager, Logger.LogLevel.Info);
        }

        /// <summary>
        /// Принимает новые подключения клиентов
        /// </summary>
        private void AcceptConection()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (_listener.Pending())
                {
                    var cl = _listener.AcceptTcpClient();
                    var serv = new DServer(cl);
                    try
                    {
                        User authUser = serv.MakeAuth();

                        if (authUser == null)
                            return;

                        if (_clients.ContainsKey(authUser))
                        {
                            Logger.AddLog($"Пользователь {authUser.Login},{authUser.Email} Уже был подключен. Но подключился снова",
                                Logger.LogTarget.ServerManager, Logger.LogLevel.Warn);
                            _clients[authUser].SendResetAuth();
                            // _clients[AuthUser].Dispose();
                        }

                        _clients[authUser] = serv;
                        Logger.AddLog($"Пользователь {authUser.Login},{authUser.Email} успешно присоединился",
                                Logger.LogTarget.ServerManager, Logger.LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Logger.AddLog($"не удалось подключить пользователя. {cl.Client.RemoteEndPoint}. {ex.Message}",
                            Logger.LogTarget.ServerManager, Logger.LogLevel.Warn);
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Отправляет вилки клиентам
        /// </summary>
        private void SendingData()
        {
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var data = _finder.GetAllFork();
                List<DServer> clients;
                try
                {
                    clients = _clients.Select(x => x.Value).ToList();
                }
                catch (Exception ex)
                {
                    clients = new List<DServer>();
                    Logger.Write(ex.Message);
                }

                foreach (var key in clients)
                {
                    try
                    {
                        key.Listening();
                        key.SendFork(data);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                sw.Stop();

                if (data.Count != 0)
                    Logger.AddLog($"Вилки были успешно отправленны клиентам за {sw.ElapsedMilliseconds} мс в количестве {data.Count}", Logger.LogTarget.ServerManager);
                if (sw.ElapsedMilliseconds > 2000)
                    Logger.AddLog($"Долго отправляли вилки за {sw.ElapsedMilliseconds} мс в количестве {data.Count}", Logger.LogTarget.ServerManager, Logger.LogLevel.Critical);

                Thread.Sleep(MainConfigurate.Configurate.ServerManagerSendTime);
            }
        }

        /// <summary>
        /// Останавливает все фоновые потоки
        /// </summary>
        public void StopListen()
        {
            foreach (var key in _clients)
            {
                try
                {
                    key.Value.Dispose();
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                _listener.Stop();
                _thListen?.Abort();
                _thSending?.Abort();
            }
            catch (Exception ex)
            {
                Logger.AddLog($"Не удалось коректно остановить. И вот почему {ex.Message}", Logger.LogTarget.ParserManager, Logger.LogLevel.Critical);
            }

        }
    }
}
