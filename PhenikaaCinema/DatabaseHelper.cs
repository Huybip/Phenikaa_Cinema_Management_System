using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace PhenikaaCinema
{
    public static class DatabaseHelper
    {
        public static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["CinemaDb"]?.ConnectionString
            ?? throw new InvalidOperationException("Không tìm thấy connection string 'CinemaDb' trong App.config.");

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static bool TestConnection(out string message)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("SELECT DB_NAME()", connection))
                    {
                        string databaseName = Convert.ToString(command.ExecuteScalar());
                        message = "Kết nối thành công! Database: " + databaseName;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                message = "Kết nối thất bại: " + ex.Message;
                return false;
            }
        }

        public static DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            DataTable table = new DataTable();
            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            return table;
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteScalar();
                }
            }
        }

        public static DataTable GetMovies()
        {
            string sql = @"
                SELECT MovieId, Title, DurationMinutes,
                       ReleaseDate, Status, PosterUrl, Description
                FROM dbo.Movies
                ORDER BY MovieId DESC";
            return ExecuteQuery(sql);
        }
    }
}
