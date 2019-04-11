using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace MysqlProfiler.Models
{
    public class SlowQueryDTO
    {
        public DateTime start_time { get; set; }
        public string user_host { get; set; }
        public object query_time { get; set; }
        public object lock_time { get; set; }
        public int rows_sent { get; set; }
        public int rows_examined { get; set; }
        public string db { get; set; }
        public object sql_text { get; set; }
        public int last_insert_id { get; set; }
        public int insert_id { get; set; }
        public int thread_id { get; set; }
    }
}
