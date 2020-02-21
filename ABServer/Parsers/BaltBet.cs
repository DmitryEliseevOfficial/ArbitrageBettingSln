using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ABShared;
using AngleSharp.Parser.Html;
using HtmlAgilityPack;
using xNet;
using Newtonsoft.Json.Linq;

namespace ABServer.Parsers
{
#if DEBUG
    
    internal class BaltBet : IParse
    {
        private string _url = "Https://baltbet.com/";
        private readonly HttpRequest _req;
        public BookmakerType Bookmaker { get; } = BookmakerType.BaltBet;
        public ConcurrentStack<Bet> Bets { get; set; } = new ConcurrentStack<Bet>();

        public BaltBet()
        {
            _req = new HttpRequest
            {
                CharacterSet = Encoding.UTF8,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36 OPR/41.0.2353.46"
            };

        }


        public bool UsingProxy { get; set; }
        public List<string> ProxyList { get; set; }

        public void SetUrl(string url)
        {
            if (url == "")
                return;
            if (!url.EndsWith("/"))
                url = url + "/";
            if (!url.ToLower().StartsWith("https://") && !url.ToLower().StartsWith("http://"))
                url = "https://" + url;
            _url = url;
        }

        public List<Bet> Parse()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<Bet> rezultBets = new List<Bet>();


            string response = _req.Get(_url).ToString();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@class='live']/ul/li/a");
            if (nodes == null)
                throw new ArgumentNullException(typeof(BaltBet).ToString());

            //Получаем Id событий
            List<string> idList = new List<string>();
            foreach (HtmlNode node in nodes)
            {
                idList.Add(node.Id.Replace("addl", ""));
                ParsePage(node.Id.Replace("addl", ""));
            }



            sw.Stop();
            Console.WriteLine($"BaltBet time:{sw.Elapsed} Count:{rezultBets.Count}");
            return rezultBets;
        }

        private void ParsePage(string id)
        {
            HttpRequest req = new HttpRequest
            {
                CharacterSet = Encoding.UTF8,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36 OPR/42.0.2393.94"
            };

            if (UsingProxy)
            {
                req.Proxy = ProxyClient.Parse(ProxyList.GetRnd());
                req.Proxy.ReadWriteTimeout = 1500;
                req.Proxy.ConnectTimeout = 1500;
            }
            RequestParams requestParams = new RequestParams()
            {
                ["ajax"] = "main",
                ["data"] = $"4|{id}",
            };

            string response = req.Post($"{_url}", requestParams).ToString();
            string data = response.Remove(response.Length - 2).Remove(0,9);
            var json = JObject.Parse(response);

            HtmlParser parser = new HtmlParser();
            var doc = parser.Parse(json["text"].ToString());
            Bet bet = new Bet();
            bet.Bookmaker= BookmakerType.BaltBet;
            bet.Site = "B";

            var maintable = doc.Body.Children[2];
            // HtmlNode mainTable = doc.DocumentNode.ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0];
            var groupe = maintable.Children[0].Children[0].TextContent.Split('.')[1].Trim();

            bet.SportType = SportTypeHelper.Parse(groupe);

            var teams =
                maintable.Children[1].Children[1].Children[0].Children[3].TextContent.Split('-');
        }
    }
#endif
}