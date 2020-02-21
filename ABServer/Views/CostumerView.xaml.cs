using System.Windows;

namespace ABServer.Views
{
    /// <summary>
    /// Логика взаимодействия для CostumerView.xaml
    /// </summary>
    public partial class CostumerView : Window
    {
        public CostumerView()
        {
            InitializeComponent();

            this.Loaded += CostumerView_Loaded;
        }

        private void CostumerView_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new ViewModel.CostumerViewModel();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            UserView uv = new UserView();
            uv.ShowDialog();
        }
    }
}
