using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConvexHullRectangleApp
{
    public abstract class AlgorithmBase
    {
        protected PointF[] points;

        protected AlgorithmBase(PointF[] points)
        {
            this.points = points;
        }

        public abstract PointF[] Run();
        public abstract IEnumerable<PointF[]> RunYield();

        public double Perimeter
        {
            get
            {
                double d = 0;
                for (int i = 0; i < points.Length - 1; i++)
                    d += Distance(points[i], points[i + 1]);
                return d;
            }
        }

        public double Area
        {
            get
            {
                double a = 0;
                for (int i = 0; i < points.Length - 1; i++)
                    a += Math.Abs(points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y);
                return a / 2;
            }
        }

        public static double Distance(PointF p1, PointF p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        protected int GetPointLocation(PointF p, PointF l1, PointF l2)
        {
            l2.X -= l1.X;
            l2.Y -= l1.Y;
            p.X -= l1.X;
            p.Y -= l1.Y;

            float cross = Cross(p, l2);

            if (cross < 0) return -1;
            if (cross > 0) return 1;
            return 0;
        }

        protected PointF GetLeftMost(PointF[] pts)
        {
            float minX = float.MaxValue;
            int leftMost = -1;

            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].X < minX)
                {
                    minX = pts[i].X;
                    leftMost = i;
                }
            }

            return pts[leftMost];
        }

        protected PointF GetRightMost(PointF[] pts)
        {
            float maxX = float.MinValue;
            int rightMost = -1;

            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].X > maxX)
                {
                    maxX = pts[i].X;
                    rightMost = i;
                }
            }

            return pts[rightMost];
        }

        public static float Cross(PointF p1, PointF p2)
        {
            return p2.X * p1.Y - p2.Y * p1.X;
        }

        public static float Cross(PointF p1, PointF p2, PointF p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p1.Y - p3.Y) * (p2.X - p3.X);
        }

        public static List<(string Name, AlgorithmBase Algorithm)> GetAlgorithms(PointF[] points)
        {
            List<(string, AlgorithmBase)> algos = new List<(string, AlgorithmBase)>();
            Type type = typeof(AlgorithmBase);
            Assembly asm = Assembly.GetAssembly(type);

            foreach (Type t in asm.GetExportedTypes())
                if (t.BaseType == type)
                    algos.Add((t.Name.Split('.').Last(), (AlgorithmBase)Activator.CreateInstance(t, points)));

            return algos;
        }
    }
}
