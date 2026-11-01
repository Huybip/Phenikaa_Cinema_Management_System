using System;
using System.Configuration;
using System.Data.SqlClient;

namespace PhenikaaCinema.Data
{
    public static class Db
    {
        public static SqlConnection GetConnection()
        {
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings["CinemaDb"];

            if (settings == null ||
                string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy connection string 'CinemaDb' trong App.config.");
            }

            return new SqlConnection(settings.ConnectionString);
        }
    }
}
