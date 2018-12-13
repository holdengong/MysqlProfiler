using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace Ade.Tools.Models
{
    public class SlowQuery
    {
        public string StartTime { get; set; }
        public string UserHost { get; set; }
        public string QueryTime { get; set; }
        public string LockTime { get; set; }
        public int RowsSent { get; set; }
        public int RowsExamined { get; set; }
        public string DB { get; set; }
        public string Sql { get; set; }
    }
}
