using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using Newtonsoft.Json.Linq;
using StaticData.Shared.Enums;
using StaticData.Shared.Model;

namespace StaticData.Parsers.Marafon
{
    public class Marafon
    {
        string url;

        public Marafon(string url= "https://www.marathonbet.com")
        {
            this.url = url;
        }

        public List<SiteRow> parseCurentLive()
        {
            List<string> targets = new List<string>();           
            //targets.Add("/su/betting/Badminton/?menu=382581");
            targets.Add("/su/betting/Basketball/?menu=6");
            targets.Add("/su/betting/Baseball/?menu=5");
            targets.Add("/su/betting/Boxing/?menu=7");
            targets.Add("/su/betting/Volleyball/?menu=22712");
            targets.Add("/su/betting/Handball/?menu=52914");
            targets.Add("/su/betting/Golf/?menu=10");            
            targets.Add("/su/betting/e-Sports/?menu=1895085");
            targets.Add("/su/betting/Snooker/?menu=2185");
            targets.Add("/su/betting/Tennis/?menu=2398");            
            targets.Add("/su/betting/Football/?menu=11"); 
            targets.Add("/su/betting/Ice+Hockey/?menu=537");


            var rezult = new List<SiteRow>();


            foreach(var target in targets)
            {
                rezult.AddRange(parsePage("https://www.marathonbet.com" + target));
            }
           

            return rezult;
        }

        private List<SiteRow> parsePage(string link)
        {
            var rezult = new List<SiteRow>();

            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var respone = wb.DownloadString(link);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respone);

            var name_node = doc.DocumentNode.SelectSingleNode("//a[@class=\"sport-category-label\"]");

            var blocks = doc.DocumentNode.SelectNodes("//div[@class=\"category-container\"]").ToList();
            
            foreach(var block in blocks)
            {
                string groupe = block.ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[3].InnerText.Trim();

                var teams = block.ChildNodes[3].ChildNodes[3].ChildNodes[1].ChildNodes;

                foreach(var row in teams)
                {
                    if(row.Name== "tbody" && row.Attributes["data-event-treeid"]!=null)
                    {
                        var current=row.ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes;
                        SiteRow rw = new SiteRow();
                        rw.Sport = name_node.InnerText.Trim();
                        rw.Groupe = groupe;
                        rw.Site = Shared.Enums.ParserType.Marafon;
                        var dt=current[1].ChildNodes[3].InnerText.Trim();
                        if (dt.Contains("ноя"))
                            dt = dt.Replace(" ноя", ".11.2016");
                        else if (dt.Contains("дек"))
                            dt = dt.Replace(" дек", ".12.2016");
                        else if (dt.Contains("янв"))
                            dt = dt.Replace(" янв ", ".01.");
                        else if (dt.Contains("фев"))
                            dt = dt.Replace(" фев ", ".01.");
                        else
                        {
                            
                        }
                        rw.TimeStart = DateTime.Parse(dt);
                        rw.TeamName = current[1].ChildNodes[1].ChildNodes[1].ChildNodes[3].InnerText.Trim();
                        
                        rezult.Add(rw);

                        var rw1 = rw.Clone();
                        rw1.TeamName= current[3].ChildNodes[1].ChildNodes[1].ChildNodes[3].InnerText.Trim();
                        rezult.Add(rw1);
                    }
                }
            }

            return rezult;
        }

        List<SiteRow> _rezult = new List<SiteRow>();
        public List<SiteRow> ParseAnonsLive()
        {
            _rezult.Clear();

                ParseCurrentLives();

            _rezult.AddRange(ParseAnonsLive(DateTime.Now.ToUniversalTime().AddHours(3)));
            if((DateTime.Now.ToUniversalTime().AddHours(3)- DateTime.Now.ToUniversalTime().AddDays(1).AddHours(3)).Days!=0)
                _rezult.AddRange(ParseAnonsLive(DateTime.Now.ToUniversalTime().AddDays(1).AddHours(3)));

            return _rezult;
        }

