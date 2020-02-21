using System;
using System.Collections.Generic;
using StaticData.Shared.Model;

namespace ABServer
{

    public class UnicDataDecorator
    {
        public static event Action<List<UnicData>> UpdateBase;

        public static void SetUpdateBase(List<UnicData> newBaseData)
        {
                UpdateBase?.Invoke(newBaseData);
        }

        private List<UnicData> _bd;


        public UnicDataDecorator(List<UnicData> bd)
        {
            _bd = bd;
        }

        public void SetBase(List<UnicData> newBaseData)
        {
            if (newBaseData != null)
            {
                _bd = newBaseData;
            }
        }

        public int GetNumber(string str)
        {
            if (String.IsNullOrWhiteSpace(str))
                return -1;
            for (int i = 0; i < _bd.Count; i++)
            {
                if (_bd[i].Value.Trim().ToLower() == str.Trim().ToLower())
                    return _bd[i].Id;
            }
            return -1;
        }

        /// <summary>
        /// Проверяет. существует ли такая команда в базе
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Exist(string data)
        {
            return GetNumber(data) != -1;
        }

        /// <summary>
        /// Проверяет. Равны ли две строки между собой
        /// </summary>
        /// <param name="str1">Строка 1</param>
        /// <param name="str2">Строка 2</param>
        /// <returns></returns>
        public bool StringCompare(string str1, string str2)
        {
            if (str1 == str2)
                return true;

            var id1 = GetNumber(str1);
            if (id1 == -1)
                return false;
            var id2 = GetNumber(str2);

            if (id1 == id2)
                return true;

            return false;
        }
        
        public void SetNewBd(List<UnicData> newBd)
        {
            _bd = newBd;
        }
    }
}
