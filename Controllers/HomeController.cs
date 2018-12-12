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

        public IActionResult Index()
        {
            //int buffer = 1024 * 10;

            //byte[] byteData = new byte[buffer];

            //string generalLogPath = Configuration["MySqlProfiler:GeneralLogPath"];
            //string slowLogPath = Configuration["MySqlProfiler:SlowLogPath"];

            //var fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(generalLogPath);
            //var desFileName = generalLogPath.Replace(fileNameWithoutExt, fileNameWithoutExt + "_Copy");
            //System.IO.File.Copy(generalLogPath, desFileName);

            //using (FileStream fs = System.IO.File.OpenRead(desFileName))
            //{
            //    fs.Seek(fs.Length - buffer, SeekOrigin.Begin);

            //    fs.Read(byteData, 0, buffer);
            //}

            //System.IO.File.Delete(desFileName);

            //var clippedStrs = System.Text.Encoding.Default.GetString(byteData);

            //string regexPattern = "[0-9]{4}-[0,1,2]{1}[0-9]{1}-[0,1,2]{1}[0-9]{1}T[0,1,2]{1}[0-9]{1}:[0-5]{1}[0-9]{1}:[0-5]{1}[0-9]{1}.[0-9]{6}Z";  

            //var lines = Regex.Split(clippedStrs, regexPattern).Skip(1).Reverse().ToList();

            //string tempLine = string.Empty;

            //foreach (var line in lines)
            //{
            //    tempLine = Regex.Replace(line, @"[0-9]*\s*Query\s*", string.Empty);

            //    if (tempLine.Contains("mice"))
            //    {
            //        querys.Add(tempLine);
            //    }
            //}


            int size = int.Parse(Configuration["Size"]);
            string connStr = Configuration["Sql:DefaultConnection"];

            string[] traceDbs = Configuration["TraceDatabaseNames"].Split(",");
            string[] blackList = Configuration["Blacklist"].Split(",");

            MySql.Data.MySqlClient.MySqlConnection mySqlConnection = new MySql.Data.MySqlClient.MySqlConnection(connStr);

            string sqlStart = "set global log_output='table';set global general_log=on; repair table mysql.general_log;";
            Dapper.SqlMapper.Execute(mySqlConnection, sqlStart);


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


            List<LogItemDTO> logItemDTOs = Dapper.SqlMapper.Query<LogItemDTO>(mySqlConnection, $" select * from mysql.general_log order by event_time desc limit {size} ").ToList();

            List<LogItem> logItems = new List<LogItem>();

            logItemDTOs.ForEach(e=> {
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

            ViewBag.Logs = logItems;

            return View();
        }
    }
}
