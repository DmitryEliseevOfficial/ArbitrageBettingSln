using ABShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ABClient.Data
{
    class SettingsManger
    {
        public static Settings LoadSettingTry()
        {
            try
            {
                 return Load();
                
            }
            catch
            {
                Settings sett = new Settings();
                sett.bookmakers = new List<Bookmaker>
                {
                    new Bookmaker()
                    {
                        Name = "Зенит",
                        BkType = BookmakerType.Zenit,
                        Id = 0,
                        Url = "https://zenitbet.com",
                        IsShow = true,
                        Sports = SportTypeHelper.InitSports()
                    },
                    new Bookmaker()
                    {
                        Name = "Олимп",
                        BkType = BookmakerType.Olimp,
                        Id = 1,
                        Url = "https://olimp.com",
                        IsShow = true,
                        Sports = SportTypeHelper.InitSports()
                    },
                    new Bookmaker()
                    {
                        Name = "Фонбет",
                        BkType = BookmakerType.Fonbet,
                        Id = 2,
                        Url = "https://fonbet.com",
                        IsShow = true,
                        Sports = SportTypeHelper.InitSports()
                    },
                    new Bookmaker()
                    {
                        Name = "Марафон",
                        BkType = BookmakerType.Marafon,
                        Id = 3,
                        Url = "https://www.marathonbet.com/",
                        IsShow = true,
                        Sports = SportTypeHelper.InitSports()
                    },
                    new Bookmaker()
                    {
                        Name = "Париматч",
                        BkType = BookmakerType.Parimatch,
                        Id = 4,
                        Url = "https://www.parimatchbets2.com/",
                        IsShow = true,
                        Sports = SportTypeHelper.InitSports()
                    }
                };

                sett.Sports = SportTypeHelper.InitSports();

                return sett;
            }

           
        }

        private static Settings Load()
        {       
            var mr = new MemoryStream(File.ReadAllBytes("Settings.dat"));
            BinaryFormatter fr = new BinaryFormatter();
            return (Settings)fr.Deserialize(mr);
                  
        }


        public static void SaveSettings(Settings data)
        {
            var mr = new MemoryStream();
            BinaryFormatter fr = new BinaryFormatter();
            fr.Serialize(mr, data);
            File.WriteAllBytes("Settings.dat", mr.ToArray());
            mr.Dispose();
        }


    }
}
