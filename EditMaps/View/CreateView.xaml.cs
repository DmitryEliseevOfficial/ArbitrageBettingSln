using EditMaps.ViewModel;
using System.Windows;

namespace EditMaps.View
{
    /// <summary>
    /// Логика взаимодействия для CreateView.xaml
    /// </summary>
    public partial class CreateView
    {
        private readonly CreateViewModel _vm;
        public CreateView()
        {
            InitializeComponent();
            _vm= new CreateViewModel();
            DataContext = _vm;
            Closing += CreateView_Closing;
            WindowState = WindowState.Maximized;
        }


        private void CreateView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!_vm.Save())
            {
                var rez=MessageBox.Show("Вы уверены, что хотите закрыть приложение?","",MessageBoxButton.YesNo);
                if (rez == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private bool _show;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!_show)
            {
                _show = true;
                gridBase.Visibility = Visibility.Visible;
                spBase.Visibility = Visibility.Visible;
            }
            else
            {
                _show = false;
                gridBase.Visibility = Visibility.Collapsed;
                spBase.Visibility = Visibility.Collapsed;
            }
        }
    }
}
