using System;
using System.Collections.Generic;

namespace ABClient.Data
{
    /// <summary>
    /// Служит для хранения текущих настроек окна
    /// </summary>
    [Serializable]
    public class WindowsSettings
    {
        public int MinPercent { get; set; }

        public int MaxPercent { get; set; } = 100;


        public double BrZoom1 { get; set; }


        public double BrZoom2 { get; set; }


        public double WindowsWidth { get; set; } = 600;

        public double WindowsHeight { get; set; } = 800;


        public double SpliterOne { get; set; } = 400;


        public double SpliterTwo { get; set; } = 250;

        public List<double> ColumnsWidth { get; set; }
    }
}
