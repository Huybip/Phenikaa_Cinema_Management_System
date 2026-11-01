using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace PhenikaaCinema.Services
{
    public class MovieDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string PosterUrl { get; set; }
        public string TrailerUrl { get; set; }
        public string Status { get; set; }
        public string GenreNames { get; set; }

        public string FormattedDuration => $"{DurationMinutes} phút";
        public string FormattedReleaseDate => ReleaseDate.HasValue ? ReleaseDate.Value.ToString("dd/MM/yyyy") : "Đang cập nhật";
        public string DisplayGenres => string.IsNullOrWhiteSpace(GenreNames) ? "Phim chiếu rạp" : GenreNames;
    }

    public class MovieService
    {
        public List<MovieDto> GetNowShowingMovies()
        {
            string sql = @"
                SELECT m.MovieId, m.Title, m.Description, m.DurationMinutes,
                       m.ReleaseDate, m.EndDate, m.PosterUrl, m.TrailerUrl, m.Status,
                       (SELECT STUFF((
                            SELECT ', ' + g.GenreName
                            FROM dbo.MovieGenres mg
                            INNER JOIN dbo.Genres g ON mg.GenreId = g.GenreId
                            WHERE mg.MovieId = m.MovieId
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')) AS GenreNames
                FROM dbo.Movies m
                WHERE m.Status = 'NowShowing'
                ORDER BY m.MovieId DESC";

            return MapMovies(DatabaseHelper.ExecuteQuery(sql));
        }

        public List<MovieDto> GetAllMovies()
        {
            string sql = @"
                SELECT m.MovieId, m.Title, m.Description, m.DurationMinutes,
                       m.ReleaseDate, m.EndDate, m.PosterUrl, m.TrailerUrl, m.Status,
                       (SELECT STUFF((
                            SELECT ', ' + g.GenreName
                            FROM dbo.MovieGenres mg
                            INNER JOIN dbo.Genres g ON mg.GenreId = g.GenreId
                            WHERE mg.MovieId = m.MovieId
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')) AS GenreNames
                FROM dbo.Movies m
                ORDER BY m.MovieId DESC";

            return MapMovies(DatabaseHelper.ExecuteQuery(sql));
        }

        public List<MovieDto> SearchMovies(string keyword, string status = null)
        {
            keyword = (keyword ?? "").Trim();
            string sql = @"
                SELECT m.MovieId, m.Title, m.Description, m.DurationMinutes,
                       m.ReleaseDate, m.EndDate, m.PosterUrl, m.TrailerUrl, m.Status,
                       (SELECT STUFF((
                            SELECT ', ' + g.GenreName
                            FROM dbo.MovieGenres mg
                            INNER JOIN dbo.Genres g ON mg.GenreId = g.GenreId
                            WHERE mg.MovieId = m.MovieId
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')) AS GenreNames
                FROM dbo.Movies m
                WHERE (@Keyword = '' OR m.Title LIKE '%' + @Keyword + '%' OR m.Description LIKE '%' + @Keyword + '%')
                  AND (@Status IS NULL OR m.Status = @Status)
                ORDER BY m.MovieId DESC";

            var pKeyword = new SqlParameter("@Keyword", SqlDbType.NVarChar, 200) { Value = keyword };
            var pStatus = new SqlParameter("@Status", SqlDbType.NVarChar, 20) { Value = string.IsNullOrEmpty(status) ? (object)DBNull.Value : status };

            return MapMovies(DatabaseHelper.ExecuteQuery(sql, pKeyword, pStatus));
        }

        public MovieDto GetMovieById(int movieId)
        {
            string sql = @"
                SELECT m.MovieId, m.Title, m.Description, m.DurationMinutes,
                       m.ReleaseDate, m.EndDate, m.PosterUrl, m.TrailerUrl, m.Status,
                       (SELECT STUFF((
                            SELECT ', ' + g.GenreName
                            FROM dbo.MovieGenres mg
                            INNER JOIN dbo.Genres g ON mg.GenreId = g.GenreId
                            WHERE mg.MovieId = m.MovieId
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')) AS GenreNames
                FROM dbo.Movies m
                WHERE m.MovieId = @MovieId";

            var pId = new SqlParameter("@MovieId", SqlDbType.Int) { Value = movieId };
            var table = DatabaseHelper.ExecuteQuery(sql, pId);
            if (table.Rows.Count == 0) return null;
            return MapMovieRow(table.Rows[0]);
        }

        private List<MovieDto> MapMovies(DataTable table)
        {
            var list = new List<MovieDto>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(MapMovieRow(row));
            }
            return list;
        }

        private MovieDto MapMovieRow(DataRow row)
        {
            return new MovieDto
            {
                MovieId = Convert.ToInt32(row["MovieId"]),
                Title = row["Title"] != DBNull.Value ? row["Title"].ToString() : "",
                Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "",
                DurationMinutes = row["DurationMinutes"] != DBNull.Value ? Convert.ToInt32(row["DurationMinutes"]) : 0,
                ReleaseDate = row["ReleaseDate"] != DBNull.Value ? Convert.ToDateTime(row["ReleaseDate"]) : (DateTime?)null,
                EndDate = row["EndDate"] != DBNull.Value ? Convert.ToDateTime(row["EndDate"]) : (DateTime?)null,
                PosterUrl = row["PosterUrl"] != DBNull.Value ? row["PosterUrl"].ToString() : null,
                TrailerUrl = row["TrailerUrl"] != DBNull.Value ? row["TrailerUrl"].ToString() : null,
                Status = row["Status"] != DBNull.Value ? row["Status"].ToString() : "",
                GenreNames = row["GenreNames"] != DBNull.Value ? row["GenreNames"].ToString() : ""
            };
        }
    }
}
