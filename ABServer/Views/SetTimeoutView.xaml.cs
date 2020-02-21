using System.Windows;
using ABServer.ViewModel;

namespace ABServer.Views
{
    /// <summary>
    /// Логика взаимодействия для SetTimeoutView.xaml
    /// </summary>
    public partial class SetTimeoutView : Window
    {
        public SetTimeoutView()
        {
            InitializeComponent();
            DataContext = new SetTimeoutViewModel();
        }
    }
}
