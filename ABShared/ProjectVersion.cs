using System;

namespace ABShared
{
    /// <summary>
    /// Хранит версию для синхранизации сервера и клиента
    /// </summary>
    public class ProjectVersion
    {
        public static Version Version { get; } = new Version("2.2.2.3");
    }
}
