using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IronPython.Hosting;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using Newtonsoft.Json;

namespace ConvexHullRectangleApp
{
    public class GrahamScan : AlgorithmBase
    {
        private const double PI2 = 2 * Math.PI;
        private const double ANGLE_EPS = 1e-9;
        private const double INF = 1e9 + 5;
        public class PointFAngle
        {
            public PointF Point;
            public double Angle;

            public override string ToString()
            {
                return $"{Point.X:N2}, {Point.Y:N2} : {Angle:N2}";
            }
        }

        public GrahamScan(PointF[] points) : base(points) { }

        public override PointF[] Run()
        {
            Stack<PointF> stack = new Stack<PointF>();

            PointF p = GetBottomLeftMost();
            PointF[] sorted = SortPoints(p);

            for (int i = 0; i < sorted.Length; i++)
            {
                while (stack.Count > 1 && CCW(stack.ElementAt(1), stack.Peek(), sorted[i]) <= 0) {
                    //Console.WriteLine("{0},{1},{2}",stack.ElementAt(1), stack.Peek(), sorted[i]);
                    stack.Pop(); 
                }
                stack.Push(sorted[i]);
            }

            while (stack.Count > 1 && CCW(stack.ElementAt(1), stack.Peek(), p) <= 0)
            {
                stack.Pop();
            }

            stack.Push(p);
            return stack.ToArray();
        }

        public override IEnumerable<PointF[]> RunYield()
        {
            Stack<PointF> stack = new Stack<PointF>();

            PointF p = GetBottomLeftMost();
            PointF[] sorted = SortPoints(p);

            for (int i = 0; i < sorted.Length; i++)
            {
                while (stack.Count > 1 && CCW(stack.ElementAt(1), stack.Peek(), sorted[i]) <= 0) stack.Pop();
                stack.Push(sorted[i]);
                yield return stack.ToArray();
            }

            stack.Push(p);
            yield return stack.ToArray();
        }

        static private double CCW(PointF p1, PointF p2, PointF p3)
        {
            PointF v1 = new PointF(p2.X - p1.X, p2.Y - p1.Y);
            PointF v2 = new PointF(p3.X - p2.X, p3.Y - p2.Y);

            return Cross(v2, v1);
        }

        private PointF[] SortPoints(PointF p)
        {
            List<PointFAngle> s = new List<PointFAngle>();
            for (int i = 0; i < points.Length; i++)
            {
                s.Add(new PointFAngle()
                {
                    Point = points[i],
                    Angle = Math.Atan2(points[i].Y - p.Y, points[i].X - p.X)
                });
                if (s[i].Angle < 0) s[i].Angle += PI2;
                if (s[i].Angle < ANGLE_EPS) s[i].Angle += PI2;
            }
            s.Sort((p1, p2) => Math.Sign(p1.Angle - p2.Angle));

            List<PointF> res = new List<PointF>();
            for (int i = 0; i < s.Count; i++)
            {
                int cur_i = i;
                int best_i = i;
                while (cur_i<s.Count && Math.Abs(s[i].Angle - s[cur_i].Angle) <= ANGLE_EPS)
                {
                    if (Distance(p, s[cur_i].Point)>Distance(p, s[best_i].Point))
                    {
                        best_i=cur_i;
                    }
                    ++cur_i;
                }
                res.Add(s[best_i].Point);
                i = cur_i - 1;
            }
            //for (int i = 0; i < s.Count; ++i)
            //{
            //    Console.WriteLine("Point: {0}, Angle {1}", s[i].Point, s[i].Angle);
            //}
            return res.ToArray();
        }

        static public bool isInsidePoint(PointF[] CWHull,PointF point)
        {
            for (int i = 0; i < CWHull.Length; ++i)
            {
                if (CCW(CWHull[i], CWHull[(i + 1) % CWHull.Length], point)>0)
                {
                    return false;
                }
            }
            return true;
        }

        static public bool isInsideEquations(PointF[] CWHull, PointF point)
        {
            for (int i = 0; i < CWHull.Length; ++i)
            {
                double p1 = (CWHull[i].Y - CWHull[(i + 1) % CWHull.Length].Y);
                double p2 = (CWHull[(i + 1) % CWHull.Length].X - CWHull[i].X);
                double b = CWHull[(i + 1) % CWHull.Length].X * CWHull[i].Y - CWHull[i].X * CWHull[(i + 1) % CWHull.Length].Y;
                if (p1 * point.X + p2 * point.Y > b) return false;
            }
            return true;
        }

