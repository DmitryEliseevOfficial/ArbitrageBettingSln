using ABShared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ABClient.Annotations;

namespace ABClient
{
    [Serializable]
    public class Bookmaker:INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public bool IsShow { get; set; }       
        
        public string Url { get; set; }

        
        public string Login { get; set; }

        public string Password { get; set; }
        

        [NonSerialized]
        private bool _isloginValue;

        public bool IsLogin
        {
            get { return _isloginValue; }
            set
            {
                if(_isloginValue==value) return;
                _isloginValue = value;
                OnPropertyChanged();
            }
        }

       

        public BookmakerType BkType { get; set; }
        public List<SportTypeData> Sports { get; set; }
        
        public bool IsFilter { get; set; } = true;

        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
