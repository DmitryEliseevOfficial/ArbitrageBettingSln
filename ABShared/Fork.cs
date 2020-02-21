using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ABShared
{
    [Serializable]
    public class Fork : INotifyPropertyChanging
    {
        public Fork()
        {
            Created = DateTime.Now;
        }

        public ForkType ForkType { get; set; } = ForkType.Main;

        public string SportName { get; set; }

        public SportType SportType { get; set; }

        public string Teams { get; set; }
        public string Time { get; set; }

        public BookmakerType TypeOne { get; set; }
        public BookmakerType TypeTwo { get; set; }


        public string SiteOne { get; set; }
        public string SiteTwo { get; set; }

        public float Cof1 { get; set; }
        public float Cof2 { get; set; }

        public string To1 { get; set; }
        public string To2 { get; set; }

        public object Data1 { get; set; }
        public object Data2 { get; set; }


        public string Url1 { get; set; }
        public string Url2 { get; set; }

        public float Profit { get; set; }

        public DateTime Created { get; }

        //[Obsolete]
        //public bool IsLast { get; set; }

        //private int _lifeTime;

        //public int LifeTime
        //{
        //    get { return _lifeTime; }
        //    set
        //    {
        //        if(_lifeTime==value) return;
        //        _lifeTime = value;
        //        OnPropertyChanged();
        //    }
        //}
        //public int DeathTime { get; set; }

        public override string ToString()
        {
            // return To1 + " - " + To2 + " | " + Cof1 + "-" + Cof2 + " | " + Teams;
            return $"{Profit:F}% | {To1}  - {To2} | {Cof1:##.##} - {Cof2:##.##} \n {Teams}";
        }

        public override bool Equals(object obj)
        {
            var equal = obj as Fork;

            if (equal?.TypeOne != TypeOne)
                return false;
            if (equal.TypeTwo != TypeTwo)
                return false;
            if (equal.To1 != To1)
                return false;
            if (equal.To2 != To2)
                return false;
            if (equal.Teams != Teams)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int rezult = 0;

            rezult += TypeOne.GetHashCode();
            rezult += TypeTwo.GetHashCode();
            if (!String.IsNullOrWhiteSpace(To1))
                rezult += To1.GetHashCode();
            if (!String.IsNullOrWhiteSpace(To2))
                rezult += To2.GetHashCode();
            if (!String.IsNullOrWhiteSpace(Teams))
                rezult += Teams.GetHashCode();
            return rezult;
        }

        [field: NonSerialized]
        public event PropertyChangingEventHandler PropertyChanging;

        private void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
        }
    }
}
