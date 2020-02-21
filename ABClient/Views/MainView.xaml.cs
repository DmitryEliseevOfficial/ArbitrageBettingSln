using ABClient.Data;
using ABClient.Protocol;
using ABClient.Target;
using ABClient.ViewModel;
using ABShared;
using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ABClient.Views
{
    /// <summary>
    /// Логика взаимодействия для MainView.xaml
    /// </summary>   
    public partial class MainView : Window
    {
        private List<Bookmaker> _bookmakers;

        private readonly Dictionary<BookmakerType,ITargerSite> _managers = new Dictionary<BookmakerType,ITargerSite>();

        private DClient _client;

        private readonly Thread _thForksUpdate;
        private MainViewCalculatorModel vm;
        private MainViewModel _fmd;

        private JsObject job1;
        private JsObject job2;


        private Fork selectedFork;
        private Settings _mainSetting;

        private List<Fork> forks = new List<Fork>();

        private MediaPlayer _player;


        public MainView(DClient client,Settings mainSetting)
        {
            client.GetData += Client_GetData;

            _client = client;
            _mainSetting = mainSetting;

            LoadData();

            InitializeChrome();

            InitializeComponent();

            CreatePlayer();

            if (mainSetting.IsDoubleButton)
            {
                BtnDouble.Visibility = Visibility.Visible;
            }

            job1 = new JsObject();
            job2 = new JsObject();

            BrowserTab1.Browser.RegisterJsObject("jsobject", job1);
            BrowserTab2.Browser.RegisterJsObject("jsobject", job2);

            job1.CoeffChanged += Job1_CoeffChanged;
            job2.CoeffChanged += Job2_CoeffChanged;
            job1.LoginChange += LoginChange;
            job2.LoginChange += LoginChange;
            job1.Stop += Job_Stop;
            job2.Stop += Job_Stop;
            job1.MaxBetChanged += Job1_MaxBetChanged;
            job2.MaxBetChanged += Job2_MaxBetChanged;

            //Показываем список активных БК       
            Test.ItemsSource = _bookmakers.Where(x => x.IsShow).ToList();

            //Инициализируем нужных менеджеров
            _managers.Add(BookmakerType.Zenit, new ZenitManager(BrowserTab1.Browser));
            _managers.Add(BookmakerType.Fonbet, new FonbetManager(BrowserTab1.Browser));
            _managers.Add(BookmakerType.Olimp, new OlimpManager(BrowserTab2.Browser));
            _managers.Add(BookmakerType.Marafon, new MarafonManager(BrowserTab2.Browser));
            _managers.Add(BookmakerType.Parimatch, new PariMatchManager(BrowserTab2.Browser));


            //Инстализируем отдельный поток для обновления вилок
            _thForksUpdate = new Thread(ForksUpdate);
            _thForksUpdate.IsBackground = true;
            _thForksUpdate.Start();

            LbFilters.ItemsSource = mainSetting.Sports;

            Loaded += MainView_Loaded;
            Closing += MainView_Closing;

            //Задаем настройки внешнего вида
            if (mainSetting.WinSet == null)
                mainSetting.WinSet = new WindowsSettings();            
            _minPercent = mainSetting.WinSet.MinPercent;
            _maxPercent = mainSetting.WinSet.MaxPercent;
            if (_minPercent != 0)
                TxtMinPercent.Text = _minPercent.ToString("#");
            else
                TxtMinPercent.Text = 0.ToString();
            TxtMaxPercent.Text = _maxPercent.ToString("#");

            Height = mainSetting.WinSet.WindowsHeight;
            Width = mainSetting.WinSet.WindowsWidth;
            SpliterOne.Height = new GridLength(mainSetting.WinSet.SpliterOne);
            SpliterTwo.Width = new GridLength(mainSetting.WinSet.SpliterTwo);

            if(mainSetting.WinSet.ColumnsWidth!=null)
            {
                var gridview = Forks.View as GridView;
                if (gridview == null)
                {
                    MessageBox.Show($"Не удалось загрузить настройки столбцов. Будут заданы настройки по умолчанию.");
                    return;
                }
                for (int i=0;i< mainSetting.WinSet.ColumnsWidth.Count;i++)
                {
                    try
                    {
                        gridview.Columns[i].Width = mainSetting.WinSet.ColumnsWidth[i];
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Ошибка задания размеров {nameof(gridview)}. {ex.Message}");
                    }
                   
                }
            }
        }

        private void InitializeChrome()
        {
            BrowserSettings set = new BrowserSettings();

            var cefPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Sdk\Cef");
            CefSettings settings = new CefSettings();
            settings.LocalesDirPath = System.IO.Path.Combine(cefPath, "locales");
            settings.Locale = "ru";
            settings.UserAgent = _mainSetting.UserAgent;
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            settings.CachePath = System.IO.Path.Combine(cefPath, "cache");
            settings.LogFile = System.IO.Path.Combine(cefPath, "Log.txt");
            settings.LogSeverity = CefSharp.LogSeverity.Error;

            if (_mainSetting.IsProxy)
            {
                settings.CefCommandLineArgs.Add("proxy-server", _mainSetting.Proxy);
            }

            if (!Cef.IsInitialized)
                Cef.Initialize(settings);
        }

        private async void Client_GetData(List<Fork> obj)
        {
            forks = obj;
            await Task.Factory.StartNew(ShowData);
        }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            _fmd = new MainViewModel(_client);
            DataContext = _fmd;


            vm = _fmd.MainViewCalculatorModel;


            vm.ChangedBetOne += Vm_ChangedBetOne;
            vm.ChangedBetTwo += Vm_ChangedBetTwo;

            //Подписываемся на событие нажатий клавишей, для последующие обработки их во vm
            TxtStaf.KeyUp += vm.EditStaf;
            TxtBet1.KeyUp += vm.EditBet1;
            Txtbet2.KeyUp += vm.EditBet2;
            TxtCoeff1.KeyUp += vm.EditCoeff1;
            TxtCoeff2.KeyUp += vm.EditCoeff2;


            foreach (Bookmaker book in _bookmakers)
            {
                _managers[book.BkType].SetUrl(book.Url);
            }



            var baner = new BanerView();
            baner.ShowDialog();

            BrowserTab1.zoomLvl = _mainSetting.WinSet.BrZoom1;
            BrowserTab2.zoomLvl = _mainSetting.WinSet.BrZoom2;

        }


        private void MainView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mainSetting.WinSet.BrZoom1 = BrowserTab1.zoomLvl;
            _mainSetting.WinSet.BrZoom2 = BrowserTab2.zoomLvl;
            _mainSetting.WinSet.MaxPercent = _maxPercent;
            _mainSetting.WinSet.MinPercent = _minPercent;
            _mainSetting.WinSet.WindowsHeight = ActualHeight;
            _mainSetting.WinSet.WindowsWidth = ActualWidth;
            _mainSetting.WinSet.SpliterOne = SpliterOne.ActualHeight;
            _mainSetting.WinSet.SpliterTwo = SpliterTwo.ActualWidth;

            _mainSetting.WinSet.ColumnsWidth = new List<double>();

            var gridview = Forks.View as GridView;
            if (gridview == null)
                return;
            foreach (var column in gridview.Columns)
            {
                var t = column.ActualWidth;
                _mainSetting.WinSet.ColumnsWidth.Add(t);
            }

            SettingsManger.SaveSettings(_mainSetting);
        }


        private void Vm_ChangedBetTwo(int obj)
        {
            if (selectedFork != null)
            {
                try
                {
                    _managers[selectedFork.TypeTwo].SetBet(Controllers.Calculator.aRound(obj), selectedFork.Data2);
                }
                catch
                {

                }
            }
        }

        private void Vm_ChangedBetOne(int obj)
        {
            if (selectedFork != null)
            {
                try
                {
                    _managers[selectedFork.TypeOne].SetBet(Controllers.Calculator.aRound(obj), selectedFork.Data1);
                }
                catch
                {

                }
            }
        }

        #region Обрабатываем события Jsobject`а 

        //счетчик срабатывание стопитов и объект блокировки
        private int _bkLoadedCount;
        private readonly object _objectLoadedLock = new object();

        private bool _isNeedAuth;
        private void Job_Stop(bool loginStatus)
        {
            lock(_objectLoadedLock)
            {
                _bkLoadedCount++;

                if (loginStatus==false)
                {
                    _isNeedAuth = true;
                    LblStatus.Dispatcher.Invoke(delegate ()
                    {
                        LblStatus.Content = "Авторизуйтесь";
                        LblStatus.Foreground = new SolidColorBrush(Colors.Red);
                    });
                    
                }
                
                if(_bkLoadedCount==2)
                {  
                    if(!_isNeedAuth)
                    {
                        LblStatus.Dispatcher.Invoke(delegate ()
                        {
                            LblStatus.Content = "Готов";
                            LblStatus.Foreground = new SolidColorBrush(Colors.Green);
                        });
                    }                
                }
            }
        }


        //Отмечаем, что залогинились
        private void LoginChange(BookmakerType bookmakerType, bool status)
        {
            if (_managers[bookmakerType].Logined == status)
                return;
            for (int i = 0; i < _bookmakers.Count; i++)
            {
                if (_bookmakers[i].BkType == bookmakerType)
                {
                    _bookmakers[i].IsLogin = status;
                    if (status)
                    {
                        _managers[bookmakerType].Clear();
                    }
                    break;
                }
            }
            _managers[bookmakerType].Logined = status;
        }

        //Смена коэф
        private void Job2_CoeffChanged(double newCoefValue)
        {
            if (_bkLoadedCount != 2)
                return;

            Dispatcher.Invoke(delegate ()
            {
                if (vm.IsLockedCoeff || vm.IslockedBetTwo)
                    return;

                vm.UpdateCoeff2(newCoefValue,TxtCoeff2);
            });
        }

        //Смена коэф
        private void Job1_CoeffChanged(double newCoefValue)
        {
            if (_bkLoadedCount != 2)
                return;
            Dispatcher.Invoke(delegate ()
            {
                if (vm.IsLockedCoeff || vm.IsLockedBetOne)
                    return;

                vm.UpdateCoeff1(newCoefValue, TxtCoeff1);
            });
        }
        
        //Отвечает за максимальную ставку
        private void Job1_MaxBetChanged(int valueNewBet)
        {
            if (_bkLoadedCount != 2)
                return;
            float percent = 1;

            //Если у нас максимальная ставка а не баланс
            if(!job1.isbalance)
                 percent = (float)_mainSetting.MaxPercent / 100;   
                    
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (vm.IsLockedBetOne)
                    return;

                var newBetSize = (int)(percent * valueNewBet);
                if (newBetSize == vm.Data.bet1)
                    return;

                if (newBetSize < vm.Data.bet1)
                {
                    TxtBet1.Text = newBetSize.ToString();
                    vm.UpdateBetOneFlag = true;
                    var source = PresentationSource.FromDependencyObject(this);
                    if(source==null)
                        return;
                    vm.EditBet1(TxtBet1, new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.NumPad0));
                    vm.LastMaxBetOne = vm.Data.bet1;
                }
              
            }));            
        }
        
        //Отвечает за максимальную ставку
        private void Job2_MaxBetChanged(int valueNewBet)
        {
            if (_bkLoadedCount != 2)
                return;
            float percent = 1;
            if (!job2.isbalance)
                percent = (float)_mainSetting.MaxPercent / 100;

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (vm.IslockedBetTwo)
                    return;

                var newBetSize = (int) (percent * valueNewBet);
                if (newBetSize == vm.Data.bet2)
                    return;
                if (newBetSize < vm.Data.bet2)
                {
                    Txtbet2.Text = newBetSize.ToString();
                    vm.UpdateBetTwoFlag = true;
                    var source = PresentationSource.FromDependencyObject(this);
                    if (source == null)
                        return;
                    vm.EditBet2(Txtbet2, new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.NumPad0));
                    vm.LastMaxBetTwo = vm.Data.bet2;
                }
            }));            
        }

        
        #endregion


        private bool _forksUpdateStart = true;

        private int _maxRecontError;



        private void ForksUpdate()
        {
            while(true)
            {
                       
                if(_forksUpdateStart)
                {
                    try
                    {
                        _client.ParseData();
                        _maxRecontError = 0;
                    }
                    catch (IOException ex)
                    {
                        _maxRecontError++;
                        if (_maxRecontError < 4)
                            Thread.Sleep(1000);
                        else
                        {
                            MessageBox.Show("Связь с сервером потеряна. Пробуем переподключиться");
                            _maxRecontError = 0;
                            _client.Reconect();
                        }
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

        private async Task ShowData()
        {

            try
            {
                _fmd?.UpdateLifeTime();


                var data = forks.Where(x => x.Profit > _minPercent && x.Profit < _maxPercent).ToList();

                //Выбираем список Бк ставки которых нужно показывать
                var mainFilter = _mainSetting.bookmakers.Where(x => x.IsFilter).Select(x => x.BkType).ToList();
                data = data.Where(x => mainFilter.Contains(x.TypeOne) && mainFilter.Contains(x.TypeTwo)).ToList();

                Dictionary<BookmakerType, List<SportType>> sportFilter = new Dictionary<BookmakerType, List<SportType>>();
                var sportFilterData = _mainSetting.bookmakers.Where(x => mainFilter.Contains(x.BkType)).ToList();
                foreach (var book in sportFilterData)
                {
                    sportFilter.Add(book.BkType, book.Sports.Where(x => x.IsFilter).Select(x => x.SportType).ToList());
                }

                data = data.Where(x => sportFilter[x.TypeOne].Contains(x.SportType) && sportFilter[x.TypeTwo].Contains(x.SportType)).ToList();

                if (Forks.ItemsSource != null)
                {
                    var tmp = data.Except((List<Fork>)Forks.ItemsSource, new ForkComparer()).ToList();
                    //Проигрываем музыку
                    if (_mainSetting.IsMusic && tmp.Any())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _player.Stop();
                            _player.Play();
                        });

                    }
                }



                var focus = await GetAsyncFocus();
                if (focus)
                {
                    if (_mainSetting.IsFocusShow1)
                    {
                        _fmd.SetForks(data);
                    }
                }
                else
                {
                    _fmd.SetForks(data);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Debug.WriteLine($"ForkShow: {DateTime.Now:O} f:{forks.Count}");
        }

        private void CreatePlayer()
        {
            _player = new MediaPlayer();
            _player.Open(
                new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                        @"\music.wav"));
            _player.Volume = 1;
            _player.Position = new TimeSpan(0, 0, 0);
        }

        private async Task<bool> GetAsyncFocus()
        {
            return Forks.IsMouseOver;
        }


        private void LoadData()
        {
            var td = SettingsManger.LoadSettingTry().bookmakers;
            _bookmakers = new List<Bookmaker>();
            foreach(var key in td)
            {
                _bookmakers.Add(key);
            }
        }



        private void SetBtnBlackStyle(Button btn)
        {
            btn.Style = (Style)Application.Current.Resources["RoundCornerBlackInverse"];
        }


        private void SetBtnLigntStyle(Button btn)
        {
            btn.Style = (Style)Application.Current.Resources["RoundCornerBlack"];
        }


        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            SetBtnBlackStyle(sender as Button);
            ShowSettingView();
        }


        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (Test.Visibility == Visibility.Visible)
            {
                Test.Visibility = Visibility.Collapsed;
                Grid.SetRow(BrdFork, 1);
                Grid.SetRowSpan(BrdFork, 2);

                BtnShow.Content = "";
                SetBtnBlackStyle(BtnShow);
            }
            else
            {
                Test.Visibility = Visibility.Visible;
                Grid.SetRow(BrdFork, 2);
                Grid.SetRowSpan(BrdFork, 1);

                BtnShow.Content = "";
                SetBtnLigntStyle(BtnShow);
            }
        }


        private void btnLogin(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                int index = (int)btn.Tag;

                var bookmaker = _bookmakers[index];

                var manag = _managers[bookmaker.BkType];

                manag.SetUrl(bookmaker.Url);
                manag.SignIn(bookmaker.Login, bookmaker.Password);

                _mainSetting.bookmakers = _bookmakers.ToList();
                SettingsManger.SaveSettings(_mainSetting);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось войти: " + ex.Message);
            }
        }


        private double _actualsize;

        //Обрабатывает клик на ввод логина/пароля
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var o = sender as TextBox;
            if (o == null)
                return;
            if (_actualsize < o.ActualWidth && _actualsize > 0)
                o.Width = _actualsize;
            _actualsize = 0;
        }

        //Обрабатывает клик на ввод логина/пароля
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var o = sender as TextBox;
            if (o == null)
                return;
            _actualsize = o.ActualWidth;
            o.Width = _actualsize + 50;
        }

        //Показываем окно с настройками нужного букмекера
        private void BtnSet(object sender, RoutedEventArgs e)
        {
            var l = sender as Button;
            if (l == null)
                return;
            ShowSettingView((int)l.Tag);
        }

        #region Работа с окном вилок

        //Клик по текущей вилке
        private void Forks_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                vm.ResetFlags();

                if (selectedFork != null)
                {
                    _managers[selectedFork.TypeOne].Clear();
                    _managers[selectedFork.TypeTwo].Clear();
                }

                _bkLoadedCount = 0;
                _isNeedAuth = false;
                LblStatus.Content = "Загрузка";
                LblStatus.Foreground = new SolidColorBrush(Colors.Red);

                var fork = ((ListViewItem)sender).Content as Fork;
                if (fork == null)
                    return;


                _fmd.IsFork = true;
                vm.SetFork(fork, _mainSetting.MaxBank);

                selectedFork = fork;
                _fmd.SelectedFork = fork;

                _managers[fork.TypeOne].ShowBet(BrowserTab1.Browser, fork.Url1, fork.Data1, Controllers.Calculator.aRound(vm.Data.bet1));
                _managers[fork.TypeTwo].ShowBet(BrowserTab2.Browser, fork.Url2, fork.Data2, Controllers.Calculator.aRound(vm.Data.bet2));

                LblBook1.Content = fork.TypeOne;
                LblBook2.Content = fork.TypeTwo;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Не удалось открыть вилку"+ex.Message);
            }
        }



        #endregion

        #region Калькулятор

        //управление блокировкой 2х коф
        private void lbCoeff1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(vm.IsLockedCoeff==true)
            {
                vm.IsLockedCoeff = false;                
            }
            else
            {
                vm.IsLockedCoeff = true;
            }
        }

        //управление блокировкой 1й ставки
        private void lbBet1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(vm.IsLockedBetOne)
            {
                vm.IsLockedBetOne = false;             
            }
            else
            {
                vm.IsLockedBetOne = true;
            }
        }

        //управление блокировкой 2й ставки
        private void lbBet2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (vm.IslockedBetTwo)
            {
                vm.IslockedBetTwo = false;               
            }
            else
            {
                vm.IslockedBetTwo = true;             
            }
        }

        //Сделать ставку 1
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFork != null)
            {
                vm.IsLockedBetOne = true;
                _managers[selectedFork.TypeOne].Run();
            }
        }

        //Сделать ставку 2
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (selectedFork != null)
            {
                vm.IslockedBetTwo = true;
                _managers[selectedFork.TypeTwo].Run();
            }
        }

        //Двойная кнопка
        private void btnDouble_Click(object sender, RoutedEventArgs e)
        {
            Button_Click(new object(), new RoutedEventArgs());
            Button_Click_1(new object(), new RoutedEventArgs());
        }

        #endregion


        private void ShowSettingView(int? l=null)
        {

            //Синхронизируем настройки
            lock (_bkChangedLock)
                _mainSetting.bookmakers = _bookmakers.ToList();
            SettingsManger.SaveSettings(_mainSetting);

            //Показываем окно настроек
            SettingsView sv = new SettingsView(_mainSetting,_client, l);
            sv.UpdateSetting += delegate (Settings dt)
            {
                lock (_bkChangedLock)
                    _mainSetting = dt;

                if (dt.IsDoubleButton)
                {
                    BtnDouble.Visibility = Visibility.Visible;
                }
                else
                    BtnDouble.Visibility = Visibility.Hidden;

                //Показываем список активных БК       
                Test.ItemsSource = _bookmakers.Where(x => x.IsShow).ToList();

                lock (_bkChangedLock)
                {
                    foreach (var manag in _managers)
                    {
                        foreach (var key in _mainSetting.bookmakers)
                        {
                            if (manag.Key == key.BkType)
                                manag.Value.SetUrl(key.Url);
                        }
                    }

                    LbFilters.ItemsSource = _mainSetting.Sports;
                }



                
            };
            sv.ShowDialog();


            SetBtnLigntStyle(BtnSetting);
        }
        


