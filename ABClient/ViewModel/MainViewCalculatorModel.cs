using ABClient.Controllers;
using ABShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ABClient.ViewModel
{
    public class MainViewCalculatorModel:DependencyObject
    {

        #region DependencyProperty
        public static readonly DependencyProperty IsLockedCoeffProperty =
            DependencyProperty.Register("IsLockedCoeff", typeof(bool), typeof(MainViewCalculatorModel), new PropertyMetadata(false));
        
        public static readonly DependencyProperty IsLockedBetOneProperty =
            DependencyProperty.Register("IsLockedBetOne", typeof(bool), typeof(MainViewCalculatorModel), new PropertyMetadata(false));
                
        public static readonly DependencyProperty IslockedBetTwoProperty =
            DependencyProperty.Register("IslockedBetTwo", typeof(bool), typeof(MainViewCalculatorModel), new PropertyMetadata(false));
        
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(CalculatorData), typeof(MainViewCalculatorModel), new PropertyMetadata(new CalculatorData() { Staf = 10000, coeff1 = 1, coeff2 = 2 }, DataChanged));

        private static void DataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = d as MainViewCalculatorModel;

            var old = e.OldValue as CalculatorData;
            var newD = e.NewValue as CalculatorData;
            if (newD == null)
                return;

           vm.ChangedBetOne?.Invoke(newD.bet1);
           vm.ChangedBetTwo?.Invoke(newD.bet2);

        }
        #endregion

        /**
         * Расчет вилки если знаем общий банк и 1 коэф
         * Банк-Банк/коэф
         * 
         * Если знаем размер коэф и ставку на 1 плечо
         * ставка2 = (коэф1 * ставку1)/ коэф2
         * */
        public event Action<int> ChangedBetOne;
        public event Action<int> ChangedBetTwo;

        //Оба коэф залочены?
        public bool IsLockedCoeff
        {
            get { return (bool)GetValue(IsLockedCoeffProperty); }
            set { SetValue(IsLockedCoeffProperty, value); }
        }      
        //Залочен первый?
        public bool IsLockedBetOne
        {
            get { return (bool)GetValue(IsLockedBetOneProperty); }
            set { SetValue(IsLockedBetOneProperty, value); }
        }
        //Залочен второй?
        public bool IslockedBetTwo
        {
            get { return (bool)GetValue(IslockedBetTwoProperty); }
            set { SetValue(IslockedBetTwoProperty, value); }
        }

        public MainViewCalculatorModel()
        {
            
        }
        
        public CalculatorData Data
        {
            get { return (CalculatorData)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public void SetFork(Fork fork,int maxBett)
        {
            var coeff1 = Convert.ToDouble(fork.Cof1);
            var coeff2 = Convert.ToDouble(fork.Cof2);
            var bet = maxBett;           
            Data = Calculator.CalculatorBet(coeff1,coeff2 ,bet);
        }

        public bool UpdateBetOneFlag { get; set; } = false;
        public bool UpdateBetTwoFlag { get; set; } = false;
        public int LastMaxBetOne { get; set; } = Int32.MaxValue;
        public int LastMaxBetTwo { get; set; } = Int32.MaxValue;

#region Ставки

        //Если редактируем общую Ставку
        public void EditStaf(object sender, KeyEventArgs ev)
        {
            /**
             * Возможные варианты
             * 1) Оба залочены
             * 2) Один из 2х залочен
             * 3) все разлочены
             * */
            var rez = sender as TextBox;
            if (rez == null)
                return;
            if (!IsDigit(ev.Key))
                return;
            try
            {
                var bet = Convert.ToInt32(rez.Text);
                UpdateBetTwoFlag = false;
                UpdateBetOneFlag = false;

                //залочен первый но разлочен второй
                if (IsLockedBetOne && !IslockedBetTwo)
                {//сумма второй ставки будет: банк - залоченная ставка                    
                    var dt = Data.Clone();
                    dt.bet2 = bet - dt.bet1;
                    dt.Staf = bet;
                    dt = Calculator.ComputeProfit(dt);
                    Data = dt;
                    rez.CaretIndex = rez.Text.Length;
                }

                //залочен Второй но разлочен первый
                else if (IslockedBetTwo && !IsLockedBetOne)
                {//сумма второй ставки будет: банк - залоченная ставка
                    var dt = Data.Clone();
                    dt.bet1 = bet - dt.bet2;
                    dt.Staf = bet;
                    dt = Calculator.ComputeProfit(dt);
                    Data = dt;
                    rez.CaretIndex = rez.Text.Length;
                }
                //залочены оба
                else if(IsLockedBetOne && IslockedBetTwo)
                {
                    rez.Text = Data.Staf.ToString();
                }
                //Все разлочены считаем по стандартной схеме
                else
                {                    
                    Data = Calculator.CalculatorBet(Data.coeff1, Data.coeff2, bet);                    
                }
                rez.CaretIndex = rez.Text.Length;
            }
            catch
            {
                Console.WriteLine("Ошибка конвертации Staff");
                rez.Text = "0";
            }
        }

        //редактируем ставку на первый лот
        public void EditBet1(object sender, KeyEventArgs ev)
        {
            var rez = sender as TextBox;
            if (rez == null)
                return;
            if (!IsDigit(ev.Key))
                return;

           /* if (IsLockedBetOne)
            {
                rez.Text = Data.bet1.ToString();
                return;
            }*/

            try
            {               
                var bet = Convert.ToInt32(rez.Text);
                var dt = Data.Clone();
                if(!IslockedBetTwo)
                {

                    if(UpdateBetOneFlag && UpdateBetTwoFlag)
                    {
                        dt.bet1 = bet;
                        var temp = (dt.bet1 * dt.coeff1) / dt.coeff2; //считаем какая должна быть ставка
                        if(temp>dt.bet2)
                        {
                            dt.Staf = dt.bet1 + dt.bet2;//ничего не поделаем, елси она больше максимальной
                        }
                        else
                        {
                            dt.bet2 =(int) temp;
                            dt.Staf = dt.bet1 + dt.bet2;
                        }

                    }
                    else
                    {
                        dt.bet1 = bet;
                        dt.bet2 =(int)( (dt.coeff1 * dt.bet1) / (dt.coeff2));
                        dt.Staf = dt.bet1 + dt.bet2;
                    }
                    
                }
                else
                {
                    dt.bet1 = bet;
                    dt.Staf = dt.bet1 + dt.bet2;
                }
                dt = Calculator.ComputeProfit(dt);
                Data = dt;
                rez.CaretIndex = rez.Text.Length;                
            }
            catch
            {
                Console.WriteLine("Ошибка конвертации bet1");
                rez.Text = "0";
            }
        }

      
        //редактируем ставку на второй лот
        public void EditBet2(object sender,KeyEventArgs ev)
        {
            var rez = sender as TextBox;
            if (rez == null)
                return;
            if (!IsDigit(ev.Key))
                return;

            try
            {
                var bet = Convert.ToInt32(rez.Text);
                var dt = Data.Clone();
                if (!IsLockedBetOne)
                {   
                    if(UpdateBetOneFlag && UpdateBetTwoFlag)
                    {
                        dt.bet2 = bet;
                        var temp = (dt.bet2 * dt.coeff2) / dt.coeff1;
                        if (temp > dt.bet1)
                        {
                            dt.Staf = dt.bet1 + dt.bet2;
                        }
                        else
                        {
                            dt.bet1 = (int)temp;
                            dt.Staf = dt.bet1 + dt.bet2;
                        }
                    }
                    else
                    {
                        dt.bet2 = bet;
                        dt.bet1 = (int)((dt.bet2 * dt.coeff2) / dt.coeff1);
                        dt.Staf = dt.bet1 + dt.bet2;
                    }            
                                                   
                }
                else
                {
                    dt.bet2 = bet;
                    dt.Staf = dt.bet1 + dt.bet2;
                }
                dt = Calculator.ComputeProfit(dt);
                Data = dt;
                rez.CaretIndex = rez.Text.Length;
                ChangedBetTwo?.Invoke(Data.bet2);

            }
            catch
            {
                Console.WriteLine("Ошибка конвертации bet2");
                rez.Text = "0";
            }
        }

        #endregion

#region Коэф
        //редактируем первый коэф
        public void EditCoeff1(object sender,KeyEventArgs ev)
        {
            var rez = sender as TextBox;
            if (rez == null)
                return;
            if (!IsDigit(ev.Key) || rez.Text=="" || rez.Text.Last() == '.' || rez.Text.Last() == ',')
                return;            
            rez.Text = rez.Text.Replace('.', ',');

            // проверям. врдуг пользователь вводи дробное число
            if (rez.Text.Length >= 2)
            {
                var last = rez.Text.Substring(rez.Text.Length - 2);
                if (last == ",0") // если человек хочет ввести 0,01 то нужно дать ему эту возможность
                {
                    rez.CaretIndex = rez.Text.Length;
                    return;
                }                    
            }
            try
            {
               
                if (rez.Text.Last() == ',')
                    rez.Text += "0";
                var coeff = Convert.ToDouble(rez.Text);

                if (coeff == Data.coeff1)
                    return;

                if (IslockedBetTwo && !IsLockedBetOne)  //если вторая ставка залоченна, то Банк и первую ставку
                {
                    var dt = Data.Clone();
                    dt.coeff1 = coeff;
                    dt.bet1 = (int)(dt.bet2 * dt.coeff2 / coeff);
                    dt.Staf = dt.bet1 + dt.bet2;
                    dt = Calculator.ComputeProfit(dt);
                    Data = dt;
               
                }
                else if(IsLockedBetOne && IslockedBetTwo) // если все залоченно, то только пересчитываем потери
                {
                    var dt = Data.Clone();
                    dt.coeff1 = coeff;
                    dt = Calculator.ComputeProfit(dt);
                    Data = dt;
                }
                else if(IsLockedCoeff && !IslockedBetTwo || IsLockedBetOne && !IslockedBetTwo)
                {
                    var dt = Data.Clone();
                    dt.coeff1 = coeff;
                    dt.bet1 = Data.bet1;
                    dt.bet2 = (int)(dt.bet1 * dt.coeff1 / dt.coeff2);
                    dt.Staf = dt.bet1 + dt.bet2;

                    dt = Calculator.ComputeProfit(dt);
                    Data = dt;
                }
                else //считаем ставки исходя из максимального Банка
                {
                    var dt = Data.Clone();
                    dt.coeff1 = coeff;
                    Data = Calculator.CalculatorBet(coeff,dt.coeff2,dt.Staf);
                }

                rez.CaretIndex = rez.Text.Length;

            }
            catch
            {
                Console.WriteLine("Ошибка конвертации EditCoeff1");
                rez.Text = "0";
            }
        }

        public void EditCoeff2(object sender, KeyEventArgs ev)
        {
            var rez = sender as TextBox;
            if (rez == null)
                return;
            if (!IsDigit(ev.Key) || rez.Text == "" || rez.Text.Last() == '.' || rez.Text.Last() == ',')
                return;           

            rez.Text = rez.Text.Replace('.', ',');

            if (rez.Text.Length >= 2)
            {
                var last = rez.Text.Substring(rez.Text.Length - 2);
                if (last == ",0")
                {
                    rez.CaretIndex = rez.Text.Length;
                    return;
                }

            }

            if (rez.Text.Last() == ',')
                rez.Text += "0";
            double coeff;
            try
            {
                coeff = Convert.ToDouble(rez.Text);
            }
            catch
            {
                return;
            }

            if (coeff == Data.coeff1)
                return;

            if (IsLockedBetOne && !IslockedBetTwo)//если вторая ставка залоченна, то Банк и первую ставку
            {
                var dt = Data.Clone();
                dt.coeff2 = coeff;
                dt.bet2 = (int)(dt.bet1 * dt.coeff1 / coeff);
                dt.Staf = dt.bet1 + dt.bet2;
                dt = Calculator.ComputeProfit(dt);
                Data = dt;
            }
            if(IslockedBetTwo && IsLockedBetOne) // если все залоченно, то только пересчитываем потери
            {
                var dt = Data.Clone();
                dt.coeff2 = coeff;
                dt = Calculator.ComputeProfit(dt);
                Data = dt;
            }
            else if (IsLockedCoeff && !IsLockedBetOne || IslockedBetTwo && !IsLockedBetOne)
            {
                var dt = Data.Clone();
                dt.coeff2 = coeff;
                dt.bet2 = Data.bet2;
                dt.bet1 = (int)(dt.bet2 * dt.coeff2 / dt.coeff1);
                dt.Staf = dt.bet1 + dt.bet2;
                dt = Calculator.ComputeProfit(dt);
                Data = dt;
            }

            else //считаем ставки исходя из максимального Банка
            {
                var dt = Data.Clone();
                dt.coeff2 = coeff;
                dt = Calculator.CalculatorBet(coeff, dt.coeff1, dt.Staf);
                dt.Reverse();
                Data = dt;
            }
            rez.CaretIndex = rez.Text.Length;
        }

#endregion

        //Сбрасывает флаги
        public void ResetFlags()
        {
            IsLockedBetOne = false;
            IslockedBetTwo = false;
            IsLockedCoeff = false;
            UpdateBetOneFlag = false;
            UpdateBetTwoFlag = false;
            LastMaxBetOne = Int32.MaxValue;
            LastMaxBetTwo = Int32.MaxValue;
          
        }

        static List<Key> keylist = new List<Key>()
        {
            Key.D0,
            Key.D1,
            Key.D2,
            Key.D3,
            Key.D4,
            Key.D5,
            Key.D6,
            Key.D7,
            Key.D8,
            Key.D9,
            Key.NumPad0,
            Key.NumPad1,
            Key.NumPad2,
            Key.NumPad3,
            Key.NumPad4,
            Key.NumPad5,
            Key.NumPad6,
            Key.NumPad7,
            Key.NumPad8,
            Key.NumPad9,
            Key.Delete,
            Key.Back
        };

        private static  bool IsDigit(Key key)
        {
           return keylist.Contains(key);            
        }

        public void UpdateCoeff1(double coeff1, TextBox text)
        {
            if (IsLockedBetOne || IsLockedCoeff)
                return;
            if (coeff1 == Data.coeff1)
                return;
            text.Text = coeff1.ToString();
            EditCoeff1(text, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromDependencyObject(text), 0, Key.NumPad0));
        }

        public void UpdateCoeff2(double coeff2,TextBox text)
        {
            if (IslockedBetTwo || IsLockedCoeff)
                return;
            if (coeff2 == Data.coeff2)
                return;
            text.Text = coeff2.ToString();
            EditCoeff2(text, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromDependencyObject(text), 0, Key.NumPad0));
        }


        public void ChangeIsLockedCoeff()
        {
            if (IsLockedCoeff)
                IsLockedCoeff = false;
            else
                IsLockedCoeff = true;
        }

        public void ChangeIsLockedBetOne()
        {
            IsLockedBetOne = !IsLockedBetOne;
        }

        public void ChangeIslockedBetTwo()
        {
            IslockedBetTwo = !IslockedBetTwo;
        }
    }
}
