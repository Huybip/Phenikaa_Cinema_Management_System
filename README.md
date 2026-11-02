# 🎬 PHENIKAA CINEMA

**PHENIKAA CINEMA** là ứng dụng quản lý rạp chiếu phim và đặt vé được phát triển bằng **C# WPF** và **SQL Server**. Hệ thống hỗ trợ quản lý phim, phòng chiếu, ghế ngồi, suất chiếu, đặt vé và thanh toán mô phỏng.

Dự án được xây dựng nhằm ứng dụng kiến thức lập trình C#, lập trình giao diện desktop, quản lý cơ sở dữ liệu và thiết kế hệ thống phần mềm vào một bài toán thực tế.

---

## 📌 1. Giới thiệu dự án

PHENIKAA CINEMA hướng đến xây dựng một hệ thống quản lý rạp chiếu phim với giao diện trực quan, dễ sử dụng và có khả năng mở rộng.

Hệ thống được thiết kế với ba nhóm người dùng chính:

- **Admin:** Quản lý hệ thống, phim, thể loại, phòng chiếu, ghế và suất chiếu.
- **Employee:** Hỗ trợ nghiệp vụ bán vé và quản lý đặt vé tại rạp.
- **Customer:** Xem phim, lựa chọn suất chiếu, chọn ghế và đặt vé.

## ✨ 2. Chức năng hệ thống

### 2.1. Chức năng xác thực và tài khoản

- Đăng nhập hệ thống.
- Đăng ký tài khoản khách hàng.
- Xác thực thông tin đăng nhập.
- Mã hóa mật khẩu.
- Phân quyền theo vai trò Admin, Employee và Customer.

### 2.2. Chức năng khách hàng (Customer)

- Xem danh sách phim.
- Xem thông tin chi tiết phim.
- Xem lịch chiếu.
- Lựa chọn suất chiếu.
- Xem sơ đồ ghế trong phòng chiếu.
- Chọn nhiều ghế để đặt vé.
- Giữ ghế tạm thời trong thời gian quy định.
- Xem thông tin booking.
- Thanh toán mô phỏng.
- Xem vé điện tử.
- Xem lịch sử đặt vé.

### 2.3. Chức năng nhân viên (Employee)

- Hỗ trợ khách hàng đặt vé tại quầy.
- Lựa chọn phim và suất chiếu.
- Chọn ghế cho khách hàng.
- Tạo booking.
- Xác nhận thanh toán mô phỏng.
- Tra cứu thông tin đặt vé.

### 2.4. Chức năng quản trị (Admin)

- Quản lý phim.
- Quản lý thể loại phim.
- Quản lý phòng chiếu.
- Quản lý sơ đồ ghế.
- Quản lý suất chiếu.
- Quản lý tài khoản và phân quyền.
- Quản lý khuyến mãi.
- Theo dõi hoạt động đặt vé và doanh thu.

*Lưu ý: Danh sách trên mô tả phạm vi chức năng của hệ thống; trạng thái hoàn thành của từng chức năng phụ thuộc vào tiến độ triển khai thực tế.*

---

## 🛠️ 3. Công nghệ sử dụng

| Công nghệ | Vai trò |
|---|---|
| C# | Ngôn ngữ lập trình chính |
| WPF | Xây dựng giao diện Desktop |
| XAML | Thiết kế giao diện |
| ADO.NET | Kết nối và thao tác cơ sở dữ liệu |
| SQL Server | Hệ quản trị cơ sở dữ liệu |
| Somee | Hosting SQL Server |
| BCrypt | Mã hóa và kiểm tra mật khẩu |
| Visual Studio | IDE phát triển ứng dụng |
| Git & GitHub | Quản lý mã nguồn |

### Công nghệ đồ họa

- WPF Canvas và Shape để xây dựng các thành phần đồ họa 2D.
- Viewport3D để xây dựng các thành phần đồ họa 3D.
- Storyboard và Animation để tạo hiệu ứng chuyển động.
- Brush, Transform và Effect để tùy chỉnh giao diện.

---

## 🏗️ 4. Kiến trúc và cấu trúc thư mục

Dự án được tổ chức theo hướng phân chia giao diện, dữ liệu, mô hình và xử lý nghiệp vụ.

