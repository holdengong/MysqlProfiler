using MysqlProfiler.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Caching.Memory;
using MysqlProfiler.Models;

namespace MysqlProfiler.Controllers
{
    public class HomeController : Controller
    {
        public IConfiguration Configuration { get; set; }

        public HomeController(IConfiguration configuration)
        {
            this.Configuration = configuration;

            this.ConnStr = Configuration["Sql:DefaultConnection"];
        }

        public static DateTime StartTime { get; set; } = DateTime.MinValue;
        public static List<string> TableNames { get; set; }
        public string ConnStr { get; set; }

        [Route("home/trace")]
        public JsonResult Trace(TraceRequest request)
        {
            On();

            if (StartTime == DateTime.MinValue)
            {
                StartTime = DateTime.Now;
            }

            string[] blackList = Configuration["Blacklist"].Split(",");

            List<string> tableNames = GetTableNames();

            List<LogItem> logItems = new List<LogItem>();
            List<LogItemDTO> logItemDTOs = new List<LogItemDTO>();

            using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnStr))
            {
                //string sqlStart = "set global log_output='table';set global general_log=on; repair table mysql.general_log;";
                //Dapper.SqlMapper.Execute(mySqlConnection, sqlStart);

                logItemDTOs = Dapper.SqlMapper.Query<LogItemDTO>(mySqlConnection, $" select * from mysql.general_log " +
                     $"where event_time>'{StartTime.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                     $"order by event_time desc ")
                     .ToList();
            }

            logItemDTOs.ForEach(e =>
            {
                LogItem logItem = new LogItem()
                {
                    Time = e.event_time.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    CommondType = e.command_type,
                    ServerId = e.server_id,
                    ThreadId = e.thread_id,
                    UserHost = e.user_host,
                    Sql = System.Text.Encoding.Default.GetString(e.argument)
                };

                if (tableNames.Any(a => logItem.Sql.Contains(a,StringComparison.OrdinalIgnoreCase))
                && !blackList.Any(b => logItem.Sql.Contains(b, StringComparison.OrdinalIgnoreCase))
                )
                {
                    if (!string.IsNullOrWhiteSpace(request.Keyword))
                    {
                        if (logItem.Sql.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            logItems.Add(logItem);
                        }
                    }
                    else
                    {
                        logItems.Add(logItem);
                    }
                }
            });


            return new JsonResult(logItems);
        }

        [Route("home/slow")]
        public JsonResult Slow()
        {
            List<SlowQuery> slowQueries = new List<SlowQuery>();
            using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnStr))
            {
                string sql = "select * from mysql.slow_log order by query_time desc";
                List<SlowQueryDTO> slowDtos = Dapper.SqlMapper.Query<SlowQueryDTO>(mySqlConnection, sql).ToList();

                slowDtos.ForEach(e => {
                    slowQueries.Add(new SlowQuery()
                    {
                        DB = e.db,
                        LockTime = DateTime.Parse(e.lock_time.ToString()).ToString("HH:mm:ss.fffff"),
                        QueryTime = DateTime.Parse(e.query_time.ToString()).ToString("HH:mm:ss.fffff"),
                        RowsExamined = e.rows_examined,
                        RowsSent = e.rows_sent,
                        Sql = System.Text.Encoding.Default.GetString( (byte[])e.sql_text),
                        StartTime = e.start_time.ToString("yyyy-MM-dd HH:mm:ss"),
                        UserHost = e.user_host
                    });
                });

            }

            return new JsonResult(slowQueries);
        }

        [Route("home/on")]
        public string On()
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnStr))
            {
                string sql = "set global log_output='table';set global general_log=on; repair table mysql.general_log;";
                Dapper.SqlMapper.Execute(mySqlConnection, sql);
            }

            return "ok";
        }

        [Route("home/off")]
        public string Off()
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnStr))
            {
                string sql = "set global general_log=off;";
                Dapper.SqlMapper.Execute(mySqlConnection, sql);
            }

            return "ok";
        }

        [Route("home/clear")]
        public string Clear()
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnStr))
            {
                string sql = $@"
                                SET GLOBAL general_log = 'OFF';
                                RENAME TABLE general_log TO general_log_temp;
                                DELETE FROM `general_log_temp`;
                                RENAME TABLE general_log_temp TO general_log;
                                SET GLOBAL general_log = 'ON';
                                ";

                Dapper.SqlMapper.Execute(mySqlConnection, sql);
            }

            return "ok";
        }

        public IActionResult Index()
        {
            return View();
        }

        private List<string> GetTableNames()
        {
            MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheKey = "MySqlProfile_TableNames";

            List<string> tableNames = memoryCache.Get <List<string>>(cacheKey);

            if (tableNames != null)
            {
                return tableNames;
            }

            string[] traceDbs = Configuration["TraceDatabaseNames"].Split(",");
            string sqlTables = "SELECT distinct TABLE_NAME FROM information_schema.columns";

            foreach (var db in traceDbs)
            {
                if (!sqlTables.Contains("WHERE"))
                {
                    sqlTables += " WHERE table_schema='" + db + "'";
                }
                else
                {
                    sqlTables += " OR table_schema='" + db + "'";
                }
            }

            using (MySqlConnection mySqlConnection = new MySqlConnection(this.ConnStr))
            {
                //  WHERE table_schema='mice'
                tableNames = Dapper.SqlMapper.Query<string>(mySqlConnection, sqlTables).ToList();
            }

            memoryCache.Set(cacheKey, tableNames, TimeSpan.FromMinutes(30));

            return tableNames;
        }
    }
}
