using System;

namespace ABShared.Protocol
{
    [Serializable]
    public class Packet
    {
        public StatusCode Code { get; set; }
        public CommandCode Comand { get; set; }
        public object Data { get; set; }
    }
}
