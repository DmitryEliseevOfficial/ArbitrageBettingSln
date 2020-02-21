using StaticData.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace StaticData.Parsers.Zenit
{
    public class Zenit
    {
        string url;

        public Zenit(string url="https://zenit88.win")
        {
            this.url = url;
        }

        public List<SiteRow> ParseAnonsLive()
        {
            List<SiteRow> rezult = new List<SiteRow>();

            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var response = wb.DownloadString(url + "/live");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);

            var table = doc.DocumentNode.SelectSingleNode("//table[@class=\"live-index-schedule-table\"]");
            if (table == null)
                throw new ArgumentNullException($"{typeof(Zenit)}: Ошибка получения таблицы с Анонсом.");

            var rows = table.FirstChild.ChildNodes.Where(x=>x.Name=="tr").ToList();
            for(int i=0;i<rows.Count;i++)
            {
                string ligaName = rows[i].InnerText.Trim();
                string SportName = ligaName.Split('.').First();
                ligaName = ligaName.Replace(SportName + ".", "");
                while(true)
                {
                    if (i == rows.Count - 1)
                        break;
                    i++;
                    if(rows[i].Attributes["class"].Value== "live-index-schedule-row")
                    {
                        i--;
                        break; // вываливаемся из while
                    }

                    SiteRow sr = new SiteRow();
                    sr.Sport = SportName;
                    sr.Groupe = ligaName;
                    sr.Match = rows[i].ChildNodes[3].InnerText;
                    sr.TimeStart = DateTime.Parse(rows[i].ChildNodes[1].InnerText.Replace(" ","/2017 "));
                    sr.Site = Shared.Enums.ParserType.Zenit;

                    string[] teams = sr.Match.Replace(" - ", "|").Split('|');
                    sr.TeamName = teams[0];
                    rezult.Add(sr);

                    var sr1 = sr.Clone();

                    sr1.TeamName = teams[1];
                    rezult.Add(sr1);

                }

            }

            var uri = new Uri(url).Host;
            response = wb.DownloadString($"https://m.{uri}/mbzenit/ajax/live/livedata?mob-all-live");
            var json = JObject.Parse(response);
            if (json["result"]?["games"] != null)
            {
                foreach (JToken jToken in json["result"]["games"])
                {
                    SiteRow sr = new SiteRow();
                    var liga =
                        json["result"]["league"].FirstOrDefault(
                            x => x.First["id"].ToString() == jToken.First["lid"].ToString());

                    var sport =
                        json["result"]["sport"].FirstOrDefault(
                            x => x.First["id"].ToString() == liga.First["sid"]?.ToString());

                    sr.Sport = sport.First["name"].ToString().Replace("Он-лайн. ","");
                    if (string.IsNullOrWhiteSpace(sr.Sport))
                    {
                        sr.Sport = "Nan";
                    }
                    sr.Groupe = liga.First["name"].ToString();
                    sr.Match = $"{jToken.First["c1"]} - {jToken.First["c2"]}";
                    sr.TimeStart = UnixTimeStampToDateTime(Convert.ToDouble(jToken.First["time"])).AddHours(3);
                    sr.Site = Shared.Enums.ParserType.Zenit;
                    sr.TeamName = jToken.First["c1"].ToString();
                    rezult.Add(sr);

                    var sr1 = sr.Clone();
                    sr1.TeamName = jToken.First["c2"].ToString();
                    rezult.Add(sr1);
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
    }
}
