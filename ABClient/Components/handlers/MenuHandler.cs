using CefSharp;
using CefSharp.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ABClient.Components.handlers
{
    public class MenuHandler : IContextMenuHandler
    {

        void IContextMenuHandler.OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {

        }

        bool IContextMenuHandler.OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            return false;
        }

        void IContextMenuHandler.OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
        }

        bool IContextMenuHandler.RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            var chromiumWebBrowser = (ChromiumWebBrowser)browserControl;



            chromiumWebBrowser.Dispatcher.Invoke(new Action(() =>
            {
                var menu = new ContextMenu
                {
                    IsOpen = true
                };

                RoutedEventHandler handler = null;

                handler = (s, e) =>
                {
                    menu.Closed -= handler;

                    if (!callback.IsDisposed)
                    {
                        callback.Cancel();
                    }
                };

                menu.Closed += handler;

                var showDevOp = new MenuItem
                {
                    Header = "Show DevTools",

                };

                showDevOp.Click += delegate (object obj, RoutedEventArgs ev)
                {
                    chromiumWebBrowser.ShowDevTools();
                };

                menu.Items.Add(showDevOp);


                chromiumWebBrowser.ContextMenu = menu;
            }));

            return true;
        }

    }
}
