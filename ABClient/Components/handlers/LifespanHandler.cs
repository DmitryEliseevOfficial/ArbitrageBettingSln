using System;
using System.Windows;
using System.Windows.Threading;
using CefSharp;
using CefSharp.Wpf;

namespace ABClient.Components.handlers
{
    public class LifespanHandler : ILifeSpanHandler
    {
        bool ILifeSpanHandler.OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {

            browserControl.Load(targetUrl);

            newBrowser = null;
            return true;

        }

        void ILifeSpanHandler.OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
            if (browser.IsPopup)
            {
                var chromiumWebBrowser = (ChromiumWebBrowser)browserControl;

                chromiumWebBrowser.Dispatcher.Invoke(() =>
                {
                    var owner = Window.GetWindow(chromiumWebBrowser);

                    if (owner != null && owner.Content == browserControl)
                    {
                        owner.Show();
                    }
                });
            }
        }

        bool ILifeSpanHandler.DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            return false;
        }

        void ILifeSpanHandler.OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {

        }
    }
}
