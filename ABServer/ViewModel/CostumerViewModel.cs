using ABServer.Commands;
using ABServer.Model.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ABServer.ViewModel
{
    class CostumerViewModel : DependencyObject
    {
        #region DependecyProperty Defined
        // Using a DependencyProperty as the backing store for FindValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FindValueProperty =
            DependencyProperty.Register("FindValue", typeof(string), typeof(CostumerViewModel), new PropertyMetadata(""));

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentItemsProperty =
            DependencyProperty.Register("CurrentItems", typeof(List<User>), typeof(CostumerViewModel), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterCommandProperty =
            DependencyProperty.Register("FilterCommand", typeof(ICommand), typeof(CostumerViewModel), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for RemoveCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RemoveCommandProperty =
            DependencyProperty.Register("RemoveCommand", typeof(ICommand), typeof(CostumerViewModel), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for SelectedUser.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedUserProperty =
            DependencyProperty.Register("SelectedUser", typeof(User), typeof(CostumerViewModel), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for SetTarifCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SetTarifCommandProperty =
            DependencyProperty.Register("SetTarifCommand", typeof(ICommand), typeof(CostumerViewModel), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for EditUserCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditUserCommandProperty =
            DependencyProperty.Register("EditUserCommand", typeof(ICommand), typeof(CostumerViewModel), new PropertyMetadata(null));
        // Using a DependencyProperty as the backing store for InfoCopyCommand.  This enables animation, styling, binding, etc...

        public static readonly DependencyProperty InfoCopyCommandProperty =
            DependencyProperty.Register("InfoCopyCommand", typeof(ICommand), typeof(CostumerViewModel), new PropertyMetadata(null));
        #endregion

        UsersManager manager;


        public CostumerViewModel()
        {
            manager = new UsersManager();
            CurrentItems = manager.GetAllUser();
            UsersManager.BaseUpdate += UsersManager_BaseUpdate;
            FilterCommand = new RealyCommand(Filter);
            RemoveCommand = new RealyCommand(Remove);
            SetTarifCommand = new RealyCommand(SetTarif);
            EditUserCommand = new RealyCommand(EditUser);
            InfoCopyCommand = new RealyCommand(InfoCopy);
        }

        private void UsersManager_BaseUpdate()
        {
            Filter();
        }

        public string FindValue
        {
            get { return (string)GetValue(FindValueProperty); }
            set { SetValue(FindValueProperty, value); }
        }

        public List<User> CurrentItems
        {
            get { return (List<User>)GetValue(CurrentItemsProperty); }
            set { SetValue(CurrentItemsProperty, value); }
        }



        public User SelectedUser
        {
            get { return (User)GetValue(SelectedUserProperty); }
            set { SetValue(SelectedUserProperty, value); }
        }


        public ICommand FilterCommand
        {
            get { return (ICommand)GetValue(FilterCommandProperty); }
            set { SetValue(FilterCommandProperty, value); }
        }

        public ICommand RemoveCommand
        {
            get { return (ICommand)GetValue(RemoveCommandProperty); }
            set { SetValue(RemoveCommandProperty, value); }
        }

        public ICommand SetTarifCommand
        {
            get { return (ICommand)GetValue(SetTarifCommandProperty); }
            set { SetValue(SetTarifCommandProperty, value); }
        }

        public ICommand EditUserCommand
        {
            get { return (ICommand)GetValue(EditUserCommandProperty); }
            set { SetValue(EditUserCommandProperty, value); }
        }

        public ICommand InfoCopyCommand
        {
            get { return (ICommand)GetValue(InfoCopyCommandProperty); }
            set { SetValue(InfoCopyCommandProperty, value); }
        }


        private void Filter()
        {
            if (FindValue == "")
                CurrentItems = manager.GetAllUser().ToList();
            else
            {
                CurrentItems = manager.GetAllUser().Where(x => x.Login.Contains(FindValue)).ToList();
            }
        }

        private void Remove(object obj)
        {
            var user = SelectedUser;
            if (user == null)
                return;
            var rez = MessageBox.Show("Вы уверены, что хотите удалить " + user.Login, "Запрос", MessageBoxButton.OKCancel);
            if (rez == MessageBoxResult.OK)
                manager.UserDelete(user);
        }

        private void SetTarif(object obj)
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Никто не выбран.");
                return;
            }
            int day;
            Int32.TryParse(obj.ToString(), out day);

            if (day == 0)
            {
                if (SelectedUser.IsFreeUSed)
                {
                    MessageBox.Show($"{SelectedUser.Login} Уже использовал FREE тариф.");
                    return;
                }
                var rez = MessageBox.Show($"Вы уверены,что хотите дать {SelectedUser.Login} Тариф FREE?", "Запрос", MessageBoxButton.YesNo);
                if (rez == MessageBoxResult.Yes)
                {
                    SelectedUser.IsFreeUSed = true;
                    SelectedUser.Left = DateTime.Now.AddDays(5);
                    manager.UpdateUser(SelectedUser);
                }
                return;
            }
            else
            {
                var rez = MessageBox.Show($"Вы уверены,что хотите дать {SelectedUser.Login}  {day} дней пользования?", "Запрос", MessageBoxButton.YesNo);
                if (rez == MessageBoxResult.Yes)
                {
                    SelectedUser.IsFreeUSed = true;
                    if ((SelectedUser.Left - DateTime.Now).TotalSeconds > 0)
                    {
                        SelectedUser.Left = SelectedUser.Left.AddDays(day);
                    }
                    else
                        SelectedUser.Left = DateTime.Now.AddDays(day);

                    var s = SelectedUser.Left - DateTime.Now;
                    manager.UpdateUser(SelectedUser);
                }
            }


        }

        private void EditUser()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Никто не выбран.");
                return;
            }

            var userView = new Views.UserView(SelectedUser);
            userView.ShowDialog();
        }

        private void InfoCopy()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Никто не выбран.");
                return;
            }

            var Info = String.Format("Имя пользователя: {0}{3}Пароль:{1}{3}Время окончания: {2}", SelectedUser.Login, SelectedUser.Password, SelectedUser.Left, Environment.NewLine);
            Clipboard.SetText(Info);
            MessageBox.Show("Информация успешно скопированна в буфер обмена");
        }
    }
}
