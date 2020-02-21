using ABServer.Commands;
using ABServer.Model;
using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using ABServer.Views;

namespace ABServer.ViewModel
{
    internal class StartViewModel:DependencyObject
    {
      
        public static readonly DependencyProperty HostAdressProperty =
            DependencyProperty.Register("HostAdress", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));
        
        public static readonly DependencyProperty HostPortProperty =
            DependencyProperty.Register("HostPort", typeof(int), typeof(StartViewModel), new PropertyMetadata(9999));
        
        public static readonly DependencyProperty UsingProxyProperty =
            DependencyProperty.Register("UsingProxy", typeof(bool), typeof(StartViewModel), new PropertyMetadata(false));

        
        public static readonly DependencyProperty ZenitLoginProperty =
            DependencyProperty.Register("ZenitLogin", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));
        
        public static readonly DependencyProperty ZenitPasswordProperty =
            DependencyProperty.Register("ZenitPassword", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));
        
        public static readonly DependencyProperty CheckProxyFileCommandProperty =
            DependencyProperty.Register("CheckProxyFileCommand", typeof(ICommand), typeof(StartViewModel), new PropertyMetadata(null));
        
        public static readonly DependencyProperty StartCommandProperty =
            DependencyProperty.Register("StartCommand", typeof(ICommand), typeof(StartViewModel), new PropertyMetadata(null));

        
        public static readonly DependencyProperty MarafonUrlProperty =
            DependencyProperty.Register("MarafonUrl", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));

        
        public static readonly DependencyProperty OlimpUrlProperty =
            DependencyProperty.Register("OlimpUrl", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));
        
        public static readonly DependencyProperty FonbetUrlProperty =
            DependencyProperty.Register("FonbetUrl", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));
        
        public static readonly DependencyProperty ZenitUrlProperty =
            DependencyProperty.Register("ZenitUrl", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));
        
        public static readonly DependencyProperty PariMatchUrlProperty =
            DependencyProperty.Register("PariMatchUrl", typeof(string), typeof(StartViewModel), new PropertyMetadata(""));


        public event Action Closed;


        #region DependencyProp
     
        public string HostAdress
        {
            get { return (string)GetValue(HostAdressProperty); }
            set { SetValue(HostAdressProperty, value); }
        }
        
        public int HostPort
        {
            get { return (int)GetValue(HostPortProperty); }
            set { SetValue(HostPortProperty, value); }
        }
        
        public bool UsingProxy
        {
            get { return (bool)GetValue(UsingProxyProperty); }
            set { SetValue(UsingProxyProperty, value); }
        }
        
        public string ZenitLogin
        {
            get { return (string)GetValue(ZenitLoginProperty); }
            set { SetValue(ZenitLoginProperty, value); }
        }
        
        public string ZenitPassword
        {
            get { return (string)GetValue(ZenitPasswordProperty); }
            set { SetValue(ZenitPasswordProperty, value); }
        }
        
        public string OlimpUrl
        {
            get { return (string)GetValue(OlimpUrlProperty); }
            set { SetValue(OlimpUrlProperty, value); }
        }
        
        public string FonbetUrl
        {
            get { return (string)GetValue(FonbetUrlProperty); }
            set { SetValue(FonbetUrlProperty, value); }
        }
        
        public string MarafonUrl
        {
            get { return (string)GetValue(MarafonUrlProperty); }
            set { SetValue(MarafonUrlProperty, value); }
        }
        
        public string ZenitUrl
        {
            get { return (string)GetValue(ZenitUrlProperty); }
            set { SetValue(ZenitUrlProperty, value); }
        }

        public string PariMatchUrl
        {
            get { return (string)GetValue(PariMatchUrlProperty); }
            set { SetValue(PariMatchUrlProperty, value); }
        }
        #endregion

        #region Command`s
        public ICommand CheckProxyFileCommand
        {
            get { return (ICommand)GetValue(CheckProxyFileCommandProperty); }
            set { SetValue(CheckProxyFileCommandProperty, value); }
        }

        public ICommand StartCommand
        {
            get { return (ICommand)GetValue(StartCommandProperty); }
            set { SetValue(StartCommandProperty, value); }
        }
        #endregion

        public RealyCommand OpenSetTimeoutCommand { get; set; }

        public StartViewModel()
        {
            String host = Dns.GetHostName();
   
            IPAddress[] ips = Dns.GetHostAddresses(host);
            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    HostAdress = ip.ToString();
                    break;
                }
            }

            CheckProxyFileCommand = new RealyCommand(delegate () { CheckProxyFile(); });
            StartCommand = new RealyCommand(Start);
            OpenSetTimeoutCommand = new RealyCommand(OpenSetTimeout);

            Settings set = Settings.Load();
            ZenitLogin = set.ZenitLogin;
            ZenitPassword = set.ZenitPassword;
            UsingProxy = set.UsingProxy;
            OlimpUrl = set.OlimpUrl;
            FonbetUrl = set.FonbetUrl;
            MarafonUrl = set.MarafonUrl;
            ZenitUrl = set.ZenitUrl;
            PariMatchUrl = set.PariMatchUrl;

           
        }


        private void OpenSetTimeout()
        {
            new SetTimeoutView().ShowDialog();
        }


        private bool CheckProxyFile()
        {
            if (!UsingProxy)
                return true;          
            if(!File.Exists("proxy.txt"))
            {
                MessageBox.Show("Файл proxy.txt не найден! Положите файл с прокси в корень!");
                return false;
            }
            if(!File.Exists("bd.data"))
            {
                MessageBox.Show("Файл bd.data не найден! Положите файл с прокси в корень!");
                return false;
            }
            return true;
        }

        private void Start()
        {
            if (!CheckProxyFile())
                return;

           
            //Сохраняем настройки
            Settings set = new Settings
            {
                UsingProxy = UsingProxy,
                ZenitLogin = ZenitLogin,
                ZenitPassword = ZenitPassword,
                Host = HostAdress,
                Port = HostPort,
                OlimpUrl = OlimpUrl,
                MarafonUrl = MarafonUrl,
                FonbetUrl = FonbetUrl,
                ZenitUrl = ZenitUrl,
                PariMatchUrl = PariMatchUrl
            };

            Settings.Save(set);
            Closed?.Invoke();
            
        }

     
    }


}
