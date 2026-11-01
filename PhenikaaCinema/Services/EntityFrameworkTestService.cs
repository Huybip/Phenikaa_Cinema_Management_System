using System;
using System.Linq;
using System.Data.Entity;
using PhenikaaCinema.Data;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Services
{
    public class EntityFrameworkTestService
    {
        public string TestConnection()
        {
            try
            {
                using (var db = new CinemaContext())
                {
                    // Kiểm tra kết nối tới SQL Server.
                    db.Database.Connection.Open();

                    string databaseName =
                        db.Database.Connection.Database;

                    db.Database.Connection.Close();

                    // Thử truy vấn bảng Movies bằng EF6.
                    var movies = db.Movies
                        .AsNoTracking()
                        .Select(m => m.Title)
                        .Take(5)
                        .ToList();

                    string result =
                        "Kết nối Entity Framework thành công!\n" +
                        "Database: " + databaseName + "\n" +
                        "Số phim đọc được: " + movies.Count;

                    if (movies.Count > 0)
                    {
                        result += "\n\nDanh sách phim:\n" +
                                  string.Join("\n", movies);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Exception current = ex;
                string error = "";

                while (current != null)
                {
                    error += current.Message + "\n\n";
                    current = current.InnerException;
                }

                return "Kiểm tra Entity Framework thất bại:\n\n"
                       + error;
            }
        }
    }
}