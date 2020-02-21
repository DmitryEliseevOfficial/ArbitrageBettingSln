using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CefSharp;
using CefSharp.Wpf;
using System.Windows;
using System.Threading.Tasks;

namespace ABClient.Target
{
    class MarafonManager : ITargerSite
    {
        string url;
        
        ChromiumWebBrowser wbControl;
       
        Dictionary<string, string> TaskList = new Dictionary<string, string>();
        public bool Logined { get; set; }

        public MarafonManager(ChromiumWebBrowser wbControl)
        {
            this.wbControl = wbControl;            
        }

        private void Wb_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
        {
            var path = new Uri(e.Url).AbsolutePath;

            if (TaskList.ContainsKey(path))
            {
                string query = TaskList[path];
                //query = "window.onload = function() {" + query + "};  ";

                e.Frame.EvaluateScriptAsync(query);
                TaskList.Remove(path);
            }
        }

        private void WbControl_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            CheckLogin();
            //CheckMaxBet();
        }


        public async Task<int> GetBalance()
        {
            const string script = @"(function()
    					{
	    					return document.getElementById(""balance"").innerText.split(' ')[1].split('.')[0];
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

        public bool SignIn(string Login, string Password)
        {
            string query = " function enterdata(){ $('#auth_login').val(" + Login + "); $('#auth_login_password').val(\"" + Password + "\"); } document.addEventListener(\"DOMContentLoaded\", enterdata);    function logined(){ $('button.btn-login').click(); } window.onload=logined;";
            TaskList["/su"] =query;

            wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;
            this.wbControl.FrameLoadStart += Wb_FrameLoadStart;
            if (wbControl.Address == url)
                wbControl.Address = "about:blank";
            wbControl.Address = new Uri(url).ToString()+"su";
            return false;
        }

        public void SetUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return;
            if (url.Last() != '/')
                url += "/";
            if(url.Length>3)
            {
                var str = url.Substring(url.Length - 3);
                if(str=="su/")
                    url= url.Substring(0,url.Length - 3);
                else if(str=="/su")
                    url = url.Substring(0, url.Length - 2);
            }
            try
            {
                url = new Uri(url).ToString();
            }
            catch
            {
                try
                {
                    url = new Uri("http://"+url).ToString();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Ваш Url от Марафона, не соответствует правилам. Укажите в настройках верный Url");
                    url = "https://www.marathonbet.com/";
                }
            }

            this.url = url;
        }
        
        public void ShowBet(ChromiumWebBrowser wb, string url, object data, int betSize)
        {
            wbControl = wb;

            wbControl.Address = "auto:blank";
            //$("span.button.btn-remove").click(); 
            //$("span[data-selection-key='4424923@Match_Winner_Including_All_OT.HB_H']").click(); 
            //document.getElementById("stake.4424923,Match_Winner_Including_All_OT.HB_H").value='123';
            //document.getElementById("stake.4424923,Match_Winner_Including_All_OT.HB_H").onkeyup(document.createEvent('KeyboardEvent'))
            string query =
                @"
document.addEventListener('DOMContentLoaded', DomLoad);  
function DomLoad() { 
   
    setTimeout(function(){

        //это можно убрать до try catch
        BasketClear(0);
        
        function BasketClear(tryNumber)
        {
            try
            {
                getBetslip().removeAll();
                //document.querySelector('span.button.btn-remove').click();
            }
             catch(ex) {
                if(tryNumber==10)
                    return;
                tryNumber++;
                console.log('BasketClearError.try= '+tryNumber);                
                setTimeout(BasketClear,tryNumber);
            }
        }


        console.log('Click= '+'" + data + @"');   

        var currentElement=document.querySelector(""span[data-selection-key='" + data + @"']"");  
        if(currentElement==null)
        {
             console.log('currentElement==null');
             return;
        }            
        currentElement.click();
        try{ if(BetslipStorageFacade.controller.isOpenCoupon()==false) { toggleBetslip();} } catch(ex) {
        console.log('BetslipStorageFacade= '+ex); }
        
        var tryNumber=1;
        setTimeout(SetMyBet,500);  

function SetMyBet(){
                            
                  try {
                        console.log('dt= '+'" + data + @"');   
                        var dt='" + data + @"'.replace('@',',');
                        var dt='stake.'+'" + data+ @"'.replace('@',',');

                        console.log('dt= '+dt);    
                        //document.getElementById(dt).focus();
                        var stakeEl=document.getElementById(dt);
                        if(stakeEl==null)
                        {
                             console.log('stakeEl==null');
                            //return;
                        }
                        /* extractSum(stakeEl, 2, false); 
                        getBetslip().checkBet(dt);*/

                       stakeEl.onkeyup(document.createEvent('KeyboardEvent'));
                        stakeEl.value='" + betSize+ @"';
                        stakeEl.onkeyup(document.createEvent('KeyboardEvent'));

                        jsobject.stoped(); 
                        setInterval(checkBalance,1000);
                        } 
                catch(ex) {
                            if(tryNumber==10)
                                return;
                            console.log('SetMyBetError: '+ex);
                            tryNumber++;
                            console.log('tryNumber='+tryNumber);    
                            setTimeout(SetMyBet,500);
                        }
     
        }; 
    },1000);
};

setInterval(function () {
    try { 
            var coef=$(""span[data-selection-key='" + data + @"']"").text().trim().split('.');
            jsobject.currentcoeff = coef[0]+'.'+coef[1];
            console.log('jsobject.currentcoeff = '+$(""span[data-selection-key='" + data + @"']"").text().trim()); 
    } catch(ex){ console.log('currentcoeff: '+ex); }
}, 1000);


function checkBalance() {

    jsobject.isbalance = false;
    var Balance = 999999;
    try {
        var balDt = document.getElementById('balance').innerText.split(' ')[1].split('.')[0].replace(',', '');
        Balance = parseInt(balDt);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }
    var maximSum = 999999;
    try {
        maximSum = parseInt(document.getElementsByClassName('min-max-stake')[0].children[0].children[0].innerText.split('.')[0].replace(',', ''));
    }
    catch (ex) {
        console.log('Max bet not parse: ' + ex);
    }
    if (maximSum < Balance) {
        jsobject.setmaxbet(maximSum);
    }
    else {
        jsobject.isbalance = true;
        jsobject.setmaxbet(Balance);
    }
}

";
#if DEBUG
            Clipboard.SetText(query);
#endif
            wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;
            wbControl.FrameLoadStart += Wb_FrameLoadStart;

            var u = new Uri(this.url + url);
            TaskList[u.AbsolutePath] =query;

            wbControl.Address = this.url+url;
        }

        public void Clear()
        {
           if(wbControl!=null)
            {
                wbControl.FrameLoadStart -= Wb_FrameLoadStart;
                wbControl.FrameLoadEnd -= WbControl_FrameLoadEnd;
                TaskList.Clear();
            }
        }

        public void SetBet(int BetSize, object data)
        {
            string query = @"       
        var y=$(""input[name='stake']"")[0]; 
        try
        {
            y.value='" + BetSize + @"';
            var dt= y.id.replace(""stake."",""""); 
            getBetslip().checkBet(dt);
        }
        catch(ex)
        {
            console.log(""Bet not set: ""+ex)
        }";
            wbControl?.ExecuteScriptAsync(query);
        }
        
        private void CheckLogin()
        {
            if (wbControl != null)
            {
               
                try
                {
                    string query = "";
                    if (!Logined)
                    {
                        query = @"
function myLogin() {
    try {
        jsobject.loginstatus = (document.getElementById('auth_login') == null && document.getElementById('logoutLink') != null);
    } catch (e) {
        jsobject.loginstatus = false;
    } 
    jsobject.logined = 'marafon';
}

setTimeout(myLogin, 1000);";
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
            string query = "try{ /*document.getElementById(\"betslip_apply_choices\").click(); */ } catch(ex) { console.log(ex);}  try{ $(\"#betslip_placebet_btn_id\").click(); } catch(ex){ console.log(ex);}";
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
    var Balance = 999999;
    try {
        var balDt = document.getElementById('balance').innerText.split(' ')[1].split('.')[0].replace(',', '');
        Balance = parseInt(balDt);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }
    var maximSum = 999999;
    try {
        maximSum = parseInt(document.getElementsByClassName('min-max-stake')[0].children[0].children[0].innerText.split('.')[0].replace(',', ''));
    }
    catch (ex) {
        console.log('Max bet not parse: ' + ex);
    }
    if (maximSum < Balance) {
        jsobject.setmaxbet(maximSum);
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
