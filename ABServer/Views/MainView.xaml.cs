using System;
using System.Windows;
using ABServer.ViewModel;

namespace ABServer.Views
{
    /// <summary>
    /// Логика взаимодействия для MainView.xaml
    /// </summary>
    public partial class MainView 
    {
        private MainViewModel _vm;

        public MainView()
        {
                  
            InitializeComponent(); 
            Loaded += MainView_Loaded;
        }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.GetLogData += Logger_GetLogData;
            _vm = new MainViewModel();
            DataContext = _vm;
           
        }

        private void Logger_GetLogData(string obj)
        {
            AddRowLog(obj);
        }
        
        private void AddRowLog(string message)
        {
            try
            {
                LbLog.Items.Add(DateTime.Now + ": " + message);
            }
            catch
            {
            }
        }
        
        private void btnCostumer_Click(object sender, RoutedEventArgs e)
        {
            CostumerView vs = new CostumerView();
            vs.ShowDialog();
        }        
       
    }

   


}
