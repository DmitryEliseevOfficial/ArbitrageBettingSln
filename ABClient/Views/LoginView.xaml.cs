#define TEST1
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ABClient.Protocol;
using ABClient.Data;
using System.Diagnostics;
using System.Net;

namespace ABClient.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        readonly Settings _settings;

        public LoginView()
        {
            _settings = SettingsManger.LoadSettingTry();

            InitializeComponent();
            txtLogin.Text = _settings.Login;
            txtPassword.Password = _settings.Password;
            Task.Factory.StartNew(LoadBaners);
            lblVersion.Content = ABShared.ProjectVersion.Version.ToString();
        }


        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var data = txtServer.Text.Split(':');
            if(data.Length!=2)
            {
                MessageBox.Show("Адрес сервера не верен. Перезапустите программу!");
                return;
            }

            string host = data[0];
            int port = Convert.ToInt32(data[1]);
            string login = txtLogin.Text;
            string password = txtPassword.Password;


            var task = Task<DClient>.Factory.StartNew(delegate()
            {
                return Conection(host, port,login,password);
            });

            var cl = await task;

            this.Dispatcher.Invoke(delegate () {
                btnLogin.IsEnabled = true;
                prgStatus.Visibility = Visibility.Collapsed;
            });

            if (cl == null)
                return;

            if(cl.NeedUpdate)
            {
                var rez=MessageBox.Show("Необходимо обновить клиент! Сделать это сейчас?","Запрос",MessageBoxButton.YesNo);
                if (rez == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start("Updater.exe");
                        this.Close();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Не удалось запустить Updater. Сделайте это вручную. "+ex.Message);
                    }
                    return;
                    
                }
                   
                else
                {
                    MessageBox.Show("Дальнейшая работа невозможна! Программа будет закрыта!");
                    this.Close();                   
                }
            }
            if(!cl.IsLogin)
            {
                return;
            }

            try
            {
                _settings.Login = txtLogin.Text;
                _settings.Password = txtPassword.Password;
                SettingsManger.SaveSettings(_settings);
                var mn = new MainView(cl,_settings);
                mn.Show();
                this.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show(ex.StackTrace);
            }
        }


        private DClient Conection(string host, int port, string login, string password)
        {

            this.Dispatcher.Invoke(delegate () {
                btnLogin.IsEnabled = false;
                prgStatus.Visibility = Visibility.Visible;
            });

            DClient cl = new DClient(host,port);
            try
            {
                if (!cl.InitConnection())
                {
                    MessageBox.Show("Не удалось соединиться с сервером");
                    return null;
                }
                cl.SignIn(login, password);
            }
            catch (Exception ex)
            {                
                MessageBox.Show(ex.Message);
                return cl;
            }
            return cl;
        }


        private void LoadBaners()
        {
#if DEBUG
            this.Dispatcher.Invoke(delegate ()
            {
                txtServer.Text = "192.168.0.194:9999";
                btnLogin.IsEnabled = true;
                prgStatus.Visibility = Visibility.Collapsed;
            });
            return;
#endif
          

            string server_data = "";


            bool good = false;
            try
            {
                WebClient wb = new WebClient();
                wb.Encoding = Encoding.UTF8;
                wb.DownloadFile("http://f-king.ru/baners/Tarif.jpg", @"ProgramData\Tarif.jpg");
                wb.DownloadFile("http://f-king.ru/baners/baner.jpg", @"ProgramData\Baners\baner.jpg");
                wb.DownloadFile("http://f-king.ru/baners/small_baner.jpg", @"ProgramData\Baners\small_baner.jpg");
                wb.DownloadFile("http://f-king.ru/baners/Partners.jpg", @"ProgramData\Baners\Partners.jpg");

                server_data = wb.DownloadString("http://f-king.ru/settings/ServerIP.txt");
                good = true;
            }

            catch(Exception ex)

            {
                MessageBox.Show("Не удалось загрузить информацию о сервере. проверьте ваше подключение к сети, и запустите еще раз!");
            }

            //Делаем доступную кнопку входа
            this.Dispatcher.Invoke(delegate () {
                txtServer.Text = server_data;

                if(good)
                    btnLogin.IsEnabled = true;

                prgStatus.Visibility = Visibility.Collapsed;
            });

        }
    }
}
