using StaticData.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;

namespace StaticData.Parsers.PariMatch
{
    public class PariMatch
    {
        private string _url;

        public PariMatch(string url)
        {
            _url = url;
        }

        public List<SiteRow> ParseAnonsLive()
        {
            var rezultList = new List<SiteRow>();
            WebClient wb = new WebClient();
            wb.Encoding = Encoding.GetEncoding(1251);
            var response = wb.DownloadString($"{_url}/live.html");
            var parser = new HtmlParser();

            IHtmlDocument document = parser.Parse(response);

            var table = document.GetElementById("sheduled");
            if (table == null)
                throw new ArgumentException("Нет таблицы с прелайвом");

            foreach(var key in table.Children[1].Children[0].Children)
            {
                if(key.ClassName.StartsWith("sport"))
                {
                    var sport = key.Children[0].TextContent;

                    foreach(var item in key.Children[1].Children)
                    {
                        if (string.IsNullOrWhiteSpace(item.Id)) continue;

                        var liga = item.PreviousSibling.TextContent;

                        foreach(var ev in item.Children)
                        {
                            var sr = new SiteRow();
                            sr.Sport = sport;
                            sr.Groupe = liga;
                            sr.Site = Shared.Enums.ParserType.PariMatch;

                            int day = Int32.Parse(ev.Attributes["data-date"].Value);
                            var dt = DateTime.Now;
                            if (dt.Day > day)
                            {
                                dt=dt.AddMonths(1);
                            }

                            sr.TimeStart=DateTime.Parse(day+"/"+ dt.Date.Month +"/"+ dt.Date.Year+" "+ ev.LastElementChild.Children[0].Children[0].TextContent).AddHours(1);
                            

                            sr.Match = ev.LastElementChild.Children[0].Children[1].TextContent;

                            if (sr.Match.Contains("угловые") || sr.Match.Contains("карточки"))
                                continue;
                            var teams = sr.Match.Replace(" - ", "|").Split('|');
                            sr.TeamName = teams[0];
                            rezultList.Add(sr);

                            var sr1 = sr.Clone();
                            sr1.TeamName = teams[1];
                            rezultList.Add(sr1);
                        }
                    }
                }
            }

            return rezultList;
        }


        private List<SiteRow> ParseTable(IHtmlCollection<IElement> divs)
        {
            var rezultList = new List<SiteRow>();

            return rezultList;

        }
    }
}
