using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;

namespace ABClient.Target
{
    internal class PariMatchManager: ITargerSite
    {
        private ChromiumWebBrowser _wbControl;
        private string _url;
        private int _betSize;

        private bool _OpenStake = false;
        
        private Dictionary<string, string> _taskList = new Dictionary<string, string>();


        public PariMatchManager(ChromiumWebBrowser wbControl)
        {
            _wbControl = wbControl;
        }

        public bool Logined { get; set; }
        public void SetUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return;
            if (!url.EndsWith("/"))
                url = url + "/";
            _url = url;
        }

        public bool SignIn(string login, string password)
        {
            string queryLogin = $" function LogIned() {{document.getElementsByName('username')[0].value=\"{login}\"; "+
                           $"document.getElementsByName('passwd')[0].value=\"{password}\"; "+
                           "document.getElementsByClassName('btn_orange ok')[0].click();} ; setTimeout(LogIned,500);";
            _taskList["/?login=1"] = queryLogin;

            string query = $" document.navAuth.submit(); ";
            _taskList["/"] = query;
            
            _wbControl.FrameLoadEnd += _wbControl_FrameLoadEnd;
            _wbControl.FrameLoadStart += _wbControl_FrameLoadStart;

            
            _wbControl.Address = "about:blank";
            _wbControl.Address = $"{_url}";


            return false;
        }

        private  void _wbControl_FrameLoadStart(object sender, CefSharp.FrameLoadStartEventArgs e)
        {
            string path = new Uri(e.Url).PathAndQuery;

            if (!_taskList.ContainsKey(path)) return;

            string query = _taskList[path];
            query = "document.addEventListener('DOMContentLoaded', DomLoaded); function DomLoaded() {" + query + "};  ";

            e.Frame.EvaluateScriptAsync(query);
            _taskList.Remove(path);
        }

        private void _wbControl_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
           checkLogin();
        }

        public Task<int> GetBalance()
        {
            throw new NotImplementedException();
        }

        public void ShowBet(ChromiumWebBrowser wb, string url, object data, int betSize)
        {
            _wbControl = wb;
            _wbControl.FrameLoadEnd += _wbControl_FrameLoadEnd;
            _wbControl.FrameLoadStart += _wbControl_FrameLoadStart;
            _betSize = betSize;
            _OpenStake = false;


            string query = $"try {{  CC(); document.getElementById('{data}').click(); jsobject.stoped(); }} catch(ex){{}} ";

            string checkCoef = " function Check(){ try{ jsobject.currentcoeff=document.getElementById('betsCoeff').innerText;  } catch(ex) { } }; setInterval(Check,1000);";
            _taskList[$"/{url}"] = query+checkCoef;

            _wbControl.Address = "about:blank";
            _wbControl.Address = $"{_url}{url}";
        }

        public void SetBet(int Betsize, object data)
        {
            _betSize = Betsize;
        }

        public void Clear()
        {
            if (_wbControl == null) return;
            _wbControl.FrameLoadEnd -= _wbControl_FrameLoadEnd;
            _wbControl.FrameLoadStart -= _wbControl_FrameLoadStart;
            _taskList.Clear();
        }

        public void Run()
        {
            
            //string setBet = $"try{{ document.getElementsByName('sums')[0].value='{_betSize}'; document.getElementById('do_stake').click(); }}  catch(ex){{}} ";

            //_taskList["stake.html"] = setBet;

            
            if (_wbControl != null)
            {
                try
                {
                    if (!_OpenStake)
                    {
                        string query = "try{ MS(); } catch(ex){} ";
                        _wbControl.ExecuteScriptAsync(query);
                        _OpenStake = true;
                        Task.Factory.StartNew(() =>
                           {
                               System.Threading.Thread.Sleep(1000);
                               Run();
                           });
                    }
                    else
                    {
                         string setBet = $" try{{ document.getElementsByName('sums')[0].value='{_betSize}'; document.getElementById('do_stake').click(); }}  catch(ex){{}}  ";
                        _wbControl.ExecuteScriptAsync(setBet);

                    }
                }
                catch
                {

                }
            }
        }

        private void checkLogin()
        {
            if (_wbControl != null)
            {
                try
                {

                    _wbControl.ExecuteScriptAsync(@"function mylogin() {
    try {
        var x = (document.getElementsByClassName('login')[0] == null);
        if (x == true) {
            jsobject.loginstatus = true;
        } else {
            jsobject.loginstatus = false;
        }
        jsobject.logined = 'parimatch';
    } catch (e) {
        log.console(e);
    } 
}; 
setTimeout(mylogin(), 1500);");
                    
                }
                catch
                {

                }
            }

        }
    }
}
