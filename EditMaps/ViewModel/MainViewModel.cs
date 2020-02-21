using Microsoft.Win32;
using StaticData.Parsers.Fonbet;
using StaticData.Parsers.Marafon;
using StaticData.Parsers.Olimp;
using StaticData.Parsers.PariMatch;
using StaticData.Parsers.Zenit;
using StaticData.Shared.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EditMaps.ViewModel
{
    internal class MainViewModel : BaseViewModel
    {
        private readonly string[] _urls;

        public MainViewModel()
        {
            LoadCommand = new ReallyCommand(Load);
            JoiningCommand = new ReallyCommand(Joining);
            ImportDataCommand = new ReallyCommand(ImportData);
            _urls = File.ReadAllLines("mirrors.txt");
        }

        private bool _isLoad;
        public bool IsLoad
        {
            get { return _isLoad; }
            set
            {
                if (_isLoad == value) return;
                _isLoad = value;
                PropChanged();
            }
        }

        public ReallyCommand LoadCommand { get; set; }
        public ReallyCommand JoiningCommand { get; set; }
        public ReallyCommand ImportDataCommand { get; set; }

        public ObservableCollection<string> Loger { get; } = new ObservableCollection<string>();


        private void Load()
        {
            IsLoad = true;
            Task.Factory.StartNew(LoadAsync);
        }

        private void LoadAsync()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
#if !DEBUG
            try
            {
#endif

                Marafon betm = new Marafon(_urls[0]);

                List<SiteRow> sm = betm.ParseAnonsLive();
                DateTime ser = DateTime.UtcNow;
                if (ser.AddHours(3).Day != ser.Day)
                {
                    List<SiteRow> s1 = betm.ParseAnonsLive(DateTime.Now.AddDays(-1));
                    sm.AddRange(s1);
                }

                SiteRow.Save("Marafon.data", sm);
                Loger.Add($"Marafon загружен. количество: {sm.Count}");
#if !DEBUG
            }
            catch (Exception ex)
            {
                Loger.Add($"При загрузке данных произошла ошибка: {ex.Message}");

            }


            try
            {
#endif
                Fonbet betf = new Fonbet(_urls[1]);
                List<SiteRow> sf = betf.ParseAnonsLive();
                SiteRow.Save("Fonbet.data", sf);
                Loger.Add($"Fonbet загружен. количество: {sf.Count}");
#if !DEBUG

            }
            catch (Exception ex)
            {
                Loger.Add($"При загрузке данных произошла ошибка: {ex.Message}");

            }

            try
            {
#endif
                Olimp beto = new Olimp(_urls[2]);
                List<SiteRow> so = beto.ParseAnonsLive();
                SiteRow.Save("Olimp.data", so);
                Loger.Add($"Olimp загружен. количество: {so.Count}");
#if !DEBUG
            }
            catch (Exception ex)
            {

                Loger.Add($"При загрузке данных произошла ошибка: {ex.Message}");

            }
#endif
            try
            {
                Zenit bet = new Zenit(_urls[3]);
                List<SiteRow> s = bet.ParseAnonsLive();
                SiteRow.Save("Zenit.data", s);
                Loger.Add($"Zenit загружен. количество: {s.Count}");
            }
            catch (Exception ex)
            {
                Loger.Add($"При загрузке данных произошла ошибка: {ex.Message}");

            }

            try
            {
                PariMatch bet = new PariMatch(_urls[4]);
                List<SiteRow> s = bet.ParseAnonsLive();
                SiteRow.Save("PariMatch.data", s);
                Loger.Add($"PariMatch загружен. количество: {s.Count}");
            }
            catch (Exception ex)
            {
                Loger.Add($"При загрузке данных произошла ошибка: {ex.Message}");

            }



            IsLoad = false;


        }

        private static void Joining()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Укажите базу для добавления",
                Multiselect = false,
                Filter = "Файл базы данных(*.data)|*.data"
            };
            dlg.ShowDialog();

            if (string.IsNullOrEmpty(dlg.FileName))
                return;

            List<UnicData> bd = UnicData.Load("bd_new.data");
            List<UnicData> newBdOne = UnicData.Load(dlg.FileName);
            List<UnicData> newBdTwo = UnicData.Load("bd.data");

            List<UnicData> x1 = newBdOne.Except(bd, new Comp()).ToList();
            List<UnicData> x2 = newBdTwo.Except(bd, new Comp()).ToList();


            int last = bd.Count;
            foreach (UnicData key in x1)
            {
                List<UnicData> dt = newBdOne.Where(x => x.Id == key.Id).ToList();

                int mainId = bd.Select(x => x.Id).Max();
                mainId++;
                foreach (UnicData mt in dt)
                {
                    UnicData tmp = mt.Clone();
                    tmp.Id = mainId;
                    bd.Add(tmp);
                }
            }


            foreach (UnicData key in x2)
            {
                List<UnicData> dt = newBdTwo.Where(x => x.Id == key.Id).ToList();
                int mainId = bd.Select(x => x.Id).Max();
                mainId++;
                foreach (UnicData mt in dt)
                {
                    UnicData tmp = mt.Clone();
                    tmp.Id = mainId;
                    bd.Add(tmp);
                }
            }

            MessageBox.Show($"Успешно добавленно {bd.Count - last} команд");

            UnicData.Save("bd_new.data", bd);
            UnicData.Save("bd.data", bd);

        }

        private void ImportData()
        {
            if (!File.Exists("bd.data"))
                return;

            List<UnicData> db = UnicData.Load("bd.data");
            List<UnicData> rezultList = new List<UnicData>();
            List<string> filesData = new List<string>()
            {
                "Fonbet.data",
                "Marafon.data",
                "Olimp.data",
                "Zenit.data"
            };

            foreach (string fileName in filesData)
            {
                List<SiteRow> data = SiteRow.Load(fileName);
                foreach (SiteRow siteRow in data)
                {
                    UnicData rez = FindData(siteRow.TeamName, db);
                    if (rez != null)
                    {
                        var team2 = siteRow.Match.Replace(siteRow.TeamName, "").Replace(" - ", "").Trim();
                        UnicData rez2 = FindData(siteRow.TeamName, db);
                        if (rez2 != null)
                            rezultList.Add(rez);
                    }

                }
            }

            rezultList = rezultList.OrderBy(x => x.Id).ToList();
            rezultList = ToUnic(rezultList);

            UnicData.Save("bd_Import.data", rezultList);
            Loger.Add($"База для импорта готова. В ней {rezultList.Count} записей");
        }

        private static UnicData FindData(string value, List<UnicData> db)
        {
            for (int i = 0; i < db.Count; i++)
            {
                if (db[i].Value == value)
                    return db[i];
            }
            return null;
        }



        private static List<UnicData> ToUnic(List<UnicData> db)
        {
            HashSet<UnicData> rezultList = new HashSet<UnicData>();

            for (int i = 0; i < db.Count; i++)
            {
                var count = db.Where(x => x.Id == db[i].Id).ToList();
                if (count.Count > 1)
                    rezultList.Add(db[i]);
            }

            return rezultList.ToList();
        }
    }
}
