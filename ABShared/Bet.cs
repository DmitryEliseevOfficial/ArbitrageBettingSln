using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ABShared
{  
    [Serializable]
    public class Bet
    {

        public DateTime Created { get; private set; }
        public Bet()
        {
            Created=DateTime.Now;
        }

        public string Site { get; set; }
        public string Time { get; set; }

        public SportType SportType { get; set; }

        public string Groupe { get; set; }


        public string Name { get; set; }

        public string Team1 { get; set; }
        public string Team2 { get; set; }

        public int Team1Id { get; set; } = -1;
        public int Team2Id { get; set; } = -1;


        //1 x 2 1X 12 X2 ф1 Ф2 Б м
        public float _1 { get; set; }
        public object _1o { get; set; }

        public float _X { get; set; }
        public object _Xo { get; set; }

        public float _2 { get; set; }
        public object _2o { get; set; }

        public float _1X { get; set; }
        public object _1Xo { get; set; }


        public float _12 { get; set; }
        public object _12o { get; set; }

        public float _X2 { get; set; }
        public object _X2o { get; set; }

        public float _F1 { get; set; }
        public float _F1_Cof { get; set; }
        public object _F1o { get; set; }

        public float _F2 { get; set; }
        public float _F2_Cof { get; set; } 
        public object _F2o { get; set; }
         
        public float _Tmin { get; set; } 
        public object _Tmino { get; set; }

        public float _Tmax { get; set; }
        public object _Tmaxo { get; set; }

        public float _Total_Cof { get; set; }

        public Dictionary<SportTimePart, Bet> Parts { get; set; } = new Dictionary<SportTimePart, Bet>();
        public List<GameBet> Games { get; set; } = new List<GameBet>();

        public string URL { get; set; }
        public BookmakerType Bookmaker { get; set; }

        //Список ИТБ и ИТМ
        public List<IBTBet> IBTList { get; } = new List<IBTBet>();

        public List<Stake> Foras { get; } = new List<Stake>();

        public List<Stake> Totals { get;  } = new List<Stake>();

        public List<Stake> ITotals { get; } = new List<Stake>();

        public float this[int x]
        {
            get
            {
                return GetIteration(x);
            }

            set
            {
                SetIteration(x,value);
            }
        }

        public override string ToString()
        {
            return 
                $"{Site} {SportType}    | {Team1} - {Team2} | {_1} - {_X} - {_2} | {_1X} - {_12} - {_X2} | Ф1({_F1_Cof}) {_F1} - Ф2({_F2_Cof}) {_F2} | Тотал({_Total_Cof}) {_Tmin} - {_Tmax}";
        }


        private float GetIteration(int x)
        {
            switch (x)
            {
                case 0:
                    return _1;
                case 1:
                    return _X;
                case 2:
                    return _2;
                case 3:
                    return _1X;
                case 4:
                    return _12;
                case 5:
                    return _X2;
                case 6:
                    return _F1;
                case 7:
                    return _F2;
                case 8:
                    return _Tmin;
                case 9:
                    return _Tmax;
                default:
                    throw new ArgumentException("index not be <0 and >9. Index=" + x);
            }
            
        }

        private void SetIteration(int x,float value)
        {
            switch (x)
            {
                case 0:
                    _1=value;
                    break;
                case 1:
                    _X=value;
                    break;
                case 2:
                    _2=value;
                    break;
                case 3:
                    _1X=value;
                    break;
                case 4:
                    _12=value;
                    break;
                case 5:
                    _X2=value;
                    break;
                case 6:
                    _F1=value;
                    break;
                case 7:
                    _F2=value;
                    break;
                case 8:
                    _Tmin=value;
                    break;
                case 9:
                    _Tmax=value;
                    break;
                default:
                throw new ArgumentException("index not be <0 and >9. Index=" + x);
            }
        }

        public string ToMyString(BetNumber numb)
        {
            switch (numb)
            {
                case BetNumber._1:
                    return "1";
                case BetNumber._X:
                    return "X";
                case BetNumber._2:
                    return "2";
                case BetNumber._1X:
                    return "1X";
                case BetNumber._12:
                     return "12";
                case BetNumber._X2:
                    return "X2";
                case BetNumber._F1:
                    return "Ф1("+_F1_Cof+")";
                case BetNumber._F2:
                    return "Ф2(" + _F2_Cof + ")";
                case BetNumber._Tmax:
                    return "Tб("+_Total_Cof+")";
                case BetNumber._Tmin:
                    return "Тм(" + _Total_Cof + ")";
                default:
                 return "none";                
            }
        }

        public float this[BetNumber x]
        {
            get { return this[(int)x]; }
            set { this[(int)x] = value; }
        }

        //Отдает данные для js скрипта
        public object GetData(BetNumber numb)
        {
            switch (numb)
            {
                case BetNumber._1:
                return _1o;
                case BetNumber._X:
                return _Xo;
                case BetNumber._2:
                return _2o;
                case BetNumber._1X:
                return _1Xo;
                case BetNumber._12:
                return _12o;
                case BetNumber._X2:
                return _X2o;
                case BetNumber._F1:
                return _F1o;
                case BetNumber._F2:
                return _F2o;
                case BetNumber._Tmax:
                return _Tmaxo;
                case BetNumber._Tmin:
                return _Tmino;
                default:
                return "none";
            }
        }

        public void SetData(BetNumber numb,object value)
        {
            switch (numb)
            {
                case BetNumber._1:
                {
                    _1o=value;
                    break;
                }
                
                case BetNumber._X:
                {
                    _Xo = value;
                    break;
                }
                case BetNumber._2:
                {
                    _2o = value;
                    break;
                }
                case BetNumber._1X:
                {
                    _1Xo = value;
                    break;
                }
                case BetNumber._12:
                {
                    _12o = value;
                    break;
                }
                case BetNumber._X2:
                {
                    _X2o = value;
                    break;
                }
                case BetNumber._F1:
                {
                    _F1o = value;
                    break;
                }
                case BetNumber._F2:
                {
                    _F2o = value;
                    break;
                }
                case BetNumber._Tmax:
                {
                    _Tmaxo = value;
                    break;
                }
                case BetNumber._Tmin:
                {
                    _Tmino = value;
                    break;
                }
                default:
                throw new ArgumentException("Bet: Betnumber not found");
            }
        }


        //Делает копию объекта без ставок
        public Bet ShortCopy()
        {
            Bet bet = new Bet
            {
                Bookmaker = Bookmaker,
                Team1 = Team1,
                Team2 = Team2,
                Site = Site,
                URL = URL,
                Time = Time,
                SportType = SportType,
                Groupe = Groupe,
                Name = Name
            };
            return bet;
        }

        public void Update()
        {
            Created=DateTime.Now;
        }
    }
}
