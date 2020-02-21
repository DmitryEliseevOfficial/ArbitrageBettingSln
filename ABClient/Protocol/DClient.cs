//#define OT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using ABShared.Protocol;
using System.IO;
using System.Threading;
using System.Windows;
using ABShared;
using System.Runtime.Serialization.Formatters.Binary;

namespace ABClient.Protocol
{

    public class DClient: IDisposable
    {
        string _version = "1.0.0.0";

        public string Version { get { return _version.ToString(); } }


        public bool NeedUpdate { get; private set; }


        public bool IsLogin { get; private set; }

        private TcpClient _client;
        private readonly string _host;
        private readonly int _port;
        string _login;
        string _password;



        public DClient(string host,int port)
        {
            _host = host;
            _port = port;
        }


        public bool InitConnection()
        {
            try
            {
                _client = new TcpClient();
                _client.ReceiveTimeout = 10000;
                _client.SendTimeout = 10000;
               
                _client.Connect(_host, _port);                
                CheckVersion();
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }       


        private void CheckVersion()
        {
            if (_client == null)
                throw new ArgumentNullException(typeof(DClient) + ": client==null");

            var packet = new Packet();
            packet.Comand = CommandCode.CheckVersion;
            packet.Data = ProjectVersion.Version.ToString();
            SendData(packet);

            packet = ReadData();           
            if (packet.Code == StatusCode.NeedUpdate)
            {
                NeedUpdate = true;
            }
                  
              
        }

        public bool SignIn(string login,string password)
        {            
            if (_client == null)
                throw new ArgumentNullException(typeof(DClient) + ": client==null");

            var stream = _client.GetStream();

            Packet pack = new Packet();
            pack.Comand = CommandCode.SingIn;
            pack.Data = $"{login}|{password}";
            SendData(pack);

            pack = ReadData();
            if (pack.Code == StatusCode.SingInFail)
                throw new ArgumentException("Неверный логин/пароль!");
            if(pack.Code== StatusCode.HasEnd)
                throw new ArgumentException("Срок подписки истек!");
            else
            {
                _login = login;
                _password = password;
                IsLogin = true;
                return true;
            }
        }

        public void ParseData()
        {
            if (!_client.Connected)
                throw new ArgumentException("Нет связи с сервером;");

            if (_client.Available == 0)
                return;

            var packet = ReadData();

            if(packet.Code==StatusCode.DataOK)
            {
                GetData?.Invoke(packet.Data as List<Fork>);
            }
            else if(packet.Code== StatusCode.SitesData)
            {
                GetSiteData?.Invoke(packet.Data as List<string>);
            }
            else if(packet.Code==StatusCode.LeftDays)
            {
                GetLeftDays?.Invoke(Convert.ToInt32(packet.Data));
            }  
            else if (packet.Code == StatusCode.HasEnd)
            {
                throw new ArgumentException("Срок подписки истек!");
            }
            else if (packet.Code == StatusCode.NeedSignIn)
            {
                throw new ArgumentException("Авторизация сброшена. Закройте все клиенты!");
            }
            else if(packet.Code== StatusCode.CloseConnection)
            {
                MessageBox.Show("Сервер закрыл соединение. Попробуйте через пару минут снова");
                try
                {
                    _client.Close();
                }
                catch
                {

                }
            }
            else
            {
                throw new ArgumentException("Ошибка не извесна ");
            }

        }


        public event Action<List<Fork>> GetData;
  
        public event Action<int> GetLeftDays;

        public event Action<List<string>> GetSiteData;

        public void SendLeftDays()
        {
            var packet = new Packet();
            packet.Comand = CommandCode.GetLeftDay;
            SendData(packet);
        }

        public void Ping()
        {
            var packet = new Packet();
            packet.Comand = CommandCode.Ping;
            packet.Data = new object();
            SendData(packet);
        }

        public void SendSiteData(BookmakerType bookmaker)
        {
            var packet = new Packet();
            packet.Comand = CommandCode.GetSites;
            packet.Data = bookmaker;
            SendData(packet);
        }

        public void AddSiteData(BookmakerType bookmaker,string site)
        {
            var packet = new Packet();
            packet.Comand = CommandCode.AddSiteData;
            packet.Data = bookmaker.ToString() + "|" + site;
            SendData(packet);
        }

        private void SendData(Packet packet)
        {
            if (!_client.Connected)
                throw new ArgumentException("Нет связи с сервером;");
            var stream = _client.GetStream();
            stream.ReadTimeout = 10000;
            BinaryFormatter fr = new BinaryFormatter();
            
            fr.Serialize(stream, packet);
        }

        private Packet ReadData()
        {
            if (!_client.Connected)
                throw new ArgumentException("Нет связи с сервером;");

            var stream = _client.GetStream();
            stream.ReadTimeout = 10000;
            BinaryFormatter fr = new BinaryFormatter();
            
            return fr.Deserialize(stream) as Packet;
        }

        public void Dispose()
        {
            _client?.Close();
            
        }

        public void Reconect()
        {
            InitConnection();
            SignIn(_login, _password);
        }
    }
}
