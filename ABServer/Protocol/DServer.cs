//#define OT
using ABServer.Model.Users;
using ABShared;
using ABShared.Protocol;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace ABServer.Protocol
{
    class DServer: IDisposable
    {
        TcpClient _client;
        NetworkStream _stream;
        double _version = 1.0;
        User _curenuser;        

        public DServer(TcpClient client)
        {
            _client = client;                      
        }


        public void Listening()
        {
           _stream = _client.GetStream();

            if(_client.Available!=0)
            {
                var packet = ReadData();
              
                switch(packet.Comand)
                {
                    case CommandCode.CheckVersion:
                    {
                        WriteVersion(packet);
                        break;
                    }                  
                    case CommandCode.GetLeftDay:
                    {
                        GetLeftDay();
                        break;
                    }
                    case CommandCode.GetSites:
                    {
                        GetSiteData(packet);
                        break;
                    }
                    case CommandCode.AddSiteData:
                    {
                        AddSiteData(packet);
                        break;
                    }
                    default:
                        break;
                }
                //_stream.WriteByte((byte)StatusCode.EndRequest);
            }
        }

        private void GetLeftDay()
        {
            var packet = new Packet();
            packet.Code = StatusCode.LeftDays;
            packet.Data = (_curenuser.Left - DateTime.Now).TotalDays;
            SendData(packet);
        }

        private void GetSiteData(Packet packet)
        {
            var bookamker =(BookmakerType) packet.Data;
            var sitemanager = new SiteManager();          
            packet = new Packet();
            packet.Code = StatusCode.SitesData;
            packet.Data = sitemanager.GetData(bookamker);
            SendData(packet);
        }

        private void AddSiteData(Packet packet)
        {
            var sitemanager = new SiteManager();
            var dt = packet.Data.ToString().Split('|');
            var bk = (BookmakerType)Enum.Parse(typeof(BookmakerType), dt[0]);
            var site = dt[1];
            sitemanager.Add(bk, site);
        }


        internal User MakeAuth()
        {
            _stream = _client.GetStream();           
          
    

            var packet = ReadData();
            var check=WriteVersion(packet);
            if (!check)
            {
                SendCloseConnection();      

                throw new ArgumentException($"{packet.Data} !={_version} Клиенту необходимо обновиться");
            }

            //Получаем логин/пароль
            packet = ReadData();
            if (packet.Comand != CommandCode.SingIn)
                throw new ArgumentException($"{packet.Comand} не соответсвует коду авторизации");

            var lgData = packet.Data.ToString().Split('|');
            string login = lgData[0];
            string password = lgData[1];

            var manager = new UsersManager();
                        
            User user=null;
            try
            {
                user = manager.MakeAuth(login, password);
            }
            catch(Exception ex)
            {
                packet = new Packet();
                packet.Code = StatusCode.SingInFail;
                SendData(packet);

                _client.Close();
                throw ex;
            }

            //проверяем не кончилась ли подписка
            var time = user.Left - DateTime.Now;
            if (time.TotalMilliseconds<0)
            {
                packet = new Packet();
                packet.Code = StatusCode.HasEnd;
                SendData(packet);              
                _client.Close();
                throw new ArgumentException($"{user.Login} закончилась подписка");
            }

            packet = new Packet();
            packet.Code = StatusCode.SingInOK;

            _curenuser = user;
        

            return user;
        }

        private bool WriteVersion(Packet packet)
        {
            Version ver = Version.Parse(packet.Data.ToString());
            bool rezult=true;
            packet = new Packet();
            if (ver!=ProjectVersion.Version)
            {                
                packet.Code=StatusCode.NeedUpdate;
                rezult = false;                     
            }
            else
            {               
                packet.Code= StatusCode.VersionOK;               
            }
            SendData(packet);

            return rezult;
        }

        public void SendFork(List<Fork> data)
        {
            if((_curenuser.Left-DateTime.Now).TotalMilliseconds<0)
            {
                Packet packet = new Packet();
                packet.Code = StatusCode.HasEnd;                
                SendData(packet);
            }
            else
            {
                Packet packet = new Packet();
                packet.Code = StatusCode.DataOK;
                packet.Data = data;
                SendData(packet);
            }            
        }
        
        //Делает сброс авторизации
        public void SendResetAuth()
        {
           try
            {
                Packet packet = new Packet();
                packet.Code = StatusCode.NeedSignIn;
                SendData(packet);
            }
            catch
            {

            }
        }

        private void SendData(Packet packet)
        {
            BinaryFormatter fr = new BinaryFormatter();
            fr.Serialize(_stream, packet);
        }

        private Packet ReadData()
        {     
            BinaryFormatter fr = new BinaryFormatter();
            return fr.Deserialize(_stream) as Packet;
        }


        private void SendCloseConnection()
        {
            try
            {
                Packet packet = new Packet();
                packet.Code = StatusCode.CloseConnection;
                SendData(packet);
            }
            catch
            {

            }
        }

        public void Dispose()
        {
            SendCloseConnection();
            _client.Close();
        }
    }
}
