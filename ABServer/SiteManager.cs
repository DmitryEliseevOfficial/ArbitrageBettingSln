using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABShared;
using System.Runtime.Serialization.Formatters.Binary;

namespace ABServer
{
    class SiteManager
    {
        private static Dictionary<BookmakerType, HashSet<String>> _sites;
        private static bool IsLoad = false;
        private static object obj = new object();


        public SiteManager()
        {
            if (!IsLoad)
                Load();
        }

        private void Load()
        {
            FileStream str = null;
            lock (obj)
            {
                try
                {
                    BinaryFormatter fr = new BinaryFormatter();
                    str = File.OpenRead("sites.dat");
                    _sites = (Dictionary<BookmakerType, HashSet<String>>)fr.Deserialize(str);
                    str.Close();

                }
                catch
                {
                    _sites = new Dictionary<BookmakerType, HashSet<string>>();
                    _sites.Add(BookmakerType.Fonbet, new HashSet<string>());
                    _sites.Add(BookmakerType.Marafon, new HashSet<string>());
                    _sites.Add(BookmakerType.Olimp, new HashSet<string>());
                    _sites.Add(BookmakerType.Zenit, new HashSet<string>());
                    if (str != null)
                        str.Close();
                    return;
                }

                IsLoad = true;
            }
        }

        private void Save()
        {
            try
            {
                lock (obj)
                {
                    using (var mr = new MemoryStream())
                    {
                        BinaryFormatter fr = new BinaryFormatter();
                        fr.Serialize(mr, _sites);
                        File.WriteAllBytes("sites.dat", mr.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Не удалось сохранить базу сайтов: " + ex.Message);
            }
        }

        public List<string> GetData(BookmakerType bookmaker)
        {
            return _sites[bookmaker].ToList();
        }

        public void Add(BookmakerType bookmaker, string site)
        {
            _sites[bookmaker].Add(site);
            Save();
        }

    }
}
