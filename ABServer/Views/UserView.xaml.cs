using ABServer.Model.Users;
using ABServer.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ABServer.Views
{
    /// <summary>
    /// Логика взаимодействия для UserView.xaml
    /// </summary>
    public partial class UserView : Window
    {
        User _user;
        UserViewModel viewModel;

        public UserView(User user=null)
        {
            InitializeComponent();

            //Если нужно создать нового пользователя
            if(user==null)
            {
                _user = new User();
                this.Title = "Добавить пользователя";
                btnSave.Content = "Добавить";
            }
            _user = user;
            viewModel= new UserViewModel(_user);
            viewModel.Close += ViewModel_Close;
            this.Loaded += UserView_Loaded;
        }

        private void ViewModel_Close()
        {
            this.Close();
        }

        private void UserView_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = viewModel;
        }

        /// <summary>
        /// Убираем рамку после неправильного ввода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            //#FFABADB3
            var o = sender as TextBox;
            o.BorderBrush = new SolidColorBrush( Color.FromRgb(0xAB,0xAD,0xB3) );
        }


    }
    
}
