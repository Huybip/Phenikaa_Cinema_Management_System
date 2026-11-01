using System.Data.Entity;
using PhenikaaCinema.Models;


namespace PhenikaaCinema.Data
{
    public class CinemaContext : DbContext
    {
        static CinemaContext()
        {
            Database.SetInitializer<CinemaContext>(null);
        }
        public CinemaContext()
            : base("name=CinemaDb")
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Showtime> Showtimes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingSeat> BookingSeats { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<BookingPromotion> BookingPromotions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Các bảng liên kết sử dụng khóa chính kép.
            modelBuilder.Entity<MovieGenre>()
                .HasKey(x => new { x.MovieId, x.GenreId });

            modelBuilder.Entity<BookingPromotion>()
                .HasKey(x => new { x.BookingId, x.PromotionId });

            // Một người dùng có thể là khách đặt vé hoặc nhân viên tạo đơn.
            modelBuilder.Entity<Booking>()
                .HasOptional(x => x.Customer)
                .WithMany(x => x.CustomerBookings)
                .HasForeignKey(x => x.CustomerUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Booking>()
                .HasRequired(x => x.CreatedBy)
                .WithMany(x => x.CreatedBookings)
                .HasForeignKey(x => x.CreatedByUserId)
                .WillCascadeOnDelete(false);

            // Quan hệ lịch chiếu - phim - phòng.
            modelBuilder.Entity<Showtime>()
                .HasRequired(x => x.Movie)
                .WithMany(x => x.Showtimes)
                .HasForeignKey(x => x.MovieId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Showtime>()
                .HasRequired(x => x.Room)
                .WithMany(x => x.Showtimes)
                .HasForeignKey(x => x.RoomId)
                .WillCascadeOnDelete(false);

            // Quan hệ đặt vé - lịch chiếu.
            modelBuilder.Entity<Booking>()
                .HasRequired(x => x.Showtime)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.ShowtimeId)
                .WillCascadeOnDelete(false);

            // Quan hệ ghế đặt - đơn đặt vé, lịch chiếu và ghế.
            modelBuilder.Entity<BookingSeat>()
                .HasRequired(x => x.Booking)
                .WithMany(x => x.BookingSeats)
                .HasForeignKey(x => x.BookingId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BookingSeat>()
                .HasRequired(x => x.Showtime)
                .WithMany(x => x.BookingSeats)
                .HasForeignKey(x => x.ShowtimeId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BookingSeat>()
                .HasRequired(x => x.Seat)
                .WithMany(x => x.BookingSeats)
                .HasForeignKey(x => x.SeatId)
                .WillCascadeOnDelete(false);

            // Tắt cascade delete trên các quan hệ còn lại để tránh
            // việc xóa dây chuyền ngoài ý muốn.
            modelBuilder.Entity<User>()
                .HasRequired(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Seat>()
                .HasRequired(x => x.Room)
                .WithMany(x => x.Seats)
                .HasForeignKey(x => x.RoomId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Payment>()
                .HasRequired(x => x.Booking)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.BookingId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ticket>()
                .HasRequired(x => x.BookingSeat)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.BookingSeatId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MovieGenre>()
                .HasRequired(x => x.Movie)
                .WithMany(x => x.MovieGenres)
                .HasForeignKey(x => x.MovieId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MovieGenre>()
                .HasRequired(x => x.Genre)
                .WithMany(x => x.MovieGenres)
                .HasForeignKey(x => x.GenreId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BookingPromotion>()
                .HasRequired(x => x.Booking)
                .WithMany(x => x.BookingPromotions)
                .HasForeignKey(x => x.BookingId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BookingPromotion>()
                .HasRequired(x => x.Promotion)
                .WithMany(x => x.BookingPromotions)
                .HasForeignKey(x => x.PromotionId)
                .WillCascadeOnDelete(false);

            // Ánh xạ kiểu dữ liệu SQL Server.
            modelBuilder.Entity<Movie>()
                .Property(x => x.Description)
                .IsMaxLength();

            modelBuilder.Entity<Movie>()
                .Property(x => x.ReleaseDate)
                .HasColumnType("date");

            modelBuilder.Entity<Movie>()
                .Property(x => x.EndDate)
                .HasColumnType("date");

            modelBuilder.Entity<Seat>()
                .Property(x => x.ExtraPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Showtime>()
                .Property(x => x.BasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BookingSeat>()
                .Property(x => x.SeatPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
                .Property(x => x.DiscountAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
                .Property(x => x.FinalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Promotion>()
                .Property(x => x.DiscountValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BookingPromotion>()
                .Property(x => x.DiscountAmount)
                .HasPrecision(18, 2);
        }
    }
}