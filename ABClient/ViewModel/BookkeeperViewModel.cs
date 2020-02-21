using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;


namespace ABClient.ViewModel
{
    internal class BookkeeperViewModel : INotifyPropertyChanged
    {

#region Поля
        public event PropertyChangedEventHandler PropertyChanged;

        private void PropChanged([CallerMemberName] string name="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

 
        public int Sum
        {
            get {
                return _sum;
            }
            set
            {
                if (_sum == value) return;
                _sum = value;
                PropChanged();
            }
        }
        private int _sum;


        public int Current
        {
            get { return _current; }
            set
            {
                if (_current == value) return;
                _current = value; PropChanged();
            }
        }
        private int _current;

        public Visibility SumVisibility
        {
            get
            {
                return _sumVisibility;
            }
            set
            {
                if(_sumVisibility==value) return;
                _sumVisibility = value; PropChanged();
            }
        }
        private Visibility _sumVisibility = Visibility.Visible;


        public Visibility CurrentVisibility
        {
            get
            {
                return _currentVisibility;
            }
            set
            {
                if(_currentVisibility==value) return;
                _currentVisibility = value; PropChanged();
            }
        }
        private Visibility _currentVisibility = Visibility.Visible;
        

        public Visibility MainVisibility
        {
            get
            {
                return _mainVisibility;
            }
            set
            {
                if(_mainVisibility==value) return;
                _mainVisibility = value; PropChanged();
            }
        }
        private Visibility _mainVisibility = Visibility.Hidden;

        public bool IsSumShow
        {
            get
            {
                return _isSumShow;
            }
            set
            {
                if(_isSumShow==value) return;
                _isSumShow = value;PropChanged();
            }
        }
        private bool _isSumShow = true;

        /// <summary>
        /// Команда сохранения итогового результата в файл
        /// </summary>
        public ReallyCommand SaveCommand { get; set; }

        /// <summary>
        /// Команда сложения текущего числа с суммой
        /// </summary>
        public ReallyCommand SumCommand { get; set; }

        /// <summary>
        /// Команда вычитания текущего числа с суммой
        /// </summary>
        public ReallyCommand DivCommand { get; set; }

        /// <summary>
        /// Команда для отображения суммы
        /// </summary>
        public ReallyCommand ShowSumCommand { get; set; }

        /// <summary>
        /// Команда для отображения блока с текущим письмом
        /// </summary>
        public ReallyCommand ShowCurrentCommand { get; set; }

        /// <summary>
        /// Команда отображения панели с калькулятором
        /// </summary>
        public ReallyCommand ShowPanelCommand { get; set; }


        /// <summary>
        /// Стиль для кнопки с логинами
        /// </summary>
        public Style ShowLoginStyle
        {
            get { return _showLoginStyle; }
            set
            {
                if (_showLoginStyle == value) return;
                _showLoginStyle = value; PropChanged();
            }
        }
        private Style _showLoginStyle;

        /// <summary>
        /// Стиль для кнопки калькулятора
        /// </summary>
        public Style ShowBookStyle
        {
            get { return _showBookStyle; }
            set
            {
                if (_showBookStyle == value) return;
                _showBookStyle = value; PropChanged();
            }
        }
        private Style _showBookStyle;

#endregion
      
        public BookkeeperViewModel()
        {
            ShowCurrentCommand = new ReallyCommand(ShowCurrentExeute);
            ShowSumCommand = new ReallyCommand(ShowSumExecute);
            DivCommand = new ReallyCommand(DivExecute);
            SumCommand = new ReallyCommand(SumExecute);
            SaveCommand = new ReallyCommand(SaveExecute);
            ShowPanelCommand = new ReallyCommand(ShowPanel);

            ShowLoginStyle= (Style)Application.Current.Resources["RoundCornerBlackInverse"];
            ShowBookStyle= (Style)Application.Current.Resources["RoundCornerBlack"];
        }

   

        private void SaveExecute()
        {
            DateTime dt = DateTime.Now;
            List<string> rez = new List<string>
            {
                $"-------{dt:dd-MM-yyyy HH:mm}-------",
                Sum.ToString(),
                "-------------------------\n\n"
            };


            string path = Path.Combine(Environment.CurrentDirectory, "FinanceLog");
            if(!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }

            string filename = Path.Combine(path, dt.ToString("dd_MM_yyyy_HH_mm") + ".txt");
            File.WriteAllLines(filename, rez);

            Sum = 0;
            Current = 0;
            MessageBox.Show("Данные успешно сохранены","Сообщение");
        }


        private void SumExecute()
        {
            Sum += Current;
            Current = 0;
        }

        private void DivExecute()
        {
            Sum -= Current;
            Current = 0;
        }


        private void ShowPanel()
        {
            //HACK: Не самый шикарный вариант.             
            if(MainVisibility==Visibility.Visible)
            {
                ShowLoginStyle = (Style)Application.Current.Resources["RoundCornerBlackInverse"];
                ShowBookStyle = (Style)Application.Current.Resources["RoundCornerBlack"];
                MainVisibility = Visibility.Collapsed;
            }
            else
            {
                ShowLoginStyle = (Style)Application.Current.Resources["RoundCornerBlack"];
                ShowBookStyle = (Style)Application.Current.Resources["RoundCornerBlackInverse"];
                MainVisibility = Visibility.Visible;
            }
        }

        private void ShowSumExecute()
        {            
            if (SumVisibility==Visibility.Visible)
            {
                SumVisibility = Visibility.Hidden;
                IsSumShow = false;
            }
            else
            {
                IsSumShow = true;
                SumVisibility = Visibility.Visible;
            }
        }


        private void ShowCurrentExeute()
        {
            if (CurrentVisibility == Visibility.Visible)
            {
                CurrentVisibility = Visibility.Hidden;
            }
            else
            {
                CurrentVisibility = Visibility.Visible;
            }
        }
    }
}
