using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ABClient.Components.handlers;
using CefSharp;
using CefSharp.Wpf;

namespace ABClient.Components
{
    /// <summary>
    /// Логика взаимодействия для BrowserTab.xaml
    /// </summary>
    public partial class BrowserTab : UserControl
    {


        private double _zoomLvl { get; set; } = 0;
        public double zoomLvl { get { return _zoomLvl; }
            set
            {
                _zoomLvl = value;
                if (this.IsInitialized)
                {
                    if(Cef.IsInitialized)
                        BrowserControl.SetZoomLevel(zoomLvl);
                    lbZoom.Dispatcher.Invoke(delegate()
                    { lbZoom.Content = (int)(_zoomLvl * 100) + "%"; });
                    
                }
            }
        }

        public ChromiumWebBrowser Browser {get { return BrowserControl; } }
       


        public BrowserTab()
        {
            InitializeComponent();
            var bind = new Binding();
            BrowserControl.MenuHandler = new handlers.MenuHandler();
            BrowserControl.LifeSpanHandler= new LifespanHandler();
            Browser.FrameLoadStart += Browser_FrameLoadStart;
        }

        private async void Browser_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
        {
            if (Cef.IsInitialized)
                if (await Browser.GetZoomLevelAsync() != _zoomLvl)
                    zoomLvl = _zoomLvl;
        }
        

        private void btnSub_Click(object sender, RoutedEventArgs e)
        {
            zoomLvl = zoomLvl - 0.2;
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            zoomLvl = zoomLvl + 0.2;  
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            BrowserControl.ShowDevTools();
        }
    }
}