```text
PHENIKAA-CINEMA/
│
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── AuthService.cs
├── DatabaseHelper.cs
├── PasswordHelper.cs
├── WindowNavigation.cs
│
├── App.config
├── App.config.example
├── packages.config
├── PHENIKAACINEMA.csproj
│
├── Data/
│   ├── CinemaContext.cs
│   └── Db.cs
│
├── Models/
│   ├── Booking.cs
│   ├── BookingPromotion.cs
│   ├── BookingSeat.cs
│   ├── Genre.cs
│   ├── Movie.cs
│   ├── MovieGenre.cs
│   ├── Payment.cs
│   ├── Promotion.cs
│   ├── Role.cs
│   ├── Room.cs
│   ├── Seat.cs
│   ├── Showtime.cs
│   ├── Ticket.cs
│   └── User.cs
│
├── Services/
│   ├── EntityFrameworkTestService.cs
│   └── RegistrationService.cs
│
├── Views/
│   ├── LoginWindow.xaml
│   ├── RegisterWindow.xaml
│   │
│   ├── Admin/
│   ├── Employee/
│   ├── Customer/
│   └── Booking/
│
├── Resources/
│   ├── Images/
│   ├── Icons/
│   └── Styles/
│
└── .gitignore
```

*Cấu trúc trên là cấu trúc tham chiếu được ghi nhận trong quá trình phát triển. Các file và thư mục có thể được bổ sung hoặc điều chỉnh theo phiên bản project thực tế.*

### Vai trò các thành phần

- **Data:** Quản lý kết nối và truy cập cơ sở dữ liệu.
- **Models:** Đại diện cho các đối tượng dữ liệu trong hệ thống.
- **Services:** Xử lý nghiệp vụ và thao tác dữ liệu.
- **Views:** Chứa các giao diện WPF theo từng vai trò.
- **Resources:** Lưu trữ hình ảnh, icon và tài nguyên giao diện.
- **AuthService:** Xử lý xác thực người dùng.
- **PasswordHelper:** Hỗ trợ mã hóa và kiểm tra mật khẩu.
- **WindowNavigation:** Hỗ trợ điều hướng giữa các cửa sổ.

---

## 🗄️ 5. Cơ sở dữ liệu

Hệ thống sử dụng SQL Server để lưu trữ và quản lý dữ liệu.

### Các bảng chính

| Bảng | Mô tả |
|---|---|
| Roles | Vai trò người dùng |
| Users | Thông tin tài khoản |
| Movies | Thông tin phim |
| Genres | Thể loại phim |
| MovieGenres | Quan hệ phim và thể loại |
| Rooms | Phòng chiếu |
| Seats | Ghế trong phòng chiếu |
| Showtimes | Suất chiếu |
| Bookings | Thông tin đặt vé |
| BookingSeats | Ghế thuộc booking |
| Payments | Thông tin thanh toán |
| Tickets | Vé điện tử |
| Promotions | Chương trình khuyến mãi |
| BookingPromotions | Khuyến mãi áp dụng cho booking |

### Stored Procedures

Hệ thống sử dụng Stored Procedures để xử lý một số nghiệp vụ đặt vé quan trọng:

| Stored Procedure | Chức năng |
|---|---|
| `sp_CreateBookingHold` | Tạo booking và giữ ghế tạm thời |
| `sp_ConfirmMockPayment` | Xác nhận thanh toán mô phỏng |
| `sp_CancelPendingBooking` | Hủy booking đang chờ thanh toán |
| `sp_ExpireBookingHolds` | Giải phóng các booking đã hết thời gian giữ ghế |

### Quy trình đặt vé

```text
Khách hàng / Nhân viên
        |
        v
   Chọn phim
        |
        v
   Chọn suất chiếu
        |
        v
     Chọn ghế
        |
        v
  Tạo Booking Hold
        |
        v
  Giữ ghế tạm thời
        |
        v
 Thanh toán mô phỏng
        |
        v
  Xác nhận thanh toán
        |
        v
 Cập nhật trạng thái Booking
        |
        v
     Tạo vé
        |
        v
   Hiển thị vé điện tử
```

**Lưu ý:** Thanh toán trong phiên bản này là thanh toán mô phỏng, không kết nối ngân hàng hoặc cổng thanh toán thực tế.

---

## 🎨 6. Thiết kế giao diện

Giao diện PHENIKAA CINEMA hướng đến phong cách hiện đại, trực quan và thống nhất.

Bảng màu thiết kế:

