using System;
using System.Diagnostics;
using ABShared.Enum;

namespace ABShared
{
    [Serializable]
    public class Stake
    {
        public StakeType StakeType { get; set; }
        public ETeam Team { get; set; } = ETeam.Both;
        public float Coef { get; set; }
        public float Parametr { get; set; }
        public object ParametrO { get; set; }


        public override string ToString()
        {
            string type;
            switch (StakeType)
            {
                case StakeType.Fora1:
                    type = "Ф1";
                    break;
                case StakeType.Fora2:
                    type = "Ф2";
                    break;
                case StakeType.Tmin:
                    type = "Тм";
                    break;
                case StakeType.Tmax:
                    type = "Тб";
                    break;
                case StakeType.ITmin:
                    type = "ИТм";
                    break;
                case StakeType.ITmax:
                    type = "ИТб";
                    break;
                default:
                    type = "Nan";
                    break;
            }

            return $"{type}({Parametr}) ";
        }
    }
}