        public List<SiteRow> ParseAnonsLive(DateTime date)
        {
            var rezult = new List<SiteRow>();

            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var uri = $"{url}/su/live/announces?form_name=form&calendar=" + date.ToString("dd.MM.yyyy") + "&announce_sort=BY_TIME&sport_select=ALL&pageAction=default";
            var response = wb.DownloadString(uri);
            var json = JArray.Parse(response);

            HtmlDocument doc = new HtmlDocument();
            if (json.First["content"] == null)
                return new List<SiteRow>();
            doc.LoadHtml(json.First["content"].ToString());

            var nodes = doc.DocumentNode.SelectNodes("//tr[@class=\"announce\"]");

            foreach(var key in nodes)
            {
                var t = key.ChildNodes[1].ChildNodes[1].ChildNodes;

                var rw = new SiteRow();
                var dt = t[1].InnerText.Trim();
                if (dt.Contains("ноя"))
                    dt = dt.Replace(" ноя", ".11.2017 ");
                else if (dt.Contains("дек"))
                    dt = dt.Replace(" дек", ".12.2017 ");
                else if (dt.Contains("янв"))
                    dt = dt.Replace(" янв ", ".01.2017 ");
                else if (dt.Contains("фев"))
                    dt = dt.Replace(" фев ", ".02.2017 ");
                else if (dt.Contains("мар"))
                    dt = dt.Replace(" мар ", ".03.2017 ");
                else if (dt.Contains("апр"))
                    dt = dt.Replace(" апр ", ".04.2017 ");
                else if (dt.Contains("май"))
                    dt = dt.Replace(" май ", ".05.2017 ");
                else
                {

                }
                rw.TimeStart = DateTime.Parse(dt);

                rw.Site = Shared.Enums.ParserType.Marafon;
                rw.Sport = t[3].InnerText.Split('.').First().Trim();
                var teams = t[3].ChildNodes[1].InnerText.Replace("&nbsp;","").Replace(" - ", "|").Split('|');
               
                if(teams.Count()!=2)
                {
                    teams = t[3].ChildNodes[1].InnerText.Replace("&nbsp;", "").Replace(" @ ", "|").Split('|');
                }
                if (teams.Length != 2)
                    continue;

                rw.Groupe = t[3].ChildNodes[0].InnerText.Replace(rw.Sport + ". ","").Trim();
                rw.TeamName = teams[0];

                rw.Match = $"{rw.TeamName} - {teams[1]}";

                rezult.Add(rw);

                var rw1 = rw.Clone();
                rw1.TeamName = teams[1];
                rezult.Add(rw1);



            }

            
            return rezult;
        }

        private void ParseCurrentLives()
        {
            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var respone = wb.DownloadString($"{url}/su/live/popular");
            var parser = new HtmlParser();
            var doc = parser.Parse(respone);

            var mainContainer = doc.GetElementById("container_AVAILABLE");
            foreach (IElement child in mainContainer.Children)
            {
                if (child.ChildElementCount <2)
                    continue;

                foreach (IElement liga in child.Children[1].Children)
                { 

                    if (liga.ChildElementCount == 0)
                        continue;
                    if (liga.Children[1].ChildElementCount == 0)
                        continue;

                    foreach (IElement ev in liga.Children[1].Children)
                    {
                        string[] teams;
                        SiteRow rw = new SiteRow();
                        rw.Site = ParserType.Marafon;
                        try
                        {
                            rw.Sport = child.Children[0].TextContent.Trim();
                            rw.Groupe = liga.Children[0].TextContent.Trim();
                            rw.Match = ev.Children[2].TextContent.Replace(" @ ", " - ").Trim();
                            teams = rw.Match.Replace(" - ", "|").Split('|');
                            if (teams.Length != 2)
                                continue;
                            rw.TeamName = teams[0];
                        }
                        catch
                        {
                            continue;
                        }

                        _rezult.Add(rw);

                        var rw1 = rw.Clone();
                        rw1.TeamName = teams[1];
                        _rezult.Add(rw1);

                    }               

                   
                }
            }
        }
    }
}
