using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using StaticData.Shared.Model;
using System.Windows;

namespace EditMaps.ViewModel
{
    internal class CreateViewModel : BaseViewModel
    {
        public event Action UpdateBase;

        private List<SiteRow> FonbetData { get; set; }
        private List<SiteRow> OlimpData { get; set; }
        private List<SiteRow> MarafonData { get; set; }
        private List<SiteRow> ZenitData { get; set; }
        private List<SiteRow> PariMatchData { get; set; }


        #region Коллекции для отображения
        ObservableCollection<SiteRow> _fonbet;
        public ObservableCollection<SiteRow> Fonbet
        {
            get { return _fonbet; }
            set
            {
                if (_fonbet == value) return;
                _fonbet = value;
                PropChanged();
            }
        }

        private ObservableCollection<SiteRow> _olimp;
        public ObservableCollection<SiteRow> Olimp
        {
            get { return _olimp; }
            set
            {
                if (_olimp == value) return;
                _olimp = value;
                PropChanged();
            }
        }

        private ObservableCollection<SiteRow> _marafon;
        public ObservableCollection<SiteRow> Marafon
        {
            get { return _marafon; }
            set
            {
                if (_marafon == value) return;
                _marafon = value;
                PropChanged();
            }
        }

        private ObservableCollection<SiteRow> _zenit;
        public ObservableCollection<SiteRow> Zenit
        {
            get { return _zenit; }
            set
            {
                if (_zenit == value) return;
                _zenit = value;
                PropChanged();
            }
        }

        private ObservableCollection<SiteRow> _pariMatch;
        public ObservableCollection<SiteRow> PariMatch
        {
            get { return _pariMatch; }
            set
            {
                if (_pariMatch == value) return;
                _pariMatch = value;
                PropChanged();
            }
        }

        private ObservableCollection<UnicData> _base;
        public ObservableCollection<UnicData> Base
        {
            get { return _base; }
            set
            {
                if (_base == value) return;
                _base = value;
                PropChanged();
            }

        }
        #endregion

        #region Поля для Биндинга выделений в Listbox
        public UnicData DataSelected { get; set; }


        private SiteRow _fonbetSelected;
        public SiteRow FonbetSelected
        {
            get { return _fonbetSelected; }
            set
            {
                if (_fonbetSelected == value) return;
                _fonbetSelected = value;
                PropChanged();
            }
        }

        private SiteRow _olimpSelected;
        public SiteRow OlimpSelected
        {
            get { return _olimpSelected; }
            set
            {
                if (_olimpSelected == value) return;
                _olimpSelected = value;
                PropChanged();
            }
        }

        private SiteRow _marafonSelected;
        public SiteRow MarafonSelected
        {
            get { return _marafonSelected; }
            set
            {
                if (_marafonSelected == value) return;
                _marafonSelected = value;
                PropChanged();
            }
        }

        private SiteRow _zenitSelected;
        public SiteRow ZenitSelected
        {
            get { return _zenitSelected; }
            set
            {
                if (_zenitSelected == value) return;
                _zenitSelected = value;
                PropChanged();
            }
        }

        private SiteRow _pariMacthSelected;
        public SiteRow PariMacthSelected
        {
            get { return _pariMacthSelected; }
            set
            {
                if (_pariMacthSelected == value) return;
                _pariMacthSelected = value;
                PropChanged();
            }
        }
        #endregion

        #region Команды
        public ReallyCommand SortCommand { get; set; }
        public ReallyCommand AddingCommand { get; set; }

        public ReallyCommand FonbetSkipCommand { get; set; }
        public ReallyCommand OlimpSkipCommand { get; set; }
        public ReallyCommand MarafonSkipCommand { get; set; }
        public ReallyCommand ZenitSkipCommand { get; set; }
        public ReallyCommand PariMatchSkipCommand { get; set; }


        public ReallyCommand AddCommand { get; set; }
        public ReallyCommand EditCommand { get; set; }
        public ReallyCommand ClearCommand { get; set; }
        #endregion

        #region Поля для биндинга с нижними текстбоксами, для редактирования
        public int NewId { get; set; }