        static private double MyLog(double x)
        {
            if (x < 0) throw new Exception("not happening brah"); 
            return Math.Log(x);
        }

        public List<PointF> solveGetMaximumRectangleWithAngle(double t)
        {
            PointF[] CWHull = Run();
            List<double> P1 = new List<double>(), P2 = new List<double>(), B = new List<double>();

            for (int i = 0; i < CWHull.Length; ++i)
            {
                double p1 = (CWHull[i].Y - CWHull[(i + 1) % CWHull.Length].Y);
                double p2 = (CWHull[(i + 1) % CWHull.Length].X - CWHull[i].X);
                double b = CWHull[(i + 1) % CWHull.Length].X * CWHull[i].Y - CWHull[i].X * CWHull[(i + 1) % CWHull.Length].Y;
                P1.Add(p1);
                P2.Add(p2);
                B.Add(b);
            }

            // Формируем данные для передачи
            var data = new
            {
                P1 = P1,
                P2 = P2,
                B = B,
                t = t
            };

            // Сериализуем данные в JSON
            string jsonData = JsonConvert.SerializeObject(data);

            // Указываем путь к Python интерпретатору и скрипту
            string pythonPath = @"C:\Python311\python.exe";
            string scriptPath = @"../../src/cvx_optimization.py";
            File.WriteAllText(@"../../src/data.json", jsonData);

            // Запускаем процесс Python
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = pythonPath;
            start.Arguments = $"\"{scriptPath}\"";
            //Console.WriteLine(start.Arguments);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.CreateNoWindow = true;
            List<double> result_list;
            Console.WriteLine("CWHull size: {0}", CWHull.Length);

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    result_list = JsonConvert.DeserializeObject<List<double>>(result);
                }

