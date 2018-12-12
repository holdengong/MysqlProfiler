using Ade.Tools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ade.Tools.Controllers
{
    public class HomeController : Controller
    {
        public IConfiguration Configuration { get; set; }

        public HomeController(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public static DateTime StartTime { get; set; } = DateTime.MinValue;

        public JsonResult Start()
        {
            if (StartTime == DateTime.MinValue)
            {
                StartTime = DateTime.Now;
            }

            int size = int.Parse(Configuration["Size"]);
            string connStr = Configuration["Sql:DefaultConnection"];

            string[] traceDbs = Configuration["TraceDatabaseNames"].Split(",");
            string[] blackList = Configuration["Blacklist"].Split(",");

            MySql.Data.MySqlClient.MySqlConnection mySqlConnection = new MySql.Data.MySqlClient.MySqlConnection(connStr);

            //string sqlStart = "set global log_output='table';set global general_log=on; repair table mysql.general_log;";
            //Dapper.SqlMapper.Execute(mySqlConnection, sqlStart);


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


            //  WHERE table_schema='mice'
            List<string> tableNames = Dapper.SqlMapper.Query<string>(mySqlConnection, sqlTables).ToList();


            List<LogItemDTO> logItemDTOs = Dapper.SqlMapper.Query<LogItemDTO>(mySqlConnection, $" select * from mysql.general_log " +
                //$"where event_time>'{StartTime.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                $"order by event_time desc limit {size} ").ToList();

            List<LogItem> logItems = new List<LogItem>();

            logItemDTOs.ForEach(e => {
                LogItem logItem = new LogItem()
                {
                    Time = e.event_time,
                    CommondType = e.command_type,
                    ServerId = e.server_id,
                    ThreadId = e.thread_id,
                    UserHost = e.user_host,
                    Sql = System.Text.Encoding.Default.GetString(e.argument)
                };

                if (tableNames.Any(a => logItem.Sql.Contains(a))
                && !blackList.Any(b => logItem.Sql.Contains(b))
                )
                {
                    logItems.Add(logItem);
                }
            });

            return new JsonResult(logItems);
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
