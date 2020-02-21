using System;
using System.Collections.Generic;

namespace ABServer.Parsers.fonbetModel
{
    internal class CurrentLine
    {
        internal Dictionary<int, Event> Events { get; set; } = new Dictionary<int, Event>();

        internal Dictionary<int, Sport> Sports { get; set; } = new Dictionary<int, Sport>();

        internal DateTime LastUpdate { get; set; }

        internal List<Event> GetAdditionTime(int eventId)
        {
            List<Event> rezult = new List<Event>();

            foreach (KeyValuePair<int, Event> key in Events)
            {
                if (key.Value.ParentId == eventId)
                    if (!key.Value.IsBlock)
                        rezult.Add(key.Value);
#if DEBUG
                    else
                    {
                        Console.WriteLine($"Заблокированное событие {key.Key} пропустили");
                    }
#endif
            }

            return rezult;
        }
    }
}
