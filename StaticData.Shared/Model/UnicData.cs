using StaticData.Shared.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace StaticData.Shared.Model
{
    [Serializable]
    public class UnicData
    {
        private UnicData()
        {

        }

        public UnicData(int Id,string Value,ParserType Site)
        {
            this.Id = Id;
            this.Site = Site;
            this.Value = Value;
        }


        public int Id { get; set; }
        public string Value { get; set; }
        public ParserType Site { get; set; }


        public override string ToString()
        {
            return $"{Id}   -   {Value}";
        }
        
        public UnicData Clone()
        {
            UnicData dt = new UnicData();
            dt.Id = Id;
            dt.Site = Site;
            dt.Value = Value;

            return dt;
        }

        public override bool Equals(object obj)
        {
            var sender = obj as UnicData;

            if (sender == null)
                return false;
            if (sender.Id != this.Id)
                return false;
            if (sender.Value != this.Value)
                return false;

            return true;


        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static void Save(string name, List<UnicData> data)
        {
            using (MemoryStream mr = new MemoryStream())
            {
                BinaryFormatter fr = new BinaryFormatter();
                fr.Serialize(mr, data);
                File.WriteAllBytes(name, mr.ToArray());
            }
        }

        public static List<UnicData> Load(string name)
        {
            var data = File.ReadAllBytes(name);
            using (MemoryStream mr = new MemoryStream(data))
            {
                BinaryFormatter fr = new BinaryFormatter();
                return (List<UnicData>)fr.Deserialize(mr);
            }
        }

    }
}
