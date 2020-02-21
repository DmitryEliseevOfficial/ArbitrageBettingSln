using EditMaps.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EditMaps.View
{
    /// <summary>
    /// Логика взаимодействия для EditBaseView.xaml
    /// </summary>
    public partial class EditBaseView : Window
    {
        EditBaseViewModel vm;

        public EditBaseView()
        {
            InitializeComponent();
            vm = new EditBaseViewModel();
            DataContext = vm;
        }
    }
}
