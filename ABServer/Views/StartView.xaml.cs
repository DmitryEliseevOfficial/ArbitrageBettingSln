using System;
using ABServer.ViewModel;

namespace ABServer.Views
{
    /// <summary>
    /// Логика взаимодействия для StartView.xaml
    /// </summary>
    public partial class StartView 
    {
        public StartView()
        {
            InitializeComponent();
            Loaded += StartView_Loaded;
        }

        private void StartView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
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
                Dt_Closed();

            StartViewModel dt = new StartViewModel();
            DataContext = dt;
            dt.Closed += Dt_Closed;
        }

        private void Dt_Closed()
        {
            MainView mv = new MainView();
            mv.Show();
            Close();
        }
    }
}
