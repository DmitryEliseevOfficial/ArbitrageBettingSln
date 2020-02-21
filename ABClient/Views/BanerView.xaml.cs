using System.Windows;

namespace ABClient.Views
{
    /// <summary>
    /// Логика взаимодействия для BanerView.xaml
    /// </summary>
    public partial class BanerView : Window
    {
        public BanerView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
