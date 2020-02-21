using StaticData.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StaticData.Shared.Model
{
    [Serializable]
    public class SiteRow
    {
        public ParserType Site { get; set; }
        public string Groupe { get; set; }
        public string TeamName { get; set; }
        public DateTime TimeStart { get; set; }
        public string Sport { get; set; }
        public bool IsUsed { get; set; }

        public string Match { get; set; }


        public SiteRow Clone()
        {
            var rezult = new SiteRow();

            rezult.Sport = Sport;
            rezult.Groupe = Groupe;
            rezult.Site = Site;
            rezult.TimeStart = TimeStart;
            rezult.Match = Match;

            return rezult;
        }
        public static void Save(string name, List<SiteRow> data)
        {
            using (MemoryStream mr = new MemoryStream())
            {
                BinaryFormatter fr = new BinaryFormatter();
                fr.Serialize(mr, data);
                File.WriteAllBytes(name, mr.ToArray());
            }
        }

        public static List<SiteRow> Load(string name)
        {
            var data = File.ReadAllBytes(name);
            using (MemoryStream mr = new MemoryStream(data))
            {
                BinaryFormatter fr = new BinaryFormatter();
                return (List<SiteRow>)fr.Deserialize(mr);
            }
        }

        public static List<SiteRow> ToUnic(List<SiteRow> data)
        {
            var rezult = new List<SiteRow>();
            data = data.OrderBy(x => x.TeamName).ToList();

            foreach (var key in data)
            {
                var items = rezult.Where(x => x.TeamName == key.TeamName).ToList();
                if (items.Count == 0)
                    rezult.Add(key);
            }

            return rezult;
        }

        public static List<SiteRow> RemoveDate(List<SiteRow> data, DateTime dt)
        {
            var rezult = new List<SiteRow>();
            foreach (var dat in data)
            {
                if (dat.TimeStart >= dt)
                    rezult.Add(dat);
            }
            return rezult;
        }
    }
}