        private int _fonbetId;
        public int FonbetId
        {
            get { return _fonbetId; }
            set
            {
                if (_fonbetId == value) return;
                _fonbetId = value;
                PropChanged();
            }
        }


        private int _olimpId;
        public int OlimpId
        {
            get { return _olimpId; }
            set
            {
                if (_olimpId == value) return;
                _olimpId = value;
                PropChanged();
            }
        }

        private int _marafonId;
        public int MarafonId
        {
            get { return _marafonId; }
            set
            {
                if (_marafonId == value) return;
                _marafonId = value;
                PropChanged();
            }
        }

        private int _zenitId;
        public int ZenitId
        {
            get { return _zenitId; }
            set
            {
                if (_zenitId == value) return;
                _zenitId = value;
                PropChanged();
            }
        }

        private int _pariMatchId;
        public int PariMatchId
        {
            get { return _pariMatchId; }
            set
            {
                if (_pariMatchId == value) return;
                _pariMatchId = value;
                PropChanged();
            }
        }
        #endregion

        private string _sortText = "";
        public string SortText
        {
            get { return _sortText; }
            set
            {
                if (_sortText == value) return;
                _sortText = value;
                PropChanged();
            }
        }

        public CreateViewModel()
        {
            Task.Factory.StartNew(LoadData);

            SortCommand = new ReallyCommand(Sort);
            AddingCommand = new ReallyCommand(Adding);

            FonbetSkipCommand = new ReallyCommand(FonbetSkip);
            OlimpSkipCommand = new ReallyCommand(OlimpSkip);
            MarafonSkipCommand = new ReallyCommand(MarafonSkip);
            ZenitSkipCommand = new ReallyCommand(ZenitSkip);
            PariMatchSkipCommand = new ReallyCommand(PariMatchSkip);

            AddCommand = new ReallyCommand(Add);
            EditCommand = new ReallyCommand(Edit);
            ClearCommand = new ReallyCommand(Clear);
        }

