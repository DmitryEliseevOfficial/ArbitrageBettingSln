using ABServer.Model.Users;
using System;
using System.Windows;
using ABServer.Commands;
using System.Windows.Input;

namespace ABServer.ViewModel
{
    class UserViewModel : DependencyObject
    {
        UsersManager manager;
        User _oldUser;
        public event Action Close;

        #region DependencyProperty Defined

        // Using a DependencyProperty as the backing store for User.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(User), typeof(UserViewModel), new PropertyMetadata(new User()));

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register("MyProperty", typeof(ICommand), typeof(UserViewModel), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for AddNewUserCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddNewUserCommandProperty =
            DependencyProperty.Register("AddNewUserCommand", typeof(ICommand), typeof(UserViewModel), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for NewUser.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NewUserProperty =
            DependencyProperty.Register("IsNewUser", typeof(bool), typeof(UserViewModel), new PropertyMetadata(true));


        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsWriteDataErrorProperty =
            DependencyProperty.Register("IsWriteDataError", typeof(bool), typeof(UserViewModel), new PropertyMetadata(false));


        // Using a DependencyProperty as the backing store for NewLoginCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NewLoginCommandProperty =
            DependencyProperty.Register("NewLoginCommand", typeof(ICommand), typeof(UserViewModel), new PropertyMetadata(null));


        #endregion

        public UserViewModel(User user)
        {
            if (user != null)
            {
                _oldUser = this.User = user;
                IsNewUser = false;
            }
            else
            {
                User = new User();
                IsNewUser = true;
                _oldUser = null;
            }
            this.NewPasswordGenerate = new RealyCommand(NewPassword);
            this.AddNewUserCommand = new RealyCommand(AddNewUser);
            this.NewLoginCommand = new RealyCommand(NewLogin);
            manager = new UsersManager();
            manager.GetAllUser();
        }

        public User User
        {
            get { return (User)GetValue(UserProperty); }
            set { SetValue(UserProperty, value); }
        }


        public bool IsNewUser
        {
            get { return (bool)GetValue(NewUserProperty); }
            set { SetValue(NewUserProperty, value); }
        }

        public ICommand NewPasswordGenerate
        {
            get { return (ICommand)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }


        public ICommand AddNewUserCommand
        {
            get { return (ICommand)GetValue(AddNewUserCommandProperty); }
            set { SetValue(AddNewUserCommandProperty, value); }
        }

        public ICommand NewLoginCommand
        {
            get { return (ICommand)GetValue(NewLoginCommandProperty); }
            set { SetValue(NewLoginCommandProperty, value); }
        }


        private void NewLogin()
        {
            RandomPassword.SetParams(true, true, false, false);
            User.Login = RandomPassword.Generate(10);
            RandomPassword.Restore();
        }

        private void NewPassword()
        {
            User.Password = RandomPassword.Generate(8, 10);
        }

        private void AddNewUser()
        {
            if (IsNewUser)
            {
                manager.AddUser(User);
            }
            else
            {
                manager.UpdateUser(_oldUser, User);
            }

            if (Close != null)
                Close();
        }

        public bool IsWriteDataError
        {
            get { return (bool)GetValue(IsWriteDataErrorProperty); }
            set { SetValue(IsWriteDataErrorProperty, value); }
        }



    }
}
