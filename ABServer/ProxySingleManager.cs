using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ABServer
{
    internal static class ProxySingleManager
    {
        private static List<string> _proxyList;
        private static bool _isInit;
        private static object _lock = new object();
        private static int _currentIndex;

        public static string GetProxy()
        {
            lock (_lock)
            {
                if (!_isInit)
                    Init();
                if (_currentIndex == _proxyList.Count)
                    _currentIndex = 0;
                var rezult = _proxyList[_currentIndex];
                _currentIndex++;
                return rezult;
            }
        }

        private static void Init()
        {
            _proxyList = File.ReadAllLines("proxy.txt").ToList();
            _isInit = true;
        }
    }
}
