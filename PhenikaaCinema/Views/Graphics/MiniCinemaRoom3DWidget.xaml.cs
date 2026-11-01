using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace PhenikaaCinema.Views.Graphics
{
    public partial class MiniCinemaRoom3DWidget : UserControl
    {
        public MiniCinemaRoom3DWidget()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                BuildMiniRoom();
                StartTurntableAnimation();
            };
        }

        private void StartTurntableAnimation()
        {
            var anim = new DoubleAnimation
            {
                From = -25.0,
                To = 25.0,
                Duration = TimeSpan.FromSeconds(7),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            RoomTurntableRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, anim);
        }

        private void BuildMiniRoom()
        {
            while (MiniSceneGroup.Children.Count > 3)
            {
                MiniSceneGroup.Children.RemoveAt(MiniSceneGroup.Children.Count - 1);
            }

            // Floor
            var floorMesh = new MeshGeometry3D();
            floorMesh.Positions.Add(new Point3D(-7, 0, -5));
            floorMesh.Positions.Add(new Point3D(7, 0, -5));
            floorMesh.Positions.Add(new Point3D(7, 0, 7));
            floorMesh.Positions.Add(new Point3D(-7, 0, 7));
            floorMesh.TriangleIndices.Add(0); floorMesh.TriangleIndices.Add(1); floorMesh.TriangleIndices.Add(2);
            floorMesh.TriangleIndices.Add(0); floorMesh.TriangleIndices.Add(2); floorMesh.TriangleIndices.Add(3);
            MiniSceneGroup.Children.Add(new GeometryModel3D(floorMesh, new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(20, 20, 28)))));

            // Curved 3D Screen
            var screenMesh = new MeshGeometry3D();
            int segs = 10;
            for (int i = 0; i <= segs; i++)
            {
                double u = (double)i / segs;
                double x = (u - 0.5) * 9.0;
                double z = -4.5 + Math.Cos((u - 0.5) * Math.PI) * 0.7 - 0.7;
                screenMesh.Positions.Add(new Point3D(x, 0.4, z));
                screenMesh.Positions.Add(new Point3D(x, 4.2, z));
            }
            for (int i = 0; i < segs; i++)
            {
                int b1 = i * 2; int t1 = b1 + 1;
                int b2 = (i + 1) * 2; int t2 = b2 + 1;
                screenMesh.TriangleIndices.Add(b1); screenMesh.TriangleIndices.Add(t1); screenMesh.TriangleIndices.Add(t2);
                screenMesh.TriangleIndices.Add(b1); screenMesh.TriangleIndices.Add(t2); screenMesh.TriangleIndices.Add(b2);
            }
            MiniSceneGroup.Children.Add(new GeometryModel3D(screenMesh, new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(245, 245, 255)))));

            // Mini 3D Seats block rows
            Color[] rowColors = { Color.FromRgb(245, 158, 11), Color.FromRgb(249, 115, 22), Color.FromRgb(37, 99, 235), Color.FromRgb(59, 130, 246) };
            for (int r = 0; r < 4; r++)
            {
                double z = -0.5 + (r * 1.5);
                double y = r * 0.4;
                var mat = new DiffuseMaterial(new SolidColorBrush(rowColors[r % rowColors.Length]));

                for (int c = -3; c <= 3; c++)
                {
                    double x = c * 0.85;
                    MiniSceneGroup.Children.Add(CreateBox(new Point3D(x, y + 0.3, z), 0.65, 0.5, 0.5, mat));
                }
            }
        }

        private GeometryModel3D CreateBox(Point3D center, double xSize, double ySize, double zSize, Material material)
        {
            var mesh = new MeshGeometry3D();
            double hX = xSize / 2.0; double hY = ySize / 2.0; double hZ = zSize / 2.0;

            Point3D p0 = new Point3D(center.X - hX, center.Y - hY, center.Z - hZ);
            Point3D p1 = new Point3D(center.X + hX, center.Y - hY, center.Z - hZ);
            Point3D p2 = new Point3D(center.X + hX, center.Y - hY, center.Z + hZ);
            Point3D p3 = new Point3D(center.X - hX, center.Y - hY, center.Z + hZ);
            Point3D p4 = new Point3D(center.X - hX, center.Y + hY, center.Z - hZ);
            Point3D p5 = new Point3D(center.X + hX, center.Y + hY, center.Z - hZ);
            Point3D p6 = new Point3D(center.X + hX, center.Y + hY, center.Z + hZ);
            Point3D p7 = new Point3D(center.X - hX, center.Y + hY, center.Z + hZ);

            AddQuad(mesh, p3, p2, p6, p7);
            AddQuad(mesh, p1, p0, p4, p5);
            AddQuad(mesh, p4, p7, p6, p5);
            AddQuad(mesh, p0, p1, p2, p3);
            AddQuad(mesh, p0, p3, p7, p4);
            AddQuad(mesh, p2, p1, p5, p6);

            return new GeometryModel3D(mesh, material);
        }

        private void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0); mesh.Positions.Add(p1); mesh.Positions.Add(p2); mesh.Positions.Add(p3);
            mesh.TriangleIndices.Add(baseIdx); mesh.TriangleIndices.Add(baseIdx + 1); mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx); mesh.TriangleIndices.Add(baseIdx + 2); mesh.TriangleIndices.Add(baseIdx + 3);
        }
    }
}

