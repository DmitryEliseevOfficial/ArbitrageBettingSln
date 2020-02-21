using System;
using System.Diagnostics;

namespace ABShared
{
    [Serializable]
    public class GameBet
    {
        public string Team1 { get; set; }
        public string Team2 { get; set; }

        public float Coef1 { get; set; }
        public float Coef2 { get; set; }

        public object Coef1o { get; set; }
        public object Coef2o { get; set; }


        public TenisGamePart GameNumber { get; set; }

        public SportTimePart Set { get; set; }
    }
}
