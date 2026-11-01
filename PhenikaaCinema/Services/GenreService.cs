using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Services
{
    public class GenreDto
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class GenreService
    {
        public List<GenreDto> GetAllGenres(bool activeOnly = false)
        {
            string sql = @"
                SELECT GenreId, GenreName, Description, IsActive
                FROM dbo.Genres
                WHERE (@ActiveOnly = 0 OR IsActive = 1)
                ORDER BY GenreName";

            var pActive = new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly };
            var table = DatabaseHelper.ExecuteQuery(sql, pActive);

            var list = new List<GenreDto>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(new GenreDto
                {
                    GenreId = Convert.ToInt32(row["GenreId"]),
                    GenreName = row["GenreName"].ToString(),
                    Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "",
                    IsActive = Convert.ToBoolean(row["IsActive"])
                });
            }
            return list;
        }

        public bool AddGenre(string name, string description, bool isActive, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Tên thể loại không được để trống.";
                return false;
            }

            try
            {
                string checkSql = "SELECT COUNT(*) FROM dbo.Genres WHERE LOWER(GenreName) = LOWER(@Name)";
                var dt = DatabaseHelper.ExecuteQuery(checkSql, new SqlParameter("@Name", name.Trim()));
                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
                {
                    error = "Thể loại phim này đã tồn tại.";
                    return false;
                }

                string insertSql = @"
                    INSERT INTO dbo.Genres (GenreName, Description, IsActive)
                    VALUES (@Name, @Description, @IsActive)";

                var pName = new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = name.Trim() };
                var pDesc = new SqlParameter("@Description", SqlDbType.NVarChar, 255)
                {
                    Value = string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim()
                };
                var pActive = new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive };

                int rows = DatabaseHelper.ExecuteNonQuery(insertSql, pName, pDesc, pActive);
                return rows > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool UpdateGenre(int genreId, string name, string description, bool isActive, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Tên thể loại không được để trống.";
                return false;
            }

            try
            {
                string checkSql = "SELECT COUNT(*) FROM dbo.Genres WHERE LOWER(GenreName) = LOWER(@Name) AND GenreId != @GenreId";
                var dt = DatabaseHelper.ExecuteQuery(checkSql,
                    new SqlParameter("@Name", name.Trim()),
                    new SqlParameter("@GenreId", genreId));

                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
                {
                    error = "Tên thể loại đã được sử dụng.";
                    return false;
                }

                string updateSql = @"
                    UPDATE dbo.Genres
                    SET GenreName = @Name,
                        Description = @Description,
                        IsActive = @IsActive
                    WHERE GenreId = @GenreId";

                var pId = new SqlParameter("@GenreId", SqlDbType.Int) { Value = genreId };
                var pName = new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = name.Trim() };
                var pDesc = new SqlParameter("@Description", SqlDbType.NVarChar, 255)
                {
                    Value = string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim()
                };
                var pActive = new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive };

                int rows = DatabaseHelper.ExecuteNonQuery(updateSql, pId, pName, pDesc, pActive);
                return rows > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool DeleteGenre(int genreId, out string error)
        {
            error = null;
            try
            {
                // Kiểm tra xem có phim nào đang gán thể loại này không
                string checkSql = "SELECT COUNT(*) FROM dbo.MovieGenres WHERE GenreId = @GenreId";
                var dt = DatabaseHelper.ExecuteQuery(checkSql, new SqlParameter("@GenreId", genreId));
                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
                {
                    error = "Không thể xóa thể loại này vì đang có phim liên kết. Vui lòng chuyển trạng thái sang ngưng hoạt động.";
                    return false;
                }

                string deleteSql = "DELETE FROM dbo.Genres WHERE GenreId = @GenreId";
                int rows = DatabaseHelper.ExecuteNonQuery(deleteSql, new SqlParameter("@GenreId", genreId));
                return rows > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public List<int> GetGenreIdsForMovie(int movieId)
        {
            var list = new List<int>();
            try
            {
                string sql = "SELECT GenreId FROM dbo.MovieGenres WHERE MovieId = @MovieId";
                var dt = DatabaseHelper.ExecuteQuery(sql, new SqlParameter("@MovieId", movieId));
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(Convert.ToInt32(row["GenreId"]));
                }
            }
            catch { }
            return list;
        }

        public void SetGenresForMovie(int movieId, List<int> genreIds)
        {
            try
            {
                string deleteSql = "DELETE FROM dbo.MovieGenres WHERE MovieId = @MovieId";
                DatabaseHelper.ExecuteNonQuery(deleteSql, new SqlParameter("@MovieId", movieId));

                if (genreIds != null && genreIds.Count > 0)
                {
                    foreach (int gId in genreIds)
                    {
                        string insertSql = "INSERT INTO dbo.MovieGenres (MovieId, GenreId) VALUES (@MovieId, @GenreId)";
                        DatabaseHelper.ExecuteNonQuery(insertSql,
                            new SqlParameter("@MovieId", movieId),
                            new SqlParameter("@GenreId", gId));
                    }
                }
            }
            catch { }
        }
    }
}
