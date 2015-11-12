using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EnvironmentMaker {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PointCloud : MonoBehaviour {

        PlyReader reader;
        List<Vector3[][]> walkPoints;
        List<Color[][]> walkColors;
        List<Vector3> walkHeadPos;
        List<Vector3> bodyposes;
        int kinectNums = 4;
        public GameObject Pointer;
        List<Vector3> estimates = new List<Vector3>();

        private Mesh mesh;

        int[] beforeTime;
        int[] pointsNumbers;
        List<MyTime[]> fileTimes;

        // Use this for initialization
        void Start() {
            mesh = new Mesh();
            reader = new PlyReader();
            walkPoints = new List<Vector3[][]>();
            walkColors = new List<Color[][]>();
            fileTimes = new List<MyTime[]>();
            walkHeadPos = new List<Vector3>();
            beforeTime = new int[kinectNums];
            pointsNumbers = new int[kinectNums];
            for (int i = 0; i < pointsNumbers.Length; i++) {
                pointsNumbers[i] = 0;
            }
            GetComponent<MeshFilter>().mesh = mesh;
            string dir = "result";
            LoadModels(dir);
            LoadIndexCSV(dir);
            var array = new int[10];
        }

        void Update() {
        }

        void FixedUpdate() {
            Destroy(GetComponent<MeshFilter>().mesh);
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;

            var points = new List<Vector3>();
            var colors = new List<Color>();
            var time = Time.deltaTime * 1000;
            for (int i = 0; i < kinectNums; i++) {
                beforeTime[i] += (int)Math.Floor(time);
                int index = pointsNumbers[i];
                var timeDiff = fileTimes[(index + 1) % fileTimes.Count][i].GetMilli() - fileTimes[index][i].GetMilli();
                if (beforeTime[i] > timeDiff)
                    pointsNumbers[i] = (pointsNumbers[i] + 1) % walkPoints.Count;
                foreach (var v in walkPoints[pointsNumbers[i]][i]) {
                    points.Add(v);
                }
                foreach (var c in walkColors[pointsNumbers[i]][i]) {
                    colors.Add(c);
                }
                Pointer.transform.position = this.transform.position + estimates[index % estimates.Count];
            }
            mesh.vertices = points.ToArray();
            mesh.colors = colors.ToArray();
            mesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
        }

        void LoadModels(string dir) {
            string baseDir = Path.Combine("polygons", dir);
            int num = 1;
            while (File.Exists(Path.Combine(baseDir, $"model_{num}_0.ply"))) {
                num++;
            }
            Vector3[][][] tmpPoints = new Vector3[num][][];
            Color[][][] tmpColors = new Color[num][][];
            Parallel.For(0, num, n => {
                var pointlist = new List<Point>[kinectNums];
                for (int i = 0; i < kinectNums; i++) {
                    var plist = new List<Point>();
                    var fileName = Path.Combine(baseDir, $"model_{n}_{i}.ply");
                    foreach (var p in reader.Load(fileName)) {
                        plist.Add(p);
                    }
                    if (i > 0) {
                        var source = new List<Point>();
                        for (int j = 0; j < i; j++) {
                            pointlist[j].ForEach(p => source.Add(p));
                        }
                        var sourceBorder = BorderPoints(source);
                        var destBorder = BorderPoints(plist);
                        //var sourceLine = CalcLine(SelectPoint(plist, source));
                        //var destLine = CalcLine(SelectPoint(source, plist));
                        float diffY = (float)CalcY(sourceBorder, destBorder);
                        //var diffXZ = CalcXZ(sourceLine, destLine);
                        if (diffY < 0.2) {
                            plist = plist.Select(p => p - new Vector3(0, diffY, 0)).ToList();
                        }
                    }
                    pointlist[i] = plist;
                }
                tmpPoints[n] = pointlist.Select(v => v.Select(p => p.GetVector3()).ToArray()).ToArray();
                tmpColors[n] = pointlist.Select(c => c.Select(p => p.GetColor()).ToArray()).ToArray();
            });
            walkPoints = tmpPoints.ToList();
            walkColors = tmpColors.ToList();
            for (int i = 1; i < walkColors.Count; i++) {
                EstimateArm(i);
            }
        }

        void LoadIndexCSV(string dir) {
            string baseDir = Path.Combine("polygons", dir);
            List<string[]> data = new List<string[]>();
            using (StreamReader reader = new StreamReader(Path.Combine(baseDir, "index.csv"))) {
                string str = reader.ReadLine();
                while(str != null) {
                    var split = str.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length > 0)
                        data.Add(split);
                    str = reader.ReadLine();
                }
            }
            for (int i = 0; i < data.Count; i += kinectNums) {
                var times = new MyTime[kinectNums];
                for (int j = 0; j < kinectNums; j++) {
                    times[j] = ParseTime(data[i][1]);
                }
                fileTimes.Add(times);
            }
        }

        List<Vector3> EstimateArm(int index) {
            var source = new List<Point>();
            var dest = new List<Point>();
            for (int i = 0; i < kinectNums; i++) {
                var svecs = walkPoints[0][i];
                var scols = walkColors[0][i];
                source.Add(new Point(svecs[0], scols[0]));
                var dvecs = walkPoints[index][i];
                var dcols = walkColors[index][i];
                dest.Add(new Point(dvecs[0], dcols[0]));
            }
            var sborder = BorderPoints(source);
            var dborder = BorderPoints(dest);
            if (sborder.Count > 0 && dborder.Count > 0) {
                var standard = sborder[0];
                Func<List<Point>, List<Point>, double> f = (p1, p2) =>
    Functions.SubColor(Functions.AverageColor(p1.Select(s => s.GetColor()).ToList()), Functions.AverageColor(p2.Select(s => s.GetColor()).ToList())).SqrLength()
     + (Functions.AverageVector(p1.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(p2.Select(s => s.GetVector3()).ToList())).sqrMagnitude;

                var min = dborder.Min(de => f(standard, de));
                var target = dborder.Find(d => Math.Abs(f(standard, d) - min) >= 0);
                if (target == null && dborder.Count > 0) {
                    dborder.ForEach(d => print(Math.Abs(f(standard, d) - min)));
                }
                var vecs = target.Select(t => t.GetVector3()).ToList();
                var vec = Functions.AverageVector(vecs);
                estimates.Add(vec);
            } else {
                estimates.Add(Vector3.zero);
            }

            return null;
        }

        MyTime ParseTime(string str) {
            var split = str.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            int hour, minute, second, millisecond;
            if (!int.TryParse(split[0], out hour)) return null;
            if (!int.TryParse(split[1], out minute)) return null;
            if (!int.TryParse(split[2], out second)) return null;
            if (!int.TryParse(split[3], out millisecond)) return null;
            return new MyTime(hour, minute, second, millisecond);
        }

        void LoadBody(string dir) {
            string baseDir = Path.Combine("polygons", dir);
            using (StreamReader reader = new StreamReader(Path.Combine(baseDir, "bodyposes.txt"))) {
                string str = reader.ReadLine();
                while(str != null) {
                    var split = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    int x = int.Parse(split[1]);
                    int y = int.Parse(split[2]);
                    int z = int.Parse(split[3]);
                    var point = new Point(x, y, z, 0, 0, 0);
                    bodyposes.Add(point.GetVector3() / 1000);
                    str = reader.ReadLine();
                }
            }
        }

        List<List<Point>> BorderPoints(List<Point> points) {
            var dictionary = new List<KeyValuePair<double, List<Point>>>();
            var border = new List<List<Point>>();
            var tmp = new List<Point>();
            foreach (var v in points) {
                tmp.Add(v);
            }
            tmp.Sort((t1, t2) => Math.Sign(t1.Y - t2.Y));
            while (tmp.Count > 0) {
                var v = tmp[0];
                var near = tmp.TakeWhile(t => Math.Abs(t.Y - v.Y) < 20).ToList();
                foreach (var n in near) {
                    tmp.Remove(n);
                }
                near.Sort((n1, n2) => Math.Sign(n1.GetVector3().sqrMagnitude - n2.GetVector3().sqrMagnitude));
                if (near.Count > 1) {
                    var medians = new List<double>();
                    var tmp2 = new List<Point>();
                    foreach (var n in near) {
                        tmp2.Add(n);
                    }
                    while(near.Count > 0) {
                        var first = near.First();
                        var last = near.Last();
                        near.Remove(first);
                        near.Remove(last);
                        medians.Add((first.GetColor() - last.GetColor()).SqrLength());
                    }
                    var median = medians.Median();
                    dictionary.Add(new KeyValuePair<double, List<Point>>(median, tmp2));
                }
            }
            for (int i = 0; i < dictionary.Count; i++) {
                var nears = dictionary[i].Value;
                var distance = dictionary.Min(d => {
                    if (d.Value == nears) {
                        return double.MaxValue;
                    } else {
                        var col1 = Functions.AverageColor(d.Value.Select(s => s.GetColor()).ToList());
                        var col2 = Functions.AverageColor(nears.Select(s => s.GetColor()).ToList());
                        return Functions.SubColor(col1, col2).SqrLength();
                    }
                });
                if (distance > 0.001) {
                    border.Add(dictionary[i].Value);
                }
            }
            return border;
        }

        List<Point> SelectPoint(List<Point> source, List<Point> dest) {
            var select = new List<Point>();
            var minX = source.Min(s => s.X);
            var maxX = source.Max(s => s.X);
            var minZ = source.Min(s => s.Z);
            var maxZ = source.Max(s => s.Z);
            foreach (var d in dest) {
                if (d.X >= minX && d.X <= maxX && d.Z >= minZ && d.Z <= maxZ) {
                    select.Add(d);
                }
            }
            return select;
        }

        List<XZLine> CalcLine(List<Point> points) {
            var tmp = new List<Point>();
            foreach (var p in points) {
                tmp.Add(p);
            }
            tmp.Sort((t1, t2) => Math.Sign(t1.GetVector3().sqrMagnitude - t2.GetVector3().sqrMagnitude));
            var lines = new List<XZLine>();
            int i = 0;
            while (tmp.Count > i) {
                var v = tmp[i];
                double num = 0.1;
                var near = tmp.FindAll(t => (t.GetVector3() - v.GetVector3()).sqrMagnitude < num * num);
                i += near.Count;
                var line = LeastSquareMethod(near);
                lines.Add(line);
            }
            return lines;
        }

        XZLine LeastSquareMethod(List<Point> points) {
            var data = new List<Vector2>();
            foreach (var p in points) {
                var vec = p.GetVector3();
                data.Add(new Vector2(vec.x, vec.z));
            }
            return XZLine.LeastSquareMethod(data);
        }

        double CalcY(List<List<Point>> source, List<List<Point>> destination) {
            var diffs = new List<double>();
            Func<List<Point>, List<Point>, double> f = (p1, p2) =>
                Functions.SubColor(Functions.AverageColor(p1.Select(s => s.GetColor()).ToList()), Functions.AverageColor(p2.Select(s => s.GetColor()).ToList())).SqrLength()
                 + (Functions.AverageVector(p1.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(p2.Select(s => s.GetVector3()).ToList())).sqrMagnitude;
            for (int i = 0; i < source.Count; i++) {
                var spoints = source[i];
                var min = destination.Min(b => f(b, spoints));
                var index = destination.FindIndex(b => f(b, spoints) == min);
                var dpoints = destination[index];
                var diff = Functions.AverageVector(dpoints.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(spoints.Select(s => s.GetVector3()).ToList());
                diffs.Add(diff.y);
            }

            return diffs.Median();
        }

        Vector2 CalcXZ(List<XZLine> source, List<XZLine> dest) {
            double[] mins = new double[source.Count];
            for (int i = 0; i < source.Count; i++) {
                mins[i] = dest.Min(d => Math.Abs(d.A - source[i].A));
            }
            var minimum = mins.Min();
            var index = Array.FindIndex(mins, m => m == minimum);
            XZLine sourceLine = source[index];
            XZLine destLine = dest.Find(d => d.A - sourceLine.A - minimum < 0.0000001);
            Vector2 destNormal = new Vector2((float)destLine.A, -1).normalized;
            var vecs = destLine.Vectors;
            var dists = vecs.Select(v => sourceLine.CalcDistance(v));
            return (float)dists.Average() * destNormal;
        }
    }

    class XZLine {
        public double A { get; private set; }
        public double B { get; private set; }
        public List<Vector2> Vectors { get; private set; }
        public XZLine(double a, double b, List<Vector2> vectors) {
            A = a;
            B = b;
            Vectors = vectors;
        }

        public XZLine(double a, double b)
            : this(a, b, new List<Vector2>()) {
        }

        public double Calc(double x, double z) {
            return A * x - z + B;
        }

        public double Calc(Vector2 vec) {
            return Calc(vec.x, vec.y);
        }

        public static XZLine LeastSquareMethod(List<Vector2> vectors) {
            double a, b;
            double sumX = vectors.Sum(v => v.x), sumY = vectors.Sum(v => v.y),
                sumXY = vectors.Sum(v => v.x * v.y), sumXX = vectors.Sum(v => v.x * v.x);
            int n = vectors.Count;
            a = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
            b = (sumXX * sumY - sumX * sumXY) / (n * sumXX - sumX * sumX);
            return new XZLine(a, b, vectors);
        }

        public double CalcDistance(Vector2 vector) {
            return Math.Abs(Calc(vector)) / Math.Sqrt(A * A + 1);
        }

        public override string ToString() {
            return $"z = {A}x + {B}";
        }
    }
}
