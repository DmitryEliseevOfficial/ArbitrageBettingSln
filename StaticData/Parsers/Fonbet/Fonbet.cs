using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net;
using StaticData.Shared.Model;
using StaticData.Shared.Enums;

namespace StaticData.Parsers.Fonbet
{
    public class Fonbet
    {
        private readonly string _url;



        public Fonbet(string url = "Https://fonbet5.com")
        {
            _url = url;
        }

        public class GZipWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                return request;
            }
        }

        public List<SiteRow> ParseAnonsLive()
        {
            string adress = GetApiAdress();

            WebClient wb = new GZipWebClient();
            wb.Encoding = Encoding.UTF8;
            var respone = wb.DownloadString($"https:{adress}/line/currentLine/ru/");

            var json = JObject.Parse(respone);

            var rezult = new List<SiteRow>();
            var events = json["events"].Where(x => x["level"].ToString() == "1").ToList();

            foreach (var ev in events)
            {
                if (ev["state"]?["willBeLive"]?.ToString() == "True")
                {

                }
                else
                {
                    continue;
                }
                SiteRow rw = new SiteRow();
                rw.Site = ParserType.Fonbet;
                rw.TeamName = ev["team1"].ToString();
                rw.TimeStart = UnixTimeStampToDateTime(SetDouble(ev["startTime"].ToString())).AddHours(3);
                var sport = json["sports"].Where(x => x["id"].ToString() == ev["sportId"].ToString()).ToList().First();
                rw.Groupe = sport["name"].ToString();

                rw.Sport = rw.Groupe.Split('.').First().Trim();
                rw.Match = $"{rw.TeamName} - {ev["team2"]}";

                rezult.Add(rw);

                var rw2 = rw.Clone();
                rw2.TeamName = ev["team2"].ToString();


                rezult.Add(rw2);
            }

            wb = new GZipWebClient();
            respone = wb.DownloadString($"https:{adress}/live/currentLine/ru/");
            json = JObject.Parse(respone);
            events = json["events"].Where(x => x["level"].ToString() == "1").ToList();
            foreach (JToken ev in events)
            {
                SiteRow rw = new SiteRow();
                rw.Site = ParserType.Fonbet;
                rw.TeamName = ev["team1"].ToString();
                rw.TimeStart = UnixTimeStampToDateTime(SetDouble(ev["startTime"].ToString())).AddHours(3);
                var sport = json["sports"].Where(x => x["id"].ToString() == ev["sportId"].ToString()).ToList().First();
                rw.Groupe = sport["name"].ToString();

                rw.Sport = rw.Groupe.Split('.').First().Trim();
                rw.Match = $"{rw.TeamName} - {ev["team2"]}";

                rezult.Add(rw);

                var rw2 = rw.Clone();
                rw2.TeamName = ev["team2"].ToString();


                rezult.Add(rw2);
            }


            rezult = rezult.OrderBy(x => x.TimeStart).ToList();
            return rezult;
        }


        private string GetApiAdress()
        {
            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var respone = wb.DownloadString($"{_url}/urls.json");
            var json = JObject.Parse(respone);
            return json["line"].Last().ToString();
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        private static double SetDouble(string data)
        {
            double rez;
            double.TryParse(data, out rez);
            return rez;
        }
    }
}
