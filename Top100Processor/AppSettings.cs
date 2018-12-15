using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Top100Processor
{
    public class AppSettings
    {
        public static string ConnectionStringOriginal { get; private set; }

        public static string ConnectionStringAltered { get; private set; }

        static AppSettings()
        {
            var env = Environment.GetEnvironmentVariable("APP_ENV") ?? "development";
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, false)
                .AddJsonFile($"appsettings.{env}.json", true, false)
                .Build();

            ConnectionStringOriginal = config.GetConnectionString("original");
            ConnectionStringAltered = config.GetConnectionString("altered");
        }
    }
}