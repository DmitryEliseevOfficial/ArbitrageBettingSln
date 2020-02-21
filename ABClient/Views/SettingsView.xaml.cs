using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ABClient.Data;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using ABClient.Protocol;

namespace ABClient.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public event Action<Settings> UpdateSetting;


        List<string> userAgents;        
        Settings settings;

        DClient client;

        public SettingsView(Settings set,DClient client,int? id=null)
        {
            this.client = client;
            settings = set;
            InitializeComponent();

            lblVersion.Content = ABShared.ProjectVersion.Version.ToString();
            this.DataContext = settings;

            try
            {
                txtLince.Text = File.ReadAllText(Environment.CurrentDirectory + @"/ProgramData/Lince.txt");
                userAgents = File.ReadAllLines(@"Data\UserAgentsList.txt").ToList();
                userAgents.Insert(0, settings.UserAgent);
                cmbUserAgents.ItemsSource = userAgents;
                lvBookmakes.SelectedIndex = 0;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка загруки. "+ex.Message);
            }

            if (id != null)
            {
                tbcTabs.SelectedIndex = 1;
                lvBookmakes.SelectedIndex = (int)id;
            }

            client.GetLeftDays += Client_GetLeftDays;
            client.GetSiteData += Client_GetSiteData;
            this.Loaded += SettingsView_Loaded;
        }
        

        private void Client_GetSiteData(List<string> obj)
        {            
            cmbUrl.Dispatcher.Invoke(delegate()
            {
                cmbUrl.ItemsSource = null;
                cmbUrl.ItemsSource = obj;
            });
            
        }

        private void Client_GetLeftDays(int obj)
        {
            btnLeft.Dispatcher.Invoke(delegate()
            {
                btnLeft.Content = $"Осталось {obj} дней.";
            });
        }

        private  void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(delegate () { LoadData(); });
        }
        

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SettingsManger.SaveSettings(settings);
            if (UpdateSetting != null)
                UpdateSetting(settings);
            this.Close();
        }
        

        private void btnClearCookie_Click(object sender, RoutedEventArgs e)
        {
            CefSharp.Cef.Shutdown();
            string path = System.IO.Path.Combine(Environment.CurrentDirectory, "Sdk", "Cef", "cache");
            try
            {
                Directory.Delete(path,true);
                MessageBox.Show("Для продолжения работы перезапустите программу!");
                UpdateSetting(settings);
                this.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Не удалось очистить cookie. "+ ex.Message);
            }
        }

        //Загружает курс с ЦБ
        private async Task<Tuple<float, float>> LoadCurs()
        {
            float usd = 0;
            float evro = 0;

            try
            {
                WebClient wb = new WebClient();
                var respone = wb.DownloadString("http://www.cbr.ru/scripts/XML_daily.asp?date_req=" + DateTime.UtcNow.ToString("dd/MM/yyyy"));
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(respone);
                foreach (System.Xml.XmlElement kurs in doc.ChildNodes[1].ChildNodes)
                {
                    if (kurs["NumCode"].InnerText == "840")
                    {
                        usd = float.Parse(kurs["Value"].InnerText);
                    }
                    else if (kurs["NumCode"].InnerText == "978")
                    {
                        evro = float.Parse(kurs["Value"].InnerText);
                    }
                }
            }
            catch
            {

            }

            return new Tuple<float, float>(usd, evro);
        }

        private void ckdLoad_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(delegate () { LoadData(); });
        }

        //Загружает курс и отображает его
        private async Task LoadData()
        {
            client.SendLeftDays();
            if (settings.LoadCourse)
            {
                var curs = await LoadCurs();
                settings.CourseUSD = curs.Item1;
                settings.CourseEUR = curs.Item2;
            }
        }
        

        private void lvBookmakes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvBookmakes.SelectedItem == null)
                return;
            var sl = lvBookmakes.SelectedItem as Bookmaker;
            try
            {
                client.SendSiteData(sl.BkType);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Не удалось получить список зеркал. "+ex.Message);
            }

        }

        private void BtnCheck_Click(object sender, RoutedEventArgs e)
        {

            if (lvBookmakes.SelectedItem == null)
                return;
            var sl = lvBookmakes.SelectedItem as Bookmaker;
            try
            {
                client.AddSiteData(sl.BkType, txtUrl.Text);
                client.SendSiteData(sl.BkType);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Не удалось добавить зеркало" + ex.Message);
            }
           
        }

        private void cmbUrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUrl.SelectedIndex == -1)
                return;

            if (lvBookmakes.SelectedItem == null)
                return;
            var sl = lvBookmakes.SelectedItem as Bookmaker;
            if (sl == null)
                return;
            sl.Url= txtUrl.Text = cmbUrl.SelectedItem.ToString();

        }
    }
}
