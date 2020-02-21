using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ABServer.Model
{
    internal class Bookmaker : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Site { get; set; }

        private WorkStatus _status = WorkStatus.Stop;
        public WorkStatus Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    PropChanged();
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void PropChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    internal enum WorkStatus
    {
        Work,
        Stop,
        Error
    }
}
