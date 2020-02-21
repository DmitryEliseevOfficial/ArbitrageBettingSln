using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ABServer
{
    [Serializable]
    internal class MainConfigurate
    {
        private static MainConfigurate _configurate;

        public static MainConfigurate Configurate
        {
            get
            {
                if (_configurate == null)
                    Init();
                return _configurate;
            }
            set
            {
                if (_configurate == value) return;
                _configurate = value;
            }
        }

        private static void Init()
        {
            try
            {
                var data = File.ReadAllBytes("timeout.dat");
                var mr = new MemoryStream(data);
                var fr = new BinaryFormatter();
                _configurate = (MainConfigurate)fr.Deserialize(mr);
            }
            catch (Exception e)
            {
                _configurate = new MainConfigurate();
            }
        }

        public static void Save(MainConfigurate newData)
        {
            var mr = new MemoryStream();
            var fr = new BinaryFormatter();
            fr.Serialize(mr, newData);
            File.WriteAllBytes("timeout.dat", mr.ToArray());
            _configurate = newData;
        }

        /// <summary>
        /// Максильное время ликвидности информации о событии
        /// </summary>
        public int EventMaxTime { get; set; } = 10000;

        /// <summary>
        /// Максимальное время для ликвидности информации о событии
        /// </summary>
        public int OlimpMaxTime { get; set; } = 10000;

        /// <summary>
        /// Максимальное время для ликвидности информации о событии
        /// </summary>
        public int MarafonMaxTime { get; set; } = 10000;

        /// <summary>
        /// Максимальное время для ликвидности информации о событии
        /// </summary>
        public int ZenitMaxTime { get; set; } = 10000;

        /// <summary>
        /// Время последнего обновления линии. Т.к. у нас все делаеться в одном потоке и очень быстро, то ориентируемся по этому времени
        /// </summary>
        public int FonbetMaxTime { get; set; } = 10000;

        /// <summary>
        /// Время отсылки данных клиентам
        /// </summary>
        public int ServerManagerSendTime { get; set; } = 1000;

        /// <summary>
        /// Как часто ForkFinder будет искать вилки. Чаще секунды нету смысла
        /// </summary>
        public int ForkFinderTime { get; set; } = 1000;

        public MainConfigurate Clone()
        {
            var rez = new MainConfigurate();
            rez.EventMaxTime = EventMaxTime;
            rez.FonbetMaxTime = FonbetMaxTime;
            rez.ForkFinderTime = ForkFinderTime;
            rez.MarafonMaxTime = MarafonMaxTime;
            rez.OlimpMaxTime = OlimpMaxTime;
            rez.ServerManagerSendTime = ServerManagerSendTime;
            rez.ZenitMaxTime = ZenitMaxTime;

            return rez;

        }

    }
}
