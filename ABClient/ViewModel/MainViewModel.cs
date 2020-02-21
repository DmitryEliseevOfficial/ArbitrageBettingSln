using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ABClient.Controllers;
using ABClient.Protocol;
using ABClient.Views;
using ABShared;

namespace ABClient.ViewModel
{
    internal sealed class MainViewModel:INotifyPropertyChanged
    {
        private readonly Thread _thForksUpdate;

        private MediaPlayer _mediaPlayer;

        private readonly  DClient _client;

        
        public int GridColumn
        {
            get { return _gridColumn; }
            set
            {
                if (_gridColumn == value) return;
                _gridColumn = value; PropChanged();
            }
        }
        private int _gridColumn = 3;
        
        public int ColumnSpan
        {
            get { return _columnSpan; }
            set
            {
                if(_columnSpan==value) return;
                _columnSpan = value; PropChanged();
            }
        }
        private int _columnSpan;

        //состояние ListView true-расширена false - сжата
        public bool StateListView
        {
            get { return _stateListView; }
            set
            {
                if(_stateListView==value) return;
                _stateListView = value; PropChanged();
            }
        }
        private bool _stateListView;
        
        public List<Fork> Forks
        {
            get { return _forks; }
            set
            {
                if(_forks==value) return;
                _forks = value; PropChanged();
            }
        }
        private List<Fork> _forks;


        public ReallyCommand StretchCommand { get; set; }


        public BookkeeperViewModel BookkeeperViewModel { get; set; } = new BookkeeperViewModel();


        public MainViewCalculatorModel MainViewCalculatorModel { get; set; } = new MainViewCalculatorModel();

 
        public Fork SelectedFork
        {
            get { return _selectedFork; }
            set
            {
                if(_selectedFork!=null &&_selectedFork.Equals(value)) return;
                _selectedFork = value; PropChanged();
            }
        }
        private Fork _selectedFork;
        

        public SolidColorBrush TitleColor
        {
            get { return _titleColor; }
            set
            {
                if(_titleColor==value) return;
                _titleColor = value; PropChanged();
            }
        }
        private SolidColorBrush _titleColor = new SolidColorBrush(Colors.Green);
        
        public MainViewModel(DClient client)
        {
            _client = client;
            // client.GetData += Client_GetData;
            StretchCommand = new ReallyCommand(StretchingExecute);

            // Инстализируем отдельный поток для обновления вилок
            //_thForksUpdate = new Thread(ForksUpdate) {IsBackground = true};
            //_thForksUpdate.Start();
        }


        public bool IsFork
        {
            get { return _isFork; }
            set
            {
                if(_isFork==value) return;
                _isFork = value; PropChanged();
            }
        }
        private bool _isFork;


        public void SetForks(List<Fork> data)
        {
            //if(Forks==null)
            //    Forks=new List<Fork>();
            //var newForks = Forks.ToList();
        
            //foreach (Fork fork in Forks)
            //{
            //    if (!data.Contains(fork))
            //    {
            //        if (fork.DeathTime > 3)
            //            newForks.Remove(fork);
            //        else
            //            fork.DeathTime++;
            //    }
            //    else
            //    {
            //        fork.DeathTime = 0;
            //    }
                
            //}


            //foreach (Fork fork in data)
            //{
            //    var index = newForks.IndexOf(fork);
            //    if (index == -1)
            //    {
            //        newForks.Add(fork);
            //    }
            //    else
            //    {
            //        newForks[index] = fork;
            //    }
            //}

            //var ind = newForks.IndexOf(SelectedFork);
            //if (ind==-1)
            //{
            //    IsFork = false;
            //}
            //else
            //{
            //    SelectedFork = newForks[ind];
            //    IsFork = true;
            //}

            //UpdateLifeTime();

            //Forks=newForks.OrderBy(x=>x.Profit).Reverse().ToList();

            Forks = data;
        }

        public void UpdateLifeTime()
        {
            //if (Forks != null)
            //{
            //    foreach (Fork fork in Forks)
            //    {
            //        fork.LifeTime++;
            //    }
            //}
        }


        public bool ForksUpdateStart { get; set; } = true;

  
        private void ForksUpdate()
        {
            while (true)
            {

                if (ForksUpdateStart)
                {
                    try
                    {
                        _client.ParseData();
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(ex.Message);
                        Thread.Sleep(5000);
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        _client.Dispose();
                        break;
                    }

                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private void Client_GetData(List<Fork> obj)
        {
            //forks = obj;
            try
            {
                //this.Dispatcher.Invoke(delegate ()
                //{
                //    this.Title = forks.Count().ToString();
                //});
                //Task.Factory.StartNew(ShowData);
               SetForks(obj);
            }
            catch
            {

            }
        }

        //Растягивает ListBox с вилками
        private void StretchingExecute()
        {
            if (!_stateListView)
            {
                GridColumn = 1;
                ColumnSpan = 3;
                StateListView = true;
            }
            else
            {
                GridColumn = 3;
                ColumnSpan = 1;
                StateListView = false;
            }

        }



        //-------------------------------------------
        //-------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        private void PropChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
