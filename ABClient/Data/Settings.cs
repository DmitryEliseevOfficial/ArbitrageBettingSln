using ABShared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ABClient.Data
{
    [Serializable]
    public class Settings : INotifyPropertyChanged
    {
        public List<Bookmaker> bookmakers { get; set; } = new List<Bookmaker>();
        public List<SportTypeData> Sports { get; set; } = new List<SportTypeData>();

        public string Login { get; set; } = "";
        public string Password { get; set; } = "";

        public bool IsMusic { get; set; }
        public bool IsFocusShow1 { get; set; }
        public bool IsDoubleButton { get; set; }
        public int MaxPercent { get; set; } = 80;

        public bool LoadCourse { get; set; }
        public bool IsCheckBalance { get; set; }

        double _CourseUSD = 0;
        public double CourseUSD
        {
            get
            {
                return _CourseUSD;
            }
            set
            {
                if (value != _CourseUSD)
                {
                    _CourseUSD = value;
                    PropChanged();
                }
            }
        }

        double _CourseEUR = 0;
        public double CourseEUR
        {
            get
            {
                return _CourseEUR;
            }
            set
            {
                if (value != _CourseEUR)
                {
                    _CourseEUR = value;
                    PropChanged();
                }
            }
        }

        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.116 Safari/537.36 OPR/40.0.2308.81";
        public bool IsProxy { get; set; }
        public string Proxy { get; set; } = "http://127.0.0.1:9090";

        public int MaxBank { get; set; } = 10000;



        public WindowsSettings WinSet { get; set; } = new WindowsSettings();


        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void PropChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
