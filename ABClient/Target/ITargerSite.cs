
using System;
using CefSharp.Wpf;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ABClient.Target
{
    interface ITargerSite
    {
        bool Logined { get; set; }
        void SetUrl(string url);
        bool SignIn(string login, string password);
        Task<int> GetBalance();
        void ShowBet(ChromiumWebBrowser wb, string url, object data, int betSize);
        void SetBet(int betsize, object data);
        void Clear();
        void Run();
    }

}