#region Работа с фильтрами

        private int _minPercent;
        private int _maxPercent = 100;

        //Минимум %
        private void txtMinPercent_TextChanged(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                return;
            int temp;
            var rez = Int32.TryParse(TxtMinPercent.Text,out temp);
            if(!rez)
            {
                TxtMinPercent.Text = _minPercent.ToString();
                return;
            }
            if(temp>_maxPercent)
            {               
                return;
            }

            _minPercent = temp;
            TxtMinPercent.Text = _minPercent.ToString();
        }

        //Максимум %
        private void txtMaxPercent_TextChanged(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                return;
            int temp;
            var rez = Int32.TryParse(TxtMaxPercent.Text, out temp);
            if (!rez)
            {
                TxtMinPercent.Text = _maxPercent.ToString();
                return;
            }
            if(temp<_minPercent)
            {               
                return;
            }

            _maxPercent = temp;
            TxtMaxPercent.Text = _maxPercent.ToString();
        }

        private readonly object _bkChangedLock = new object();

        //Обрабатываем нажатеие сверху
        private async void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null)
                return;
            var x = (SportType)o.Content;
            lock (_bkChangedLock)
            {
                foreach (var book in _mainSetting.bookmakers)
                {
                    foreach (var sport in book.Sports)
                    {
                        if (sport.SportType != x) continue;
                        if (o.IsChecked != null)
                            sport.IsFilter = o.IsChecked.Value;
                    }
                }
            }

            SettingsManger.SaveSettings(_mainSetting);
        }

        //Обрабатываем нажатеие во вкладке фильтр
        private void CheckBox_Click_1(object sender, RoutedEventArgs e)
        {
            var o = sender as CheckBox;
            if (o == null)
                return;
            var book = (BookmakerType)o.Tag;
            lock (_bkChangedLock)
            {
                foreach (var key in _mainSetting.bookmakers)
                {
                    if (key.BkType == book)
                    {
                        if (o.IsChecked != null)
                            key.IsFilter = o.IsChecked.Value;
                    }
                }
            }
         
        }

        #endregion

