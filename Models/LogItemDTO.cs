using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace Ade.Tools.Models
{
    public class LogItemDTO
    {
        public DateTime event_time { get; set; }
        public string user_host { get; set; }
        public long thread_id { get; set; }
        public int server_id { get; set; }
        public string command_type { get; set; }
        public byte[] argument { get; set; }
    }
}