                string stderr = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(stderr))
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(stderr);
                }
            }
            PointF point1 = new PointF((float)result_list[0], (float)result_list[1]),
            point2 = new PointF((float)result_list[2], (float)result_list[3]),
            point3 = new PointF((float)result_list[4], (float)result_list[5]);
            PointF point4 = new PointF(point2.X + point3.X - point1.X, point2.Y + point3.Y - point1.Y);
            return new List<PointF> { point1, point2, point3, point4 };

            //Func<Vector<double>, double> logBarrierFunc = value =>
            //{
            //    double u1 = value[0], u2 = value[1],
            //    v1 = value[2], v2 = value[3],
            //    x1 = value[4], x2 = value[5],
            //    y1 = value[6], y2 = value[7],
            //    z1 = value[8], z2 = value[9];


            //    double res = -(MyLog(u1) + MyLog(v2));
            //    double mu = 1;

            //    double sum1 = MyLog(u2-t*u1)+MyLog(v1+t*v2)+MyLog(u1-y1+x1)+MyLog(u2-y2+x2)+MyLog(v1-z1+x1)+MyLog(v2-z2+x2);
            //    sum1 += MyLog(-(u2 - t * u1)) + MyLog(-(v1 + t * v2)) + MyLog(-(u1 - y1 + x1)) + MyLog(-(u2 - y2 + x2)) + MyLog(-(v1 - z1 + x1)) + MyLog(-(v2 - z2 + x2));

            //    res -= mu * sum1;

            //    double sum2 = 0;

            //    for (int i = 0; i < CWHull.Length; ++i)
            //    {
            //        double p1 = (CWHull[i].Y - CWHull[(i + 1) % CWHull.Length].Y);
            //        double p2 = (CWHull[(i + 1) % CWHull.Length].X - CWHull[i].X);
            //        double b = CWHull[(i + 1) % CWHull.Length].X * CWHull[i].Y - CWHull[i].X * CWHull[(i + 1) % CWHull.Length].Y;
            //        sum2 += MyLog(b - (p1 * x1 + p2 * x2));
            //        sum2 += MyLog(b - (p1 * y1 + p2 * y2));
            //        sum2 += MyLog(b - (p1* z1 + p2 * z2));
            //        sum2 += MyLog(b - (p1 * (y1 + z1 - x1) + p2 * (y2 + z2 - x2)));
            //    }

            //    res -= mu * sum2;

            //    Console.WriteLine(res);

            //    return res;
            //};

            //return null;

            //Vector<double> initialGuess = Vector<double>.Build.DenseOfArray(new double[] { 0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0 });
            //var solver = new NelderMeadSimplex(1e-3,1000000);
            //var result = solver.FindMinimum(ObjectiveFunction.Value(logBarrierFunc), initialGuess);

            //var resultPoint = result.MinimizingPoint;
            //PointF point1 = new PointF((float)resultPoint[4], (float)resultPoint[5]),
            //point2 = new PointF((float)resultPoint[6], (float)resultPoint[7]),
            //point3 = new PointF((float)resultPoint[8], (float)resultPoint[9]);
            //PointF point4 = new PointF(point2.X+point3.X-point1.X, point2.Y+point3.Y-point1.Y);
            //return new List<PointF> { point1,point2,point3,point4 };
        }

        public List<PointF> solveGetMaximumAxisAlignedRectangle()
        {
            List<PointF> result = solveGetMaximumRectangleWithAngle(0);
            if (result.Count != 4) throw new Exception("something went wrong no rectangle");
            var temp = result[0];
            result[0] = result[1];
            result[1] = temp;
            return result;
        }

        public static List<double> solverLogBarrier()
        {
            // Пример функции цели: f(x) = x^2 + y^2
            Func<Vector<double>, double> objectiveFunction = v =>
            {
                double x = v[0];
                double y = v[1];
                return x * x + y * y;
            };

            // Пример градиента функции цели
            Func<Vector<double>, Vector<double>> objectiveGradient = v =>
            {
                double x = v[0];
                double y = v[1];
                return Vector<double>.Build.DenseOfArray(new double[] { 2 * x, 2 * y });
            };

            // Пример гессиана функции цели
            Func<Vector<double>, Matrix<double>> objectiveHessian = v =>
            {
                return Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { 2, 0 },
                    { 0, 2 }
                });
            };

            // Пример ограничений: x >= 1, y >= 1
            Func<Vector<double>, double> constraint1 = v => v[0] - 1;
            Func<Vector<double>, double> constraint2 = v => v[1] - 1;

            // Логарифмическая барьерная функция
            Func<Vector<double>, double> logBarrier = v =>
            {
                return -Math.Log(constraint1(v)) - Math.Log(constraint2(v));
            };

            // Градиент логарифмической барьерной функции
            Func<Vector<double>, Vector<double>> logBarrierGradient = v =>
            {
                Console.WriteLine("Contraint1 : {0}, Constraint2 :{1}", constraint1(v), constraint2(v));
                double invConstraint1 = 1 / constraint1(v);
                double invConstraint2 = 1 / constraint2(v);
                return Vector<double>.Build.DenseOfArray(new double[] { -invConstraint1, -invConstraint2 });
            };

            // Гессиан логарифмической барьерной функции
            Func<Vector<double>, Matrix<double>> logBarrierHessian = v =>
            {
                Console.WriteLine("Contraint1 : {0}, Constraint2 :{1}",constraint1(v),constraint2(v));
                double invConstraint1 = 1 / constraint1(v);
                double invConstraint2 = 1 / constraint2(v);
                return Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { invConstraint1 * invConstraint1, 0 },
                    { 0, invConstraint2 * invConstraint2 }
                });
            };

            // Комбинированная функция: f(x) + μ * logBarrier(x)
            double mu = 1.0;
            Func<Vector<double>, double> combinedFunction = v =>
            {
                return objectiveFunction(v) + mu * logBarrier(v);
            };

            // Градиент комбинированной функции
            Func<Vector<double>, Vector<double>> combinedGradient = v =>
            {
                return objectiveGradient(v) + mu * logBarrierGradient(v);
            };

            // Гессиан комбинированной функции
            Func<Vector<double>, Matrix<double>> combinedHessian = v =>
            {
                Console.WriteLine("v1 {0}, v2 {1}",v[0], v[1]);
                return objectiveHessian(v) + mu * logBarrierHessian(v);
            };

            // Начальные значения переменных
            Vector<double> initialGuess = Vector<double>.Build.DenseOfArray(new double[] { 5.0, 4.0 });

            // Оптимизация с использованием Newton-Raphson метода
            var objectiveFunctionWithGradientAndHessian = ObjectiveFunction.GradientHessian(combinedFunction, combinedGradient, combinedHessian);
            var solver = new NewtonMinimizer(1e-8, 50);
            var result = solver.FindMinimum(objectiveFunctionWithGradientAndHessian, initialGuess);

            return new List<double> { result.MinimizingPoint[0], result.MinimizingPoint[1] };
        }

        private PointF GetBottomLeftMost()
        {
            int[] idxs = new int[points.Length];

            float maxY = points.Max(p => p.Y);
            int idxsCount = 0;

            for (int i = 0; i < points.Length; i++)
                if (points[i].Y == maxY) idxs[idxsCount++] = i;

            PointF[] pts = new PointF[idxsCount];
            for (int i = 0; i < idxsCount; i++) pts[i] = points[idxs[i]];

            return GetLeftMost(pts);
        }

        static double TestMaxRectangle(int n)
        {
            PointF[] points = new PointF[n];
            Random random = new Random();
            for (int i=0;i<n;i++)
            {
                points[i] = new PointF((float)(random.Next(100000)), (float)(random.Next(100000)));
            }
            GrahamScan grahamScan = new GrahamScan(points);
            Stopwatch sw = Stopwatch.StartNew();
            grahamScan.solveGetMaximumAxisAlignedRectangle();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        static double TestMaxRectangleWithCWSize(int n)
        {
            PointF[] points = new PointF[n];
            Random random = new Random();

            int cur_x = 0, cur_y = 0;
            int vec_x = 1, vec_y = 1;
            for (int i = 0; i < n; i++)
            {
                points[i] = new PointF((float)(random.Next(cur_x)), (float)(cur_y));
                cur_x+=vec_x;
                if (i % 2 == 0)
                {
                    cur_y += random.Next(100000 - cur_y);
                }
                else
                {
                    cur_y -= random.Next(cur_y);
                }
            }
            GrahamScan grahamScan = new GrahamScan(points);
            Stopwatch sw = Stopwatch.StartNew();
            grahamScan.solveGetMaximumAxisAlignedRectangle();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        static List<(double, double)> GetPolygonPoints(int sides, double radius)
        {
            List<(double, double)> points = new List<(double, double)>();

            for (int i = 0; i < sides; i++)
            {
                double angle = 2 * Math.PI / sides * i;
                double x = radius * Math.Cos(angle);
                double y = radius * Math.Sin(angle);
                points.Add((x, y));
            }

            return points;
        }

        static double TestMaxRectangleWithFixed(int n)
        {
            PointF[] points = new PointF[n];

            List < (double, double) > polygonPoints = GetPolygonPoints(n, 100000);
            for (int i = 0; i < n; i++)
            {
                points[i] = new PointF((float)polygonPoints[i].Item1, (float)polygonPoints[i].Item2);
            }
            GrahamScan grahamScan = new GrahamScan(points);
            Stopwatch sw = Stopwatch.StartNew();
            grahamScan.solveGetMaximumAxisAlignedRectangle();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public static void Tests()
        {
            int n = 5;
            for (int i = 0; i < 6; ++i)
            {
                double ms =TestMaxRectangle(n);
                Console.WriteLine("Solution for {0} vertices found with {1} s", n, ms / 1000);
                n *= 10;
            }
            return;
        }
        public static void TestsCW()
        {
            int n = 5;
            for (int i = 0; i < 6; ++i)
            {
                double ms = TestMaxRectangleWithCWSize(n);
                Console.WriteLine("Solution for {0} vertices found with {1} s", n, ms / 1000);
                n *= 10;
            }
            return;
        }
        public static void TestsFixed()
        {
            int n = 5;
            for (int i = 0; i < 6; ++i)
            {
                double ms = TestMaxRectangleWithFixed(n);
                Console.WriteLine("Solution for {0} vertices found with {1} s", n, ms / 1000);
                n *= 10;
            }
            return;
        }
    }
}
