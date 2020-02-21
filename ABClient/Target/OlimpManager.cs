using System;
using System.Collections.Generic;
using System.Linq;
using CefSharp;
using CefSharp.Wpf;
using System.Windows;
using System.Threading.Tasks;

namespace ABClient.Target
{
    internal class OlimpManager : ITargerSite
    {
        private string _url;
        private ChromiumWebBrowser _wbControl;

        private readonly Dictionary<string, string> _taskList = new Dictionary<string, string>();

        public bool Logined { get; set; }

        public OlimpManager(ChromiumWebBrowser wbControl)
        {
            _wbControl = wbControl;
        }

        public async Task<int> GetBalance()
        {
            const string script = @"(function()
    					{
	    					return document.getElementsByClassName(""currusum"")[0].innerText.replace(""'"","""").split(',')[0];;
    					})();";

            var task = _wbControl.EvaluateScriptAsync(script).ContinueWith(x =>
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
            string query = @"
var login = '" + Login + @"';
var password = '" + Password + @"';
var enterError = 0;

function myEnter() {
    if (document.forms.lf != null) {
        document.querySelectorAll('input[name=""login""]')[0].value = login;
        document.querySelectorAll('input[name=""passw""]')[0].value = password;
        document.querySelectorAll('input.msbtn1')[0].click();
        } else {
        if (enterError === 10) {
            console.log('Вход не удался');
            return;
        }
    enterError++;
        setTimeout(myEnter(), 300);
}
}

myEnter();
";
            _taskList["/betting"] = query;

            _wbControl.FrameLoadStart += WbControl_FrameLoadStart;
            _wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;

            if (_wbControl.Address == _url)
                _wbControl.Address = "about:blank";
            _wbControl.Address = _url + "betting";


            return false;
        }

        private void WbControl_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
        {
            var path = new Uri(e.Url).PathAndQuery;

            if (_taskList.ContainsKey(path))
            {
                string query = _taskList[path];
                query = "document.addEventListener('DOMContentLoaded', DomLoad);  function DomLoad() {" + query + "};  ";

                e.Frame.EvaluateScriptAsync(query);
                _taskList.Remove(path);
            }
        }


        private void WbControl_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            CheckLogin();
            //CheckMaxBet();
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
                    MessageBox.Show("Ваш Url от Olimp`a, не соответствует правилам. Укажите в настройках верный Url");
                    url = "https://olimp.com";
                }
            }
            url = url.Replace("http://", "https://");
            _url = url;
        }

        public void ShowBet(ChromiumWebBrowser wb, string url, object data, int betSize)
        {
            _wbControl = wb;
            _wbControl.FrameLoadStart += WbControl_FrameLoadStart;
            _wbControl.FrameLoadEnd += WbControl_FrameLoadEnd;

            string query = @" 
var elementId = '" + data + @"';
var betSize = '" + betSize + @"';

//Количество максимальных попыток
var maxError = 0;

function basketClear()
{
    var basketIsEmty = !(document.querySelectorAll('div.busket-body > div.basket-empty')[0] == null);
    if (basketIsEmty)
    {
        selectStake();
    }
    else
    {
        try {
            document.getElementsByClassName('clearAllbasket')[0].click();
        }
        catch (ex) {
            console.log('Не удалось очистить корзину. Пробуем снова');
        }
        setTimeout(basketClear, 300);
    }
}

function selectStake () {   
        try { 
            document.getElementById(elementId).click();

            maxError = 0;
            setTimeout(SetAndCheckValue,300); 
        }  
        catch (ex) { 
            if (maxError === 10) {
                console.log('Не удалось раскрыть ставку. завершаемся.');
                return;
            }
            maxError++;            
            console.log('Не удачная попытка выбора ставки. Пробуем еще раз' + ex);
            setTimeout(selectStake, 300); 
        }        
};

function SetAndCheckValue() {

    SetValue();

    var currentBetsize = 0;

    try {
                currentBetsize=parseInt(document.getElementsByName('singlebet_sum0')[0].value.replace(' ',''));
    } catch (e) {

    } 

    if (currentBetsize === parseInt(betSize)) {
        jsobject.stoped();
        setInterval(checkBalance,1000); 
    } else {
        if (maxError === 10) {
            console.log('Не удалось задать сумму. завершаемся.');
            return;
        }
        console.log('Сумма не вбита. пробуем еще раз');
        maxError++;
        setTimeout(SetAndCheckValue, 300);
    }
}

function SetValue() {
    try {
        document.getElementsByName('singlebet_sum0')[0].value = betSize;
        CalcMaxWin();
    }
    catch (ex) {
        console.log('SetValue error: ' + ex);
    }
};

setTimeout(basketClear, 300);


function checkCurrentCoef() {
    try {
        jsobject.currentcoeff = document.getElementById(elementId).firstChild.innerText.trim();
    }
    catch (ex) {
        jsobject.currentcoeff = '0';
        console.log('ошибка получения коээфицента');
    }
}

setInterval(checkCurrentCoef, 1000);


function checkBalance() {

    jsobject.isbalance = false;
    var Balance = 9999999;
    var maximSum = 999999;

    try {
        var balDt = document.getElementsByClassName('currusum')[0].innerText.replace(""'"",'').split(', ')[0];
        Balance = parseInt(balDt);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }

    try {
        maximSum = parseInt(document.getElementsByClassName('max_sum_ord_js')[0].value.replace(' ', ''));
    } catch (e) {
        console.log('max_sum_ord_js not parse: ' + ex);

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


            var u = new Uri(_url + url);
            _taskList[u.PathAndQuery] = query;


            //wbControl.Address = "about:blank";
            _wbControl.Reload(true);
            _wbControl.Address = new Uri(_url) + url;
        }

        public void SetBet(int Betsize, object data)
        {
            if (_wbControl != null)
            {
                try
                {
                    string query = "try { document.getElementsByName(\"singlebet_sum0\")[0].value=" + Betsize + "; CalcMaxWin(); } catch(ex){ console.log(ex);}";
                    _wbControl.ExecuteScriptAsync(query);
                }
                catch
                {

                }

            }
        }

        public void Clear()
        {
            if (_wbControl == null) return;
            _wbControl.FrameLoadEnd -= WbControl_FrameLoadEnd;
            _wbControl.FrameLoadStart -= WbControl_FrameLoadStart;
            _taskList.Clear();
        }

        private void CheckLogin()
        {
            if (_wbControl != null)
            {

                try
                {
                    string query;
                    if (!Logined)
                    {
                        query = @"
function mylogin() {
    console.log('checkLogin');
    try {
        jsobject.loginstatus = (document.forms.if == null && document.getElementById('linfb') != null);
    } catch (e) {
        jsobject.loginstatus = false;
    } 

    jsobject.logined = 'olimp';
};
setTimeout(mylogin(), 1500);";
                    }
                    else
                    {
                        query = "jsobject.loginstatus=true;";
                    }
                    _wbControl.ExecuteScriptAsync(query);

                }
                finally
                {

                }

            }

        }

        public void Run()
        {
            if (_wbControl != null)
            {
                try
                {
                    _wbControl.ExecuteScriptAsync(" try{/* document.getElementById(\"b_ever\").checked=true;*/ document.getElementsByName(\"formsubmit\")[0].click(); } catch(ex) { console.log(ex);}");
                }
                catch
                {

                }
            }
        }

        private void CheckMaxBet()
        {
            if (_wbControl != null)
            {
                try
                {
                    string func = @"
function checkBalance() {

    jsobject.isbalance = false;
    var Balance = 9999999;
    var maximSum = 999999;

    try {
        var balDt = document.getElementsByClassName('currusum')[0].innerText.replace(""'"",'').split(', ')[0];
        Balance = parseInt(balDt);
    }
    catch (ex) {
        console.log('Balance not parse: ' + ex);
    }

    try {
        maximSum = parseInt(document.getElementsByClassName('max_sum_ord_js')[0].value.replace(' ', ''));
    } catch (e) {
        console.log('max_sum_ord_js not parse: ' + ex);

    } 

    if (maximSum < Balance) {
        jsobject.setmaxbet(maximSum);
    }
    else {
        jsobject.isbalance = true;
        jsobject.setmaxbet(Balance);
    }
}";
                    _wbControl.ExecuteScriptAsync("setInterval(checkBalance,1000); " + func);
                }
                catch
                {

                }
            }
        }
    }


}
