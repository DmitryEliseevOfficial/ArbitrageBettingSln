using System.Collections.Generic;

namespace ABShared
{
    public static class SportTypeHelper
    {
        public static SportType Parse(string val)
        {
            val = val.ToLower().Trim();
            if (val.Contains("баскетбол"))
            {
                return SportType.Баскетбол;
            }
            if (val.Contains("волейбол"))
            {
                return SportType.Волейбол;
            }
            if (val.Contains("наст"))
            {
                return SportType.Настольный_теннис;
            }
            if (val.Contains("регби"))
            {
                return SportType.Регби;
            }
            if (val.Contains("теннис"))
            {
                return SportType.Теннис;
            }
            if (val.Contains("футзал"))
            {
                return SportType.Футзал;
            }
            if (val.Contains("бадминтон"))
            {
                return SportType.Бадминтон;
            }
            if (val.Contains("бейсбол"))
            {
                return SportType.Бейсбол;
            }
            if (val.Contains("гандбол"))
            {
                return SportType.Гандбол;
            }
            if (val.Contains("пляжный"))
            {
                return SportType.Пляжный_волейбол;
            }
            if (val.Contains("снукер") || val.Contains("бильярд"))
            {
                return SportType.Снукер;
            }
            if (val.Contains("футбол"))
            {
                return SportType.Футбол;
            }
            if (val.Contains("с мячом"))
            {
                return SportType.Хоккей_с_мячом;
            }
            if (val.Contains("хоккей"))
            {
                return SportType.Хоккей;
            }
            if (val.Contains("водное поло"))
            {
                return SportType.Водное_поло;
            }
            return SportType.Other;
        }

        public static List<SportTypeData> InitSports()
        {
            var data = System.Enum.GetValues(typeof(SportType));
            var sports = new List<SportTypeData>();

            foreach (SportType key in data)
            {
                if (key != SportType.Other)
                    sports.Add(new SportTypeData() { IsFilter = true, SportType = key });
            }

            return sports;
        }


        public static string ToStringFull(this SportType value)
        {
            return value.ToString().Replace("_", " ");
        }
    }
}
