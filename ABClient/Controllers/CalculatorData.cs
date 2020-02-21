using System.ComponentModel;
using System.Runtime.CompilerServices;
using ABClient.Annotations;

namespace ABClient.Controllers
{

    public class CalculatorData : INotifyPropertyChanged
    {
        public double coeff1 { get; set; }
        public double coeff2 { get; set; }

        public int bet1 { get; set; }
        public int bet2 { get; set; }

        public double profit1 { get; set; }
        public double profit2 { get; set; }
        public double Mean { get; set; }

        public int Staf { get; set; }

        public double ForkProfit { get; set; }

        private bool _isFork;

        public bool IsFork
        {
            get { return _isFork; }
            set
            {
                if (_isFork == value) return;
                _isFork = value; OnPropertyChanged();
            }
        }

        public void Reverse()
        {
            var Tcoef = coeff2;
            coeff2 = coeff1;
            coeff1 = Tcoef;

            var Tbet = bet2;
            bet2 = bet1;
            bet1 = Tbet;

            var TProfit = profit2;
            profit2 = profit1;
            profit1 = TProfit;

        }

        public CalculatorData Clone()
        {
            CalculatorData dt = new CalculatorData
            {
                coeff1 = coeff1,
                coeff2 = coeff2,
                bet1 = bet1,
                bet2 = bet2,
                profit1 = profit1,
                profit2 = profit2,
                Mean = Mean,
                Staf = Staf,
                ForkProfit = ForkProfit,
                IsFork = IsFork
            };

            return dt;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
