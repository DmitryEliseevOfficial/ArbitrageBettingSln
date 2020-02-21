using System;
using System.Diagnostics;

namespace ABShared
{
    //Описывает ставки ИТ 
    [Serializable]
    public class IBTBet
    {
        public string TeamName { get; set; }
        public float Coeff { get; set; }

        public float Tmin { get; set; }
        public object Tmino { get; set; }

        public float Tmax { get; set; }
        public object Tmaxo { get; set; }

        public float this[BetNumber numb] { get { return Iterator(numb); } set { SetIterator(numb, value); } }

        private float Iterator(BetNumber numb)
        {
            switch (numb)
            {
                case BetNumber._Tmin:
                {
                    return Tmin;
                }
                case BetNumber._Tmax:
                {
                    return Tmax;
                }
                default:
                    return -1;
            }
        }

        private void SetIterator(BetNumber numb, float value)
        {
            switch (numb)
            {
                case BetNumber._Tmin:
                {
                    Tmin = value;
                    break;
                }
                case BetNumber._Tmax:
                {
                    Tmax = value;
                    break;
                }
                default:
                    return ;
            }
        }

        public object GetData(BetNumber numb)
        {
            switch (numb)
            {
                case BetNumber._Tmin:
                {
                    return Tmino;
                }
                case BetNumber._Tmax:
                {
                    return Tmaxo;
                }
                default:
                return "error GetData";
            }
        }

        public string ToMystring(BetNumber numb)
        {
            switch (numb)
            {
                case BetNumber._Tmax:
                    return "ИTБ(" + Coeff + ")";
                case BetNumber._Tmin:
                    return "ИТМ(" + Coeff + ")";
                default:
                return "none";
            }
        }

        //Проверяет валидность ставки
        public bool Isvalid()
        {
            if (TeamName == null)
                return false;

            if (Tmin==0 && Tmax==0)
                return false;

            return true;
        }

        public override string ToString()
        {
            return String.Format("{0}({1}) {2} - {3}", TeamName, Coeff, Tmin, Tmax);
        }
    }
}