#region горячие клавиши

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.F11)
            {
                CreateScreenshort();
            }
            else if(e.Key==Key.F1)
            {              
                vm.ChangeIsLockedBetOne();
            }
            else if(e.Key==Key.F2)
            {
                vm.ChangeIslockedBetTwo();
            }
            else if(e.Key==Key.F3)
            {
                vm.ChangeIsLockedCoeff();
            }
            else if(e.Key==Key.F4)
            {
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
            }
            else if(e.Key==Key.F5)
            {
                _forksUpdateStart = !_forksUpdateStart;
            }
        }

        private void CreateScreenshort()
        {
            string filename = $"{DateTime.Now:ddMMyyyy-mmHHss}.png";
            string path = System.IO.Path.Combine(Environment.CurrentDirectory, "Screenshots");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = System.IO.Path.Combine(path, filename);
            FileStream stream = null;
            //скриншот
            try
            {
                stream = new FileStream(path, FileMode.Create);
                //выключаем блок слогинами
                Test.Visibility = Visibility.Hidden;
                SaveToPNG(this, new Size(ActualWidth, ActualHeight), stream);
                Test.Visibility = Visibility.Visible;
                stream.Dispose();
            }
            catch (Exception ex)
            {
                stream?.Dispose();
                MessageBox.Show("Не удалось сохранить скриншот. " + ex.Message);
            }
        }

        #endregion


        private static void SaveToPNG(FrameworkElement frameworkElement, Size size, Stream stream)
        {
            Transform transform = frameworkElement.LayoutTransform;
            frameworkElement.LayoutTransform = null;
            Thickness margin = frameworkElement.Margin;
            frameworkElement.Margin = new Thickness(0, 0, margin.Right - margin.Left, margin.Bottom - margin.Top);
            frameworkElement.Measure(size);
            frameworkElement.Arrange(new System.Windows.Rect(size));
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(frameworkElement);
            frameworkElement.LayoutTransform = transform;
            frameworkElement.Margin = margin;
            PngBitmapEncoder encoder = new PngBitmapEncoder {Interlace = PngInterlaceOption.On};
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(stream);
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var rez= await _managers[BookmakerType.Marafon].GetBalance();
            MessageBox.Show(rez.ToString());
        }
    }
       
}
