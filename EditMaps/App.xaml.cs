using StaticData.Shared.Model;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using StaticData.Parsers;
using StaticData.Parsers.Zenit;
using StaticData.Parsers.PariMatch;


namespace EditMaps
{
   
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {

        }

        public IEnumerable<UnicData> Compare(UnicData dt, IEnumerable<UnicData> coll)
        {
            return new List<UnicData>();
        }
    }
}
