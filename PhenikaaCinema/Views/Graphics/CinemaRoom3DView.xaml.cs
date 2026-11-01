using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using PhenikaaCinema.Services;

namespace PhenikaaCinema.Views.Graphics
{
    public partial class CinemaRoom3DView : UserControl
    {
        private Point _lastMousePosition;
        private bool _isMouseDown;
        private double _cameraDistance = 17.0;
        private double _cameraPitch = 24.0; // degrees
        private double _cameraYaw = 0.0;   // degrees

        public CinemaRoom3DView()
        {
            InitializeComponent();
            UpdateCamera();
        }

        public void LoadRoom(string roomName, List<SeatDto> seats)
        {
            RoomInfoText.Text = $"Mô phỏng 3D: {roomName} • {seats?.Count ?? 0} ghế";
            Build3DScene(seats);
        }

        private void Build3DScene(List<SeatDto> seats)
        {
            // Xóa các mesh cũ nhưng giữ lại các nguồn sáng (2 lights đầu tiên)
            while (WorldModelGroup.Children.Count > 3)
            {
                WorldModelGroup.Children.RemoveAt(WorldModelGroup.Children.Count - 1);
            }

            // 1. SÀN PHÒNG CHIẾU (Cinema carpet floor)
            WorldModelGroup.Children.Add(CreateFloorModel(24.0, 24.0, -0.1));

            // 2. MÀN HÌNH CHIẾU CONG 3D (Curved Cinema Screen)
            WorldModelGroup.Children.Add(CreateCinemaScreenModel());

            // 3. TƯỜNG PHÒNG CHIẾU (Left, Right, Back wall)
            WorldModelGroup.Children.Add(CreateWallsModel());

            // 4. CÁC HÀNG GHẾ 3D SINH ĐỘNG TỪ DATABASE
            if (seats != null && seats.Count > 0)
            {
                var rows = seats.Select(s => s.RowLabel).Distinct().OrderBy(r => r).ToList();
                int maxCol = seats.Max(s => s.SeatNumber);

                double seatSpacingX = 1.05;
                double rowSpacingZ = 1.35;
                double elevationStepY = 0.38; // Sàn giật cấp chuẩn rạp chiếu phim hiện đại

                for (int rIdx = 0; rIdx < rows.Count; rIdx++)
                {
                    string rowLabel = rows[rIdx];
                    var seatsInRow = seats.Where(s => s.RowLabel == rowLabel).ToList();
                    double zPos = 2.0 + (rIdx * rowSpacingZ);
                    double yPos = rIdx * elevationStepY;

                    // Bục giật cấp cho mỗi hàng ghế (Step riser)
                    WorldModelGroup.Children.Add(CreateStepModel(maxCol * seatSpacingX + 2.0, 1.25, yPos, zPos));

                    foreach (var seat in seatsInRow)
                    {
                        double xPos = ((seat.SeatNumber - 1) - (maxCol - 1) / 2.0) * seatSpacingX;
                        var seatModel = CreateSeatModel3D(seat, xPos, yPos, zPos);
                        WorldModelGroup.Children.Add(seatModel);
                    }
                }
            }
        }