        private void LoadData()
        {
            try
            {
                FonbetData = SiteRow.Load("Fonbet.data");
                OlimpData = SiteRow.Load("Olimp.data");
                MarafonData = SiteRow.Load("Marafon.data");
                ZenitData = SiteRow.Load("Zenit.data");
                PariMatchData = SiteRow.Load("PariMatch.data");
                Base = new ObservableCollection<UnicData>(UnicData.Load("bd.data").OrderBy(x => x.Id));
            }
            catch
            {
                Base = new ObservableCollection<UnicData>();
            }

            Base.CollectionChanged += Base_CollectionChanged;

            if (Base.Count != 0)
            {
                foreach (UnicData key in Base)
                {
                    foreach (SiteRow t in FonbetData)
                    {
                        if (t.TeamName == key.Value)
                        {
                            t.IsUsed = true;
                        }
                    }

                    foreach (SiteRow t in OlimpData)
                    {
                        if (t.TeamName == key.Value)
                        {
                            t.IsUsed = true;
                        }
                    }

                    foreach (SiteRow t in MarafonData)
                    {
                        if (t.TeamName == key.Value)
                        {
                            t.IsUsed = true;
                        }
                    }

                    foreach (SiteRow t in ZenitData)
                    {
                        if (t.TeamName == key.Value)
                        {
                            t.IsUsed = true;
                        }
                    }

                    foreach (SiteRow t in PariMatchData)
                    {
                        if (t.TeamName == key.Value)
                        {
                            t.IsUsed = true;
                        }
                    }
                }
            }

            Fonbet = new ObservableCollection<SiteRow>(FonbetData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
            Olimp = new ObservableCollection<SiteRow>(OlimpData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
            Marafon = new ObservableCollection<SiteRow>(MarafonData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
            Zenit = new ObservableCollection<SiteRow>(ZenitData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
            PariMatch = new ObservableCollection<SiteRow>(PariMatchData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));

        }

        private void Base_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateBase?.Invoke();
        }

        #region Реализация команд

        //Редактирует ID в базе, при ошибке
        private void Edit()
        {
            UnicData tmp = DataSelected;
            if (tmp == null)
            {
                MessageBox.Show("Выбирете, что-нибудь");
                return;
            }

            for (int i = 0; i < Base.Count; i++)
            {
                if (Base[i].Value == tmp.Value)
                {
                    Base.Remove(DataSelected);
                    tmp.Id = NewId;
                    Base.Add(tmp);
                    break;
                }
            }
        }

        //Производит сортировку
        private void Sort()
        {
            if (SortText != "")
            {
                Fonbet = new ObservableCollection<SiteRow>(FonbetData.Where(x => x.Sport.Contains(SortText)).OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                Olimp = new ObservableCollection<SiteRow>(OlimpData.Where(x => x.Sport.Contains(SortText)).OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                Marafon = new ObservableCollection<SiteRow>(MarafonData.Where(x => x.Sport.Contains(SortText)).OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                Zenit = new ObservableCollection<SiteRow>(ZenitData.Where(x => x.Sport.Contains(SortText)).OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                PariMatch = new ObservableCollection<SiteRow>(PariMatchData.Where(x => x.Sport.Contains(SortText)).OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));

            }
            else
            {
                Fonbet = new ObservableCollection<SiteRow>(FonbetData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                Olimp = new ObservableCollection<SiteRow>(OlimpData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                Marafon = new ObservableCollection<SiteRow>(MarafonData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                Zenit = new ObservableCollection<SiteRow>(ZenitData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));
                PariMatch = new ObservableCollection<SiteRow>(PariMatchData.OrderBy(x => x.Sport).ThenBy(x => x.TimeStart));

            }

        }

        //Делет добавление в базу выделенных элементов
        private void Adding()
        {
            int id = 1;
            if (Base.Count != 0)
            {
                id = Base.Select(x => x.Id).Max();
                id++;
            }

            //Это для работы базы с чистого листа
            if (id != 1)
            {
                //Проверяем есть ли такой элемент уже в базе.
                int temp = 0;
                if (FonbetSelected != null)
                {
                    temp = CheckValue(FonbetSelected.TeamName);
                }
                if (OlimpSelected != null && temp == 0)
                {
                    temp = CheckValue(OlimpSelected.TeamName);
                }
                if (MarafonSelected != null && temp == 0)
                {
                    temp = CheckValue(MarafonSelected.TeamName);
                }
                if (ZenitSelected != null && temp == 0)
                {
                    temp = CheckValue(ZenitSelected.TeamName);
                }
                if (PariMacthSelected != null && temp == 0)
                {
                    temp = CheckValue(PariMacthSelected.TeamName);
                }
                if (temp != 0)
                    id = temp;
            }

            //Создаем записи для нужных элементов
            if (FonbetSelected != null)
                Base.Add(new UnicData(id, FonbetSelected.TeamName, StaticData.Shared.Enums.ParserType.Fonbet));
            if (OlimpSelected != null)
                Base.Add(new UnicData(id, OlimpSelected.TeamName, StaticData.Shared.Enums.ParserType.Olimp));
            if (MarafonSelected != null)
                Base.Add(new UnicData(id, MarafonSelected.TeamName, StaticData.Shared.Enums.ParserType.Marafon));
            if (ZenitSelected != null)
                Base.Add(new UnicData(id, ZenitSelected.TeamName, StaticData.Shared.Enums.ParserType.Zenit));
            if (PariMacthSelected != null)
                Base.Add(new UnicData(id, PariMacthSelected.TeamName, StaticData.Shared.Enums.ParserType.PariMatch));

            //Удаляем из источника данных
            FonbetData.Remove(FonbetSelected);
            OlimpData.Remove(OlimpSelected);
            MarafonData.Remove(MarafonSelected);
            ZenitData.Remove(ZenitSelected);
            PariMatchData.Remove(PariMacthSelected);

            //Удаляем из отображаемой коллекции
            Fonbet.Remove(FonbetSelected);
            Olimp.Remove(OlimpSelected);
            Marafon.Remove(MarafonSelected);
            Zenit.Remove(ZenitSelected);
            PariMatch.Remove(PariMacthSelected);

            Clear();
        }

        //Сбрасывает любое выделение
        private void Clear()
        {
            //обнуляем данные
            FonbetSelected = null;
            OlimpSelected = null;
            MarafonSelected = null;
            ZenitSelected = null;
            PariMacthSelected = null;
        }

        //Добавляем данные из нижнего списка
        private void Add(object id)
        {
            string siteName = id.ToString();

            UnicData dt = null;
            if (siteName == "Fonbet")
            {

                if (FonbetSelected == null)
                {
                    MessageBox.Show("Выбирете, что-нибудь");
                    return;
                }
                dt = new UnicData(FonbetId, FonbetSelected.TeamName, StaticData.Shared.Enums.ParserType.Fonbet);
                FonbetData.Remove(FonbetSelected);
                Fonbet.Remove(FonbetSelected);
            }

            else if (siteName == "Olimp")
            {
                if (OlimpSelected == null)
                {
                    MessageBox.Show("Выбирете, что-нибудь");
                    return;
                }

                dt = new UnicData(OlimpId, OlimpSelected.TeamName, StaticData.Shared.Enums.ParserType.Olimp);
                OlimpData.Remove(OlimpSelected);
                Olimp.Remove(OlimpSelected);
            }

            else if (siteName == "Marafon")
            {
                if (MarafonSelected == null)
                {
                    MessageBox.Show("Выбирете, что-нибудь");
                    return;
                }

                dt = new UnicData(MarafonId, MarafonSelected.TeamName, StaticData.Shared.Enums.ParserType.Marafon);
                MarafonData.Remove(MarafonSelected);
                Marafon.Remove(MarafonSelected);
            }
            else if (siteName == "Zenit")
            {
                if (ZenitSelected == null)
                {
                    MessageBox.Show("Выбирете, что-нибудь");
                    return;
                }

                dt = new UnicData(ZenitId, ZenitSelected.TeamName, StaticData.Shared.Enums.ParserType.Zenit);
                ZenitData.Remove(ZenitSelected);
                Zenit.Remove(ZenitSelected);
            }

            else if (siteName == "PariMatch")
            {
                if (PariMacthSelected == null)
                {
                    MessageBox.Show("Выбирете, что-нибудь");
                    return;
                }

                dt = new UnicData(PariMatchId, PariMacthSelected.TeamName, StaticData.Shared.Enums.ParserType.PariMatch);
                PariMatchData.Remove(PariMacthSelected);
                PariMatch.Remove(PariMacthSelected);
            }

            Base.Add(dt);
        }
        #endregion

        #region Пропуск выбранного
        private void FonbetSkip()
        {

            FonbetData.Remove(FonbetSelected);
            Fonbet.Remove(FonbetSelected);
        }

        private void OlimpSkip()
        {
            OlimpData.Remove(OlimpSelected);
            Olimp.Remove(OlimpSelected);
        }

        private void MarafonSkip()
        {
            MarafonData.Remove(MarafonSelected);
            Marafon.Remove(MarafonSelected);
        }

        private void ZenitSkip()
        {
            ZenitData.Remove(ZenitSelected);
            Zenit.Remove(ZenitSelected);
        }

        private void PariMatchSkip()
        {
            PariMatchData.Remove(PariMacthSelected);
            PariMatch.Remove(PariMacthSelected);
        }
        #endregion



        public bool Save()
        {
            try
            {
                //Отменяем текущие сохранения. Иначе получаеться, что те что отредактирвоали сегодня
                //Просто исчезают. и Перед импортом нужно заново прогружать
                //SiteRow.Save("Fonbet.data", FonbetData);
                //SiteRow.Save("Olimp.data", OlimpData);
                //SiteRow.Save("Marafon.data", MarafonData);
                //SiteRow.Save("Zenit.data", ZenitData);
                //SiteRow.Save("PariMatch.data", PariMatchData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                UnicData.Save("bd.data", Base.ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            return true;
        }

        private int CheckValue(string value)
        {
            foreach (UnicData key in Base)
            {
                if (key.Value == value)
                    return key.Id;
            }
            return 0;
        }
    }
}
