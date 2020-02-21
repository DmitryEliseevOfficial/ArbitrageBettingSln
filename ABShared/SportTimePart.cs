using System;

namespace ABShared
{
    [Serializable]
    public enum SportTimePart
    {
        Match = 0,
        Time1 =1,
        Time2=2,
        Time3=3,
        Time4=4,
        Time5=5,
        Time6=6,
        Time7=7,
        Time8=8,
        Time9=9,

        Half1=11,  // используються в баскеболе и прочем
        Half2=12,
        Half3=13,
        Half4=14,

        
        Nan = 999

    }

    public static  class SportTimePartHelper
    {
        public static SportTimePart Parse(string data)
        {
            try
            {                
                return (SportTimePart)System.Enum.Parse(typeof(SportTimePart), data);
            }
            catch
            {
                return SportTimePart.Nan;
            }
        }
    }
}
