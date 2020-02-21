using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ABServer.Commands;
using ABServer.Properties;

namespace ABServer.ViewModel
{
    internal class SetTimeoutViewModel:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        

        public MainConfigurate Configurate
        {
            get { return _configurate; }
            set
            {
                if(_configurate==value) return;
                _configurate = value;OnPropertyChanged();
            }
        }
        private MainConfigurate _configurate;

        public SetTimeoutViewModel()
        {
            _configurate=MainConfigurate.Configurate.Clone();
            SaveCommand = new RealyCommand(Save);
            ResetCommand= new RealyCommand(Reset);
        }

        public RealyCommand SaveCommand { get; set; }
        public RealyCommand ResetCommand { get; set; }

        private void Reset()
        {
            Configurate = new MainConfigurate();
        }

        private void Save()
        {
            try
            {
                MainConfigurate.Save(Configurate);
                MessageBox.Show("Сохранили");
            }
            catch (Exception e)
            {
                MessageBox.Show($"не удалось сохранить настройки: {e.Message}");
            }
        }
    }
}
