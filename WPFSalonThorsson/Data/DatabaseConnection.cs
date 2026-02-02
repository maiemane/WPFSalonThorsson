using WPFSalonThorsson;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace Salon.Data
{
    public static class DatabaseConnection
    {
        private static readonly string _connectionString;

        static DatabaseConnection()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _connectionString = configuration.GetConnectionString("SalonT");
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}