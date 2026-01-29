using WPFSalonThorsson;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace Salon.Data
{
    public static class DatabaseConnection
    {
        private static readonly string _connectionString;

        // Normalt ville denne streng blive hentet fra user secrets.json, men for at sikre der er forbindelse under test og bedømmelse, er der en fallback streng her.
        private const string _fallbackConnectionString = "Server=tcp:salont.sytes.net,1433;Database=SalonT_DB;User Id=mssqlDBadmin71293;Password=0)0L>7F1c`p<WJ>=&l$=niDR38Mu0x)u;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15;";

        static DatabaseConnection()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var configured = configuration.GetConnectionString("SalonT");

            _connectionString = string.IsNullOrWhiteSpace(configured)
                ? _fallbackConnectionString
                : configured;
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}