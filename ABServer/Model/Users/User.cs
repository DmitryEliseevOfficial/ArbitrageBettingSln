using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ABServer.Model.Users
{

    [Serializable]
    public class User : INotifyPropertyChanged
    {

        public string Login
        {
            get { return _login; }
            set
            {
                if (_login == value) return;
                _login = value;
                PropChanged();
            }
        }
        private string _login = "";

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password == value) return;
                _password = value;
                PropChanged();
            }
        }
        private string _password = "";


        public DateTime Left { get; set; } = DateTime.Now;

        public int Expected { get; set; }

        public bool IsBlocked { get; set; } = false;

        public string FName { get; set; }

        public string LName { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public bool IsFreeUSed { get; set; } = false;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void PropChanged([CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

    }

}
