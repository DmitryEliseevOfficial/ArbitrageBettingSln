using EditMaps.ViewModel;
using System.Windows;

namespace EditMaps.View
{
    /// <summary>
    /// Логика взаимодействия для MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private MainViewModel _vm;
        public MainView()
        {
            InitializeComponent();
            this.Loaded += MainView_Loaded;
        }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = new MainViewModel();
            this.DataContext = _vm;
            _vm.Loger.CollectionChanged += Loger_CollectionChanged;
        }

        private void Loger_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Dispatcher.Invoke(delegate() {
                try
                {
                    Log.Items.Add(e.NewItems[0].ToString());
                }
                catch
                {
                }
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CreateView cv = new CreateView();
            cv.Show();
        }

    }
}
