using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StaticData.Parsers.Fonbet;
using StaticData.Parsers.Olimp;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using StaticData.Parsers.Marafon;
using StaticData.Shared.Model;

namespace StaticData
{
    class Program
    {
        static void Main(string[] args)
        {

            var bet = new Fonbet();

            var s = bet.ParseAnonsLive();

            

            Save("Marafon.data", s);
        }

        static List<SiteRow> RemoveDate(List<SiteRow> data,DateTime dt)
        {
            var rezult = new List<SiteRow>();
            foreach(var dat in data)
            {
                if (dat.TimeStart >= dt)
                    rezult.Add(dat);
            }

            return rezult;
        }

        static List<SiteRow> ToUnic(List<SiteRow>  data)
        {
            var rezult = new List<SiteRow>();
            data = data.OrderBy(x => x.TeamName).ToList();

            foreach(var key in data)
            {
                var items = rezult.Where(x => x.TeamName == key.TeamName).ToList();
                if (items.Count == 0)
                    rezult.Add(key);
            }

            return rezult;
        }

        static void Save(string name, List<SiteRow> data)
        {
            using (MemoryStream mr = new MemoryStream())
            {
                BinaryFormatter fr = new BinaryFormatter();
                fr.Serialize(mr, data);
                File.WriteAllBytes(name, mr.ToArray());
            }            
        }

        static List<SiteRow> Load(string name)
        {
            var data =File.ReadAllBytes(name);
            using (MemoryStream mr = new MemoryStream(data))
            {
                BinaryFormatter fr = new BinaryFormatter();
                return (List<SiteRow>)fr.Deserialize(mr);               
            }
        
            
        }
    }
}
