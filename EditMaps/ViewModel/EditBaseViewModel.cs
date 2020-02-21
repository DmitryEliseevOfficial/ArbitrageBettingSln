using StaticData.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;


namespace EditMaps.ViewModel
{
    class EditBaseViewModel:BaseViewModel
    {
        public EditBaseViewModel()
        {
            Base = new ObservableCollection<UnicData>(UnicData.Load("base_new_two.data"));
            Maximum = Base.Select(x => x.Id).Max();
            Base.CollectionChanged += Base_CollectionChanged;
        }

        private void Base_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
           
        }

        public ObservableCollection<UnicData> Base;

        List<UnicData> _CurrentList;
        public List<UnicData> CurrentList
        {
            get { return _CurrentList; }
            set
            {
                if(_CurrentList!=value)
                {
                    _CurrentList = value;
                    PropChanged();
                }
            }
        }

        int _CurentId = 0;
        public int CurentId
        {
            get { return _CurentId; }
            set
            {
                if(_CurentId!=value)
                {
                    _CurentId = value;
                    PropChanged();
                    UpdateList();
                }
            }
        }

        int _Maximum = 0;
        public int Maximum
        {
            get { return _Maximum; }
            set
            {
                if(_Maximum!=value)
                {
                    _Maximum = value;
                    PropChanged();
                }
            }
        }

        private void UpdateList()
        {
            CurrentList = Base.Where(x => x.Id == CurentId).ToList();
        }
    }
}
