using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ABShared;

namespace ABClient
{
    class JsObject
    {
        private readonly Dictionary<string, BookmakerType> _allBook = new Dictionary<string, BookmakerType>()
        {
            { "fonbet", BookmakerType.Fonbet },
            { "olimp", BookmakerType.Olimp },
            { "zenit", BookmakerType.Zenit },
            { "marafon", BookmakerType.Marafon },
            { "parimatch", BookmakerType.Parimatch }

        };


        public JsObject()
        {
            currentcoeff = "0";
            maxbet = Int32.MaxValue;
        }
        
        public event Action<int> MaxBetChanged;

        public int maxbet { get; set; }

        [JsMethod]
        public void setmaxbet(object value)
        {
            try
            {
                var tmp= Int32.Parse(value.ToString().Split('.').First().Split(',').First());                
                maxbet = tmp;
                MaxBetChanged?.Invoke(maxbet);
            }
            catch(Exception ex)
            {
                 Debug.WriteLine($"JsObject.setmaxbet: Не смог преобразовать максимальную ставку. {ex.Message}");   
            }
        }

        /// <summary>
        /// True - если это значение Баланса.
        /// False - если значение максимальной ставки
        /// </summary>
        [JsMethod]
        public bool isbalance { get; set; }

        //Уведомляет если сменеились коээфиценты
        public event Action<double> CoeffChanged;

        //Отвечает за информацию о текущем коэфиценте
        private string CurrentCoeffValue;

        [JsMethod]
        public string currentcoeffLast { get; set; }

        [JsMethod]
        public string currentcoeff
        {
            get
            {
                return CurrentCoeffValue;
            }
            set
            {
                currentcoeffLast = value;
                if (CoeffChanged != null)
                {
                    double rez;
                    var o = double.TryParse(value.Replace(".", ","), out rez);
                    if (o == false)
                    {
                        CurrentCoeffValue = "0";
                        CoeffChanged(0);
                    }
                    else
                    {
                        CurrentCoeffValue = rez.ToString();
                        CoeffChanged(rez);
                    }
                }
            }
        }

        public event Action<BookmakerType, bool> LoginChange;

        [JsMethod]
        public bool loginstatus { get; set; }

        //Содержит название сайта, на котором проверяеться авторизация 
        private string loginedValue;

        [JsMethod]
        public string logined
        {
            get
            {
                return loginedValue;
            }

            set
            {
                if (value != "")
                {
                    loginedValue = value;
                    LoginChange?.Invoke(_allBook[value], loginstatus);
                }
            }
        }



        //уведомляет о том, что процесс ставки завершен
        public event Action<bool> Stop;

        [JsMethod]
        public void stoped()
        {
            Stop?.Invoke(loginstatus);
        }

    }


    /// <summary>
    /// Помечаем методы, который вызываються на стороне js в браузере
    /// </summary>
    internal class JsMethodAttribute : Attribute
    {
    }
}