        private GeometryModel3D CreateFloorModel(double width, double depth, double y)
        {
            var mesh = new MeshGeometry3D();
            double hW = width / 2.0;
            double hD = depth / 2.0;

            mesh.Positions.Add(new Point3D(-hW, y, -hD));
            mesh.Positions.Add(new Point3D(hW, y, -hD));
            mesh.Positions.Add(new Point3D(hW, y, hD));
            mesh.Positions.Add(new Point3D(-hW, y, hD));

            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(18, 18, 26)));
            return new GeometryModel3D(mesh, material);
        }

        private GeometryModel3D CreateCinemaScreenModel()
        {
            // Màn hình chiếu cong nhẹ ở phía trước (Z = -5.0)
            var mesh = new MeshGeometry3D();
            int segments = 16;
            double screenWidth = 14.0;
            double screenHeight = 6.0;
            double screenY = 3.5;
            double centerZ = -5.0;
            double curveDepth = 1.0;

            for (int i = 0; i <= segments; i++)
            {
                double u = (double)i / segments;
                double x = (u - 0.5) * screenWidth;
                double z = centerZ + Math.Cos((u - 0.5) * Math.PI) * curveDepth - curveDepth;

                mesh.Positions.Add(new Point3D(x, screenY - screenHeight / 2.0, z)); // Bottom
                mesh.Positions.Add(new Point3D(x, screenY + screenHeight / 2.0, z)); // Top

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
            }

            for (int i = 0; i < segments; i++)
            {
                int b1 = i * 2;
                int t1 = b1 + 1;
                int b2 = (i + 1) * 2;
                int t2 = b2 + 1;

                mesh.TriangleIndices.Add(b1);
                mesh.TriangleIndices.Add(t1);
                mesh.TriangleIndices.Add(t2);

                mesh.TriangleIndices.Add(b1);
                mesh.TriangleIndices.Add(t2);
                mesh.TriangleIndices.Add(b2);
            }

            // Material màu trắng sáng giả lập ánh sáng phản chiếu từ màn hình phim
            var screenBrush = new LinearGradientBrush(
                Color.FromRgb(240, 240, 255),
                Color.FromRgb(200, 220, 255),
                new Point(0, 0), new Point(1, 1));
            var material = new DiffuseMaterial(screenBrush);
            return new GeometryModel3D(mesh, material);
        }

        private Model3DGroup CreateWallsModel()
        {
            var group = new Model3DGroup();
            var wallBrush = new SolidColorBrush(Color.FromRgb(22, 22, 30));
            var wallMaterial = new DiffuseMaterial(wallBrush);

            // Left wall
            group.Children.Add(CreateBox(new Point3D(-11.5, 4.0, 4.0), 0.4, 8.0, 18.0, wallMaterial));
            // Right wall
            group.Children.Add(CreateBox(new Point3D(11.5, 4.0, 4.0), 0.4, 8.0, 18.0, wallMaterial));
            // Back wall
            group.Children.Add(CreateBox(new Point3D(0, 4.0, 13.0), 23.4, 8.0, 0.4, wallMaterial));

            return group;
        }

        private GeometryModel3D CreateStepModel(double width, double depth, double y, double z)
        {
            var stepMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(28, 28, 38)));
            return CreateBox(new Point3D(0, y / 2.0, z), width, y, depth, stepMaterial);
        }

        private Model3DGroup CreateSeatModel3D(SeatDto seat, double x, double y, double z)
        {
            var seatGroup = new Model3DGroup();

            Color seatColor;
            if (seat.Status == SeatStatus.Booked)
            {
                seatColor = Color.FromRgb(45, 45, 55); // Xám đậm đã bán
            }
            else if (seat.Status == SeatStatus.Held)
            {
                seatColor = Color.FromRgb(215, 50, 40); // Đỏ đang giữ
            }
            else if (seat.Status == SeatStatus.Selected)
            {
                seatColor = Color.FromRgb(46, 204, 113); // Xanh lá đã chọn
            }
            else if (seat.IsVip)
            {
                seatColor = Color.FromRgb(230, 160, 20); // Vàng VIP
            }
            else if (seat.IsCouple)
            {
                seatColor = Color.FromRgb(165, 80, 190); // Tím Couple
            }
            else
            {
                seatColor = Color.FromRgb(65, 65, 85); // Ghế thường xanh đen
            }

            var cushionMaterial = new DiffuseMaterial(new SolidColorBrush(seatColor));
            var frameMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(25, 25, 30)));

            double seatWidth = seat.IsCouple ? 1.25 : 0.72;
            double seatDepth = 0.65;
            double cushionThickness = 0.16;

            // Đệm ngồi (Seat Cushion)
            seatGroup.Children.Add(CreateBox(new Point3D(x, y + 0.38, z), seatWidth, cushionThickness, seatDepth, cushionMaterial));

            // Tựa lưng (Seat Backrest)
            seatGroup.Children.Add(CreateBox(new Point3D(x, y + 0.75, z + 0.24), seatWidth, 0.68, 0.14, cushionMaterial));

            // Tay vịn 2 bên (Armrests)
            seatGroup.Children.Add(CreateBox(new Point3D(x - seatWidth / 2.0 - 0.05, y + 0.45, z), 0.08, 0.35, 0.55, frameMaterial));
            seatGroup.Children.Add(CreateBox(new Point3D(x + seatWidth / 2.0 + 0.05, y + 0.45, z), 0.08, 0.35, 0.55, frameMaterial));

            // Chân ghế (Seat base leg)
            seatGroup.Children.Add(CreateBox(new Point3D(x, y + 0.16, z), 0.14, 0.32, 0.14, frameMaterial));

            return seatGroup;
        }

        private GeometryModel3D CreateBox(Point3D center, double xSize, double ySize, double zSize, Material material)
        {
            var mesh = new MeshGeometry3D();
            double hX = xSize / 2.0;
            double hY = ySize / 2.0;
            double hZ = zSize / 2.0;

            // 8 đỉnh của hình hộp chữ nhật
            Point3D p0 = new Point3D(center.X - hX, center.Y - hY, center.Z - hZ);
            Point3D p1 = new Point3D(center.X + hX, center.Y - hY, center.Z - hZ);
            Point3D p2 = new Point3D(center.X + hX, center.Y - hY, center.Z + hZ);
            Point3D p3 = new Point3D(center.X - hX, center.Y - hY, center.Z + hZ);
            Point3D p4 = new Point3D(center.X - hX, center.Y + hY, center.Z - hZ);
            Point3D p5 = new Point3D(center.X + hX, center.Y + hY, center.Z - hZ);
            Point3D p6 = new Point3D(center.X + hX, center.Y + hY, center.Z + hZ);
            Point3D p7 = new Point3D(center.X - hX, center.Y + hY, center.Z + hZ);

            // Front (Z+)
            AddQuad(mesh, p3, p2, p6, p7);
            // Back (Z-)
            AddQuad(mesh, p1, p0, p4, p5);
            // Top (Y+)
            AddQuad(mesh, p4, p7, p6, p5);
            // Bottom (Y-)
            AddQuad(mesh, p0, p1, p2, p3);
            // Left (X-)
            AddQuad(mesh, p0, p3, p7, p4);
            // Right (X+)
            AddQuad(mesh, p2, p1, p5, p6);

            return new GeometryModel3D(mesh, material);
        }

        private void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 1);
            mesh.TriangleIndices.Add(baseIdx + 2);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 3);
        }

        private void UpdateCamera()
        {
            double pitchRad = _cameraPitch * Math.PI / 180.0;
            double yawRad = _cameraYaw * Math.PI / 180.0;

            double camY = _cameraDistance * Math.Sin(pitchRad);
            double horizontalDist = _cameraDistance * Math.Cos(pitchRad);
            double camX = horizontalDist * Math.Sin(yawRad);
            double camZ = horizontalDist * Math.Cos(yawRad);

            Point3D targetCenter = new Point3D(0, 2.0, 3.0);
            MainCamera.Position = new Point3D(targetCenter.X + camX, targetCenter.Y + camY, targetCenter.Z + camZ);
            MainCamera.LookDirection = new Vector3D(targetCenter.X - MainCamera.Position.X,
                                                    targetCenter.Y - MainCamera.Position.Y,
                                                    targetCenter.Z - MainCamera.Position.Z);
        }

        // --- TƯƠNG TÁC XOAY & THU PHÓNG 3D ---
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isMouseDown = true;
                _lastMousePosition = e.GetPosition(this);
                CinemaViewport.CaptureMouse();
            }
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _lastMousePosition.X;
                double deltaY = currentPos.Y - _lastMousePosition.Y;

                _cameraYaw -= deltaX * 0.45;
                _cameraPitch += deltaY * 0.35;

                // Giới hạn góc ngẩng camera tránh lật ngược không gian
                if (_cameraPitch < 5.0) _cameraPitch = 5.0;
                if (_cameraPitch > 85.0) _cameraPitch = 85.0;

                _lastMousePosition = currentPos;
                UpdateCamera();
            }
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = false;
            CinemaViewport.ReleaseMouseCapture();
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                _cameraDistance = Math.Max(6.0, _cameraDistance - 1.2);
            }
            else
            {
                _cameraDistance = Math.Min(35.0, _cameraDistance + 1.2);
            }
            UpdateCamera();
        }

        private void BtnResetCamera_Click(object sender, RoutedEventArgs e)
        {
            _cameraDistance = 17.0;
            _cameraPitch = 24.0;
            _cameraYaw = 0.0;
            UpdateCamera();
        }

        private void BtnScreenView_Click(object sender, RoutedEventArgs e)
        {
            _cameraDistance = 13.0;
            _cameraPitch = 12.0;
            _cameraYaw = 180.0; // Nhìn từ sau màn hình về phía khán giả
            UpdateCamera();
        }

        private void BtnSeatView_Click(object sender, RoutedEventArgs e)
        {
            _cameraDistance = 11.0;
            _cameraPitch = 15.0;
            _cameraYaw = 0.0; // Góc nhìn mắt người ngồi xem phim
            UpdateCamera();
        }
    }
}

