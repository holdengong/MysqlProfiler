using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace Ade.Tools.Models
{
    public class LogItem
    {
        public string Time { get; set; }
        public string UserHost { get; set; }
        public long ThreadId { get; set; }
        public int ServerId { get; set; }
        public string CommondType { get; set; }
        public string Sql { get; set; }
    }
}
