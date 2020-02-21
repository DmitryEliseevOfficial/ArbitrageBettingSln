using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CefSharp.Wpf;
using CefSharp;
using System.Windows;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ABClient.Target
{
    class FonbetManager : ITargerSite
    {
        string url;
        ChromiumWebBrowser wbControl;
        Dictionary<string, string> TaskList = new Dictionary<string, string>();

        public bool Logined { get; set; }


        public FonbetManager(ChromiumWebBrowser wbControl)
        {
            this.wbControl = wbControl;
        }

        private void WbControl_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            checkLogin();
            //CheckMaxBet();
        }

        private void Wb_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
        {
            var path = new Uri(e.Url).AbsolutePath;
            if (TaskList.ContainsKey(path))
            {
                string query = TaskList[path];


                e.Frame.EvaluateScriptAsync(query);
                TaskList.Remove(path);
            }
        }

        public bool SignIn(string user, string password)
        {
            wbControl.FrameLoadStart += Wb_FrameLoadStart;
            wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;
            string loginFunction = "function login(){document.getElementById(\"editLogin\").value=" + user + "; document.getElementById(\"editPassword\").value='" + password + "'; document.getElementById(\"loginButtonLogin\").click();  };";
            string query = loginFunction + " setTimeout(function(){ try { login() ;} catch (ex) { setTimeout(function(){ try { login() ;} catch (ex) { setTimeout(function(){ try  { login() ;} catch (ex) {  } },1000);  } },1000); } },1000);";
            TaskList["/live/"] = query;

            if (wbControl.Address == url + "live/")
                wbControl.Address = "about:blank";
            wbControl.Address = url + "live/";

            return false;
        }

        public void SetUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return;
            if (url.Last() != '/')
                url += "/";
            try
            {
                url = new Uri(url).ToString();
            }
            catch
            {
                try
                {
                    url = new Uri("http://" + url).ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ваш Url от Fonbet, не соответствует правилам. Укажите в настройках верный Url");
                    url = "https://www.fonbet5.com";
                }
            }


            this.url = url;
        }

        public void ShowBet(ChromiumWebBrowser wb, string url, object data, int betSize)
        {
            wbControl = wb;
            wbControl.FrameLoadStart += Wb_FrameLoadStart;
            wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;
            Regex reg = new Regex("([0-9]{7})");
            var match = reg.Match(data.ToString());
            string id = match.Value;


            var t = data.ToString().Split(',');

            if (t.Length != 3)
            {
                MessageBox.Show("Ошибка данных по вилке Fonbet");
                return;
            }

            var x = @"var fullId = '" + t[0] + @"';
var elId = '" + t[2] + @"'; //id эелемента, в которм ставка
var elData = '" + t[1] + @"'; //номер события
var sumBet = '" + betSize + @"'; //сумма
var showElId = '" + t[2] + @"'; //id строки в котороя ставка, для расскрытия
var maxError = 0;
var basketClearCurrent = 0;
var enterSumCurrent = 0;

//Все деалеться рекрусивно, т.к. связь может быть долгой. Браузер подтупливать 
basketClear();

function basketClear() {
    try {
        client.deleteNewCoupon();
        clicker();
    }
    catch (ex) {
        if (basketClearCurrent == 10)
            return;
        basketClearCurrent++;
        console.log('TrybasketClear: ' + basketClearCurrent);
        setTimeout(basketClear, 500);
    }
}


function clicker() {
    try {
        document.getElementById('eventName' + showElId).click();
        //client.addNewCouponStake(elId, elData);
        maxError = 0;
        setTimeout(choiseStake, 200);
    }
    catch (ex) {
        if (maxError == 10)
            return;
        maxError++;
        console.log('TryClicked: ' + maxError);
        setTimeout(clicker, 500);
    }
}

function choiseStake() {
    try {
        var tr = document.getElementById(fullId);
        onBetCellMouseDown(tr);
        onBetCellMouseUp(tr);
        setTimeout(enterSum, 200);
    } catch (e) {
        if (maxError == 10)
            return;
        maxError++;
        console.log('choiseStake: ' + maxError);
        setTimeout(choiseStake, 500);
    } 
}


function enterSum() {
    try {
        var t = document.getElementById('couponNewSumEdit');
        t.value = sumBet;
        t.onchange(t);
        
        jsobject.loginstatus = client.loggedIn;
        jsobject.logined = ""fonbet"";
        jsobject.stoped();
        setInterval(checkBalance, 1000);
    }
    catch (ex) {
        if (enterSumCurrent == 10)
            return;
        enterSumCurrent++;
        console.log('TrysSetSum: ' + enterSumCurrent);
        setTimeout(enterSum, 500);
    }
}

setInterval(function () 
{
    try
    {
        jsobject.currentcoeff = document.getElementById(""" + data.ToString().Split(',').First() + @""").innerText.trim();
    }
    catch(ex)
    {
    }
}, 1000);

function checkBalance() {

    jsobject.isbalance = false;
    var balance = 999999;
    var maximSum = 999999;

    try {
        balance = parseInt(client.data.loginInfo.saldo);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }

    try {
        maximSum = parseInt(document.getElementById('couponNewSumMaxLabel').innerText);
    }
    catch (ex) {
        console.log('couponNewSumMaxLabel not parse: ' + ex);
    }
    
    if (maximSum < balance) {
        jsobject.setmaxbet(maximSum);
    }
    else {
        jsobject.isbalance = true;
        jsobject.setmaxbet(balance);
    }
}

";
            TaskList["/live/"] = x;


            wb.Address = "about:blank";
            wb.Address = this.url + url;
        }

        public void Clear()
        {
            if (wbControl != null)
            {
                wbControl.FrameLoadStart -= Wb_FrameLoadStart;
                wbControl.FrameLoadEnd -= WbControl_FrameLoadEnd;
                TaskList.Clear();
            }
        }

        public void SetBet(int BetSize, object data)
        {
            string query = "document.getElementById(\"couponNewSumEdit\").value=\"" + BetSize + "\"; client.data.newCoupon.amount=" + BetSize + @"; calcUpdate();";
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
                        query =
                            "setTimeout(function(){ try { jsobject.loginstatus=client.loggedIn; jsobject.logined=\"fonbet\"; } catch(ex) {} },1000);";
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

        private void CheckMaxBet()
        {
            if (wbControl != null)
            {
                try
                {
                    string func = @"
function checkBalance() {

    jsobject.isbalance = false;
    var balance = 999999;
    var maximSum = 999999;

    try {
        balance = parseInt(client.data.loginInfo.saldo);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }

    try {
        maximSum = parseInt(document.getElementById('couponNewSumMaxLabel').innerText);
    }
    catch (ex) {
        console.log('couponNewSumMaxLabel not parse: ' + ex);
    }
    
    if (maximSum < balance) {
        jsobject.setmaxbet(maximSum);
    }
    else {
        jsobject.isbalance = true;
        jsobject.setmaxbet(balance);
    }
}";

                    wbControl.ExecuteScriptAsync("setInterval(checkBalance,1000); " + func);
                }
                catch
                {

                }
            }
        }


        public void Run()
        {
            string query = "try{ /*document.getElementsByClassName(\"buttonAgree\")[0].click();*/ } catch(ex){}  try{ document.getElementById(\"couponNewButtonPlaceBet\").click(); } catch(ex) { console.log(ex);}";
            if (wbControl != null)
            {
                try
                {
                    wbControl.ExecuteScriptAsync(query);
                }
                catch
                {

                }
            }
        }

        public void GetMaxBet()
        {
            if (wbControl != null)
            {
                string query = "jsobject.setmaxbet(client.data.newCoupon.max);";
                try
                {
                    wbControl.ExecuteScriptAsync(query);
                }
                catch
                {

                }
            }
        }

        public async Task<int> GetBalance()
        {
            const string script = @"(function()
    					{
	    					return client.data.loginInfo.saldo.split('.')[0];
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

        //client.addNewCouponStake(6318173,923);
        //$('#couponNewSumEdit').value=800;
        //$('#couponNewSumMinLabel').innerText
        //$('#couponNewSumMaxLabel').innerText
        //client.data.loginInfo.saldo - баланс

        //лог 2692548
        //пар 288145
        //$('input[name="login"]').val(2692548);
        //$('input[name="passw"]').val(288145);
        //$('input[value="Вход"].msbtn1').click();
        //$('span[data-id="577759438:26732276:2:4:-4.5:2:0"]').click();  - ставка
        //$('#btmax-577759438-2').val() - максимальная сумма
        //$('#sum-577759438-2').val(5); - ставка



    }

}
