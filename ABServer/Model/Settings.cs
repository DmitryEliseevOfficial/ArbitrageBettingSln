using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;


namespace ABServer.Model
{
    [Serializable]
    public class Settings
    {
        public bool UsingProxy { get; set; }
        public string ZenitLogin { get; set; }
        public string ZenitPassword { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public string OlimpUrl { get; set; } = "https://olimp.com/";
        public string FonbetUrl { get; set; } = "https://www.fonbet5.com/ru/";
        public string MarafonUrl { get; set; } = "https://www.marathonbet.com/su/";
        public string ZenitUrl { get; set; } = "https://zenit88.win/";
        public string PariMatchUrl { get; set; } = "https://www.parimatchbets2.com/";

        public static void Save(Settings setting)
        {
            try
            {
                var file = new FileStream("settings.dat", FileMode.Create);
                BinaryFormatter fr = new BinaryFormatter();
                fr.Serialize(file, setting);
                file.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static Settings Load()
        {
            try
            {
                var file = new FileStream("settings.dat", FileMode.Open);
                BinaryFormatter fr = new BinaryFormatter();
                var rez = (Settings)fr.Deserialize(file);
                file.Close();
                return rez;
            }
            catch
            {
                return new Settings();
            }
        }
    }
}
