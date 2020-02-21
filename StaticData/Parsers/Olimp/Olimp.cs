using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using StaticData.Shared.Model;

namespace StaticData.Parsers.Olimp
{
    public class Olimp
    {
        string url;

        public Olimp(string url = "https://olimp.com")
        {
            this.url = url;
        }

        public List<SiteRow> parseCurentLive()
        {
            var rezult = new List<SiteRow>();

            WebClient wb = new WebClient();

            wb.Encoding = Encoding.UTF8;
            var respone = wb.DownloadString($"{url}/index.php?page=line&action=1");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);
            var links = doc.DocumentNode.SelectNodes("//a").Where(x => x.Attributes["href"] != null && x.Attributes["href"].Value.ToString().Contains("page=line&action=2&sel")).ToList();

            foreach (var link in links)
            {
                Console.WriteLine(link.Attributes["href"].Value);
                rezult.AddRange(ParsePage(link.Attributes["href"].Value, link.InnerText.Trim()));
            }

            return rezult;
        }

        private List<SiteRow> ParsePage(string link, string Groupe)
        {
            List<SiteRow> rezult = new List<SiteRow>();

            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var respone = wb.DownloadString("https://olimp.com/" + link);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);

            var trs = doc.DocumentNode.SelectNodes("//tr[@class=\"hi\"]");
            if (trs == null)
            {
                //если нету в этой части ставок
                return new List<SiteRow>();
            }
            foreach (var tr in trs)
            {
                if (tr.ChildNodes[3].Attributes["width"] == null)
                    continue;

                SiteRow sr = new SiteRow();
                sr.TimeStart = DateTime.Parse(tr.ChildNodes[1].InnerText);
                sr.Groupe = Groupe;
                sr.Site = Shared.Enums.ParserType.Olimp;


                string[] teams = tr.ChildNodes[3].InnerText.Trim().Replace(" - ", "|").Split('|');
                if (teams.Length != 2)
                {
                    continue;
                }
                sr.TeamName = teams[0].Trim();

                rezult.Add(sr);

                var sr1 = sr.Clone();
                sr1.TeamName = teams[1].Trim();

                rezult.Add(sr1);

            }


            return rezult;
        }



        public List<SiteRow> ParseAnonsLive()
        {
            var rezult = new List<SiteRow>();

            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var respone = wb.DownloadString($"{url}/betting");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);
            var rows = doc.DocumentNode.SelectNodes("//tr").Where(x => x.Id != null && x.Id.Contains("t") && x.Id.ToString().Count() <= 4).ToList();

            foreach (var row in rows)
            {
                int i = 3;
                var x = row.ChildNodes[1].ChildNodes.Count;
                if (x != 5)
                {
                    i = 5;
                }


                var rw = new SiteRow();
                rw.Site = Shared.Enums.ParserType.Olimp;
                rw.Sport = row.ChildNodes[1].ChildNodes[3].InnerText.Split('.').First().Trim();
                var teams = row.ChildNodes[1].ChildNodes[3].InnerText.Replace(rw.Sport + ". ", "").Replace(" - ", "|").Split('|');
                rw.TimeStart = DateTime.Parse(row.ChildNodes[1].ChildNodes[8].ChildNodes[0].InnerText.Replace("Начало ", "").Trim());
                rw.TeamName = teams[0];
                rw.Groupe = "";

                rw.Match = $"{rw.TeamName} - {teams[1]}";
                rezult.Add(rw);

                var rw1 = rw.Clone();
                rw1.TeamName = teams[1];
                rezult.Add(rw1);

            }

            //Получаем Live
            var uri = new Uri(url).Host;
            respone = wb.DownloadString($"http://olimpapi.cc/api/v3/app_api_index.php?action=slice&live=1&time_shift=0&lang_id=0&platforma=site-api&_=0");

            var json = JObject.Parse(respone);

            if (json["data"] != null)
            {
                foreach (JToken token in json["data"])
                {
                    if (token["cn"].ToString().Contains("Статистика"))
                        continue;

                    foreach (JToken ev in token["it"])
                    {
                        var rw = new SiteRow();
                        rw.Site = Shared.Enums.ParserType.Olimp;
                        rw.Sport = token["sn"].ToString();
                        rw.Groupe = "";
                        rw.TeamName = ev["c1"].ToString();
                        rw.Match = ev["n"].ToString();
                        rw.TimeStart = UnixTimeStampToDateTime(Convert.ToDouble(ev["t"])).AddHours(3);
                        rezult.Add(rw);
                        var rw1 = rw.Clone();
                        rw1.TeamName = ev["c2"].ToString();

                        rezult.Add(rw1);

                    }
                }
            }

            return rezult;
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        private List<string> GetAllGroupe()
        {
            return new List<string>();

        }
    }
}