| Màu sắc | Mã màu | Mục đích |
|---|---|---|
| Trắng | `#FFFFFF` | Nền và thẻ nội dung |
| Xanh dương | `#2563EB` | Màu chủ đạo |
| Xanh đậm | `#1E40AF` | Nút và thành phần nhấn |
| Cam | `#F97316` | Màu nhấn và thao tác quan trọng |
| Nền sáng | `#F5F7FB` | Nền tổng thể |
| Chữ chính | `#1E293B` | Nội dung |
| Chữ phụ | `#64748B` | Thông tin bổ trợ |

Các thành phần giao diện được định hướng sử dụng chung một Design System để đảm bảo tính nhất quán giữa các màn hình.

---

## ⚙️ 7. Cài đặt và chạy dự án

### 7.1. Yêu cầu môi trường

- Windows 10 hoặc Windows 11.
- Visual Studio hỗ trợ phát triển WPF.
- .NET Framework hoặc .NET phiên bản tương ứng với cấu hình project.
- SQL Server hoặc quyền truy cập SQL Server đã được cấu hình.
- Git.

**Lưu ý:** Kiểm tra Target Framework trong file `.csproj` trước khi cài đặt môi trường phát triển.

### 7.2. Clone repository

```bash
git clone <YOUR_REPOSITORY_URL>
```

Di chuyển vào thư mục dự án:

```bash
cd PHENIKAA-CINEMA
```

### 7.3. Cấu hình kết nối database

File `App.config` chứa thông tin kết nối SQL Server và không được đưa lên GitHub.

Tạo file `App.config` từ `App.config.example`, sau đó cập nhật thông tin kết nối của bạn:

```xml
<connectionStrings>
    <add name="CinemaDb"
         connectionString="Data Source=YOUR_SERVER;
         Initial Catalog=YOUR_DATABASE;
         User ID=YOUR_USERNAME;
         Password=YOUR_PASSWORD;
         Encrypt=True;
         TrustServerCertificate=True;"
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

Thay các giá trị mẫu bằng thông tin SQL Server hợp lệ.

**Không chia sẻ hoặc commit thông tin đăng nhập database lên GitHub.**

### 7.4. Mở project

1. Mở Visual Studio.
2. Chọn Open a project or solution.
3. Chọn file `PHENIKAACINEMA.csproj` hoặc file Solution `.sln` nếu có.
4. Khôi phục các NuGet packages cần thiết.
5. Kiểm tra cấu hình kết nối database.
6. Build Solution.
7. Chạy ứng dụng bằng nút Start.

---

## 🔐 8. Bảo mật

Một số nguyên tắc bảo mật được áp dụng trong dự án:

- Mật khẩu người dùng được xử lý bằng BCrypt, không lưu mật khẩu dạng văn bản thuần.
- Sử dụng parameterized query khi truy vấn database.
- Phân quyền người dùng theo vai trò.
- Kiểm tra trạng thái booking trước khi xác nhận thanh toán.
- Kiểm tra thời gian giữ ghế để tránh đặt trùng.
- Không đưa thông tin đăng nhập SQL Server lên GitHub.
- Không lưu thông tin thẻ ngân hàng trong chức năng thanh toán mô phỏng.

---

## 🚀 9. Định hướng phát triển

Các hạng mục phát triển tiếp theo của PHENIKAA CINEMA:

- Hoàn thiện giao diện quản lý phim.
- Hoàn thiện quản lý thể loại phim.
- Hoàn thiện quản lý phòng chiếu và sơ đồ ghế.
- Hoàn thiện quản lý suất chiếu.
- Hoàn thiện quản lý tài khoản và phân quyền.
- Hoàn thiện quản lý chương trình khuyến mãi.
- Phát triển dashboard thống kê doanh thu.
- Nâng cấp trải nghiệm đồ họa 2D và 3D.
- Hoàn thiện kiểm thử và tối ưu hiệu năng.

---

## 👨‍💻 10. Tác giả

**PHENIKAA CINEMA**

- Developer: Trần Quốc Huy & Dương Thiện Hùng
- University: Phenikaa University
- Project: Cinema Management and Booking System
- Language: C#
- Framework: WPF

---

## 📄 11. License

Dự án được phát triển phục vụ mục đích học tập và nghiên cứu.

Nếu muốn sử dụng hoặc phát triển lại dự án, vui lòng ghi nhận nguồn gốc dự án và tác giả.
