using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace Ade.Tools
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();

                var connStr = configuration["Sql:DefaultConnection"];
                var clearIntervalHours = int.Parse(configuration["ClearIntervalHours"]);

                AutoClearLog(connStr, clearIntervalHours);
            }

            host.Run();
        }

        private static void AutoClearLog(string connStr,int clearIntervalHours)
        {
            var timer = new Timer(
                  x =>
                  {
                      using (MySqlConnection mySqlConnection = new MySqlConnection(connStr))
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
                  }, null, 0, (int)(TimeSpan.FromHours(clearIntervalHours).TotalMilliseconds));

        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
