using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CefSharp;
using CefSharp.Wpf;
using System.Threading.Tasks;

namespace ABClient.Target
{
    class ZenitManager : ITargerSite
    {
        string url = "";
        ChromiumWebBrowser wbControl;

        public bool Logined { get; set; }

        Dictionary<string, string> TaskList = new Dictionary<string, string>() { { "/account/login", "setTimeout(function(){ jsobject.loginstatus=false; jsobject.logined=\"zenit\";jsobject.stoped(); },2000);" } };

        public ZenitManager(ChromiumWebBrowser wbControl)
        {
            this.wbControl = wbControl;
        }

        public async Task<int> GetBalance()
        {
            const string script = @"(function()
    					{
	    					return document.getElementById(""basket-balance"").innerText.replace("" руб"","""").split("","")[0].replace("" "","""");
    					})();";

            var task = wbControl.EvaluateScriptAsync(script).ContinueWith(x =>
            {
                var response = x.Result;

                if (response.Success && response.Result != null)
                {
                    int ret = 0;
                    Int32.TryParse(response.Result.ToString(), out ret);
                    return ret;
                }

                return 0;
            });

            var rez = await task;
            return rez;
        }

        public bool SignIn(string login, string password)
        {
            string query = "$('input[name=\"login\"]').val('" + login + "');" + "$('input[name=\"password\"]').val('" + password + "');" + "document.getElementById(\"header-login-button\").click(); ";
            TaskList["/"] = query;

            //this.wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;
            wbControl.FrameLoadStart += Wb_FrameLoadStart;
            wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;
            if (wbControl.Address == url)
                wbControl.Address = "about:blank";
            wbControl.Address = url;




            return false;
        }

        private void WbControl_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            checkLogin();
            //CheckMaxBet();
        }

        public void SetUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return;
            if (url.Last() != '/')
                url += "/";
            this.url = url;
        }

        public void ShowBet(ChromiumWebBrowser wb, string url, object data, int betSize)
        {
            wbControl = wb;
            wbControl.FrameLoadStart += Wb_FrameLoadStart;
            wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;


            //открываем страницу с линией
            string query = "setTimeout(function() { try{ $('a[data-gid=\"" + url + "\"]').click(); } catch(ex) {} },100); ";
            //Щелкаем по нужной ставке
            string query1 = @"setTimeout(function(){
                                       
                                    try{ $('#basket-clear').click(); } catch(ex) {} 
                                    try
                                    {
                                        $(""a[href = '" + data + @"']"").click(); 
                                        setTimeout(function(){
                                                        document.getElementsByClassName('basket-item-amount')[0].value='" + betSize + @"'; 
                                                        
                                                        jsobject.stoped();
                                                        setInterval(checkBalance,1000); 
                                                        },500);
                                        
                                    } 
                                    catch(ex) 
                                    { 
                                        console.log(ex); 
                                    } 
                                    finally
                                    {
                                        
                                    }
                            },1000);  
                            setInterval(function () 
                            {  
                                    try 
                                    { 
                                        jsobject.currentcoeff = $(""a[href='" + data + @"']"").text().trim(); 
                                    } catch(ex){}  
                            }, 1000);  

function checkBalance() {

    jsobject.isbalance = false;
    var Balance = 9999999;
    try {
        var balDt = document.getElementById('basket-balance').innerText.replace(' руб', '').split(',')[0].replace(' ', '');
        Balance = parseInt(balDt);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }

    var MaxBet = 9999999;
    if (MaxBet < Balance) {
        jsobject.setmaxbet(MaxBet);
    }
    else {
        jsobject.isbalance = true;
        jsobject.setmaxbet(Balance);
    }
}

";

            TaskList["/live"] = query + query1;

            TaskList["/live/view"] = query1;

            wbControl.Address = "about:blank";
            wbControl.Address = this.url + "live";
        }

        //Если в таск листе что-то есть, то ставим хэндл на windows.onload
        private void Wb_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
        {
            var path = new Uri(e.Url).AbsolutePath;

            if (TaskList.ContainsKey(path))
            {
                string query = TaskList[path];
                query = "window.onload=function() {" + query + "   };";

                e.Frame.EvaluateScriptAsync(query);
                TaskList.Remove(path);
            }
        }

        public void Clear()
        {
            if (wbControl != null)
            {
                wbControl.FrameLoadEnd -= WbControl_FrameLoadEnd;
                wbControl.FrameLoadStart -= Wb_FrameLoadStart;
                TaskList.Clear();
            }
        }

        public void SetBet(int BetSize, object data)
        {
            string query = "document.getElementsByClassName(\"basket-item-amount\")[0].value=" + BetSize + ".00;";
            wbControl?.ExecuteScriptAsync(query);
        }

        private void checkLogin()
        {
            if (wbControl != null)
            {

                try
                {
                    string query = "";
                    if (!Logined)
                    {
                        query = "setTimeout(function(){ var x= document.getElementById(\"loginform\"); var log = document.getElementById(\"logout\"); if(x==null && log!=null){ jsobject.loginstatus=true;} else {jsobject.loginstatus=false;} jsobject.logined=\"zenit\"; },1000);";
                    }
                    else
                    {
                        query = "jsobject.loginstatus=true;";
                    }
                    wbControl.ExecuteScriptAsync(query);
                }
                catch
                {

                }

            }
        }

        public void Run()
        {
            string query = @"
                            try
                            {	                            
	                            var bet =document.getElementsByClassName(""basket-item-amount"")[0];
	                            var bet_id=bet.id.split('-')[3];
	                            z.basket[""odds""][bet_id].amount=bet.value;	                           
	                            document.getElementById(""basket-dobet"").click();
                            }
                            catch(ex)
                            {
	                            console.log(ex);
                            }";


            wbControl?.ExecuteScriptAsync(query);
        }

        public void CheckMaxBet()
        {
            if (wbControl != null)
            {
                try
                {
                    string func = @"
function checkBalance() {

    jsobject.isbalance = false;
    var Balance = 9999999;
    try {
        var balDt = document.getElementById('basket-balance').innerText.replace(' руб', '').split(',')[0].replace(' ', '');
        Balance = parseInt(balDt);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }

    var MaxBet = 9999999;
    if (MaxBet < Balance) {
        jsobject.setmaxbet(MaxBet);
    }
    else {
        jsobject.isbalance = true;
        jsobject.setmaxbet(Balance);
    }
}";
                    wbControl.ExecuteScriptAsync("setInterval(checkBalance,1000); " + func);
                }
                catch
                {

                }
            }
        }

    }
}
