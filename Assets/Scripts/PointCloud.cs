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
        bool walk = true;
        int kinectNums = 4;

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
            if (Input.GetKeyDown(KeyCode.Space)) {
                walk = !walk;
            }
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
                if (beforeTime[i] > fileTimes[pointsNumbers[i] + 1][i].GetMilli() - fileTimes[pointsNumbers[i]][i].GetMilli())
                    pointsNumbers[i] = (pointsNumbers[i] + 1) % walkPoints.Count;
                foreach (var v in walkPoints[pointsNumbers[i]][i]) {
                    points.Add(v);
                }
                foreach (var c in walkColors[pointsNumbers[i]][i]) {
                    colors.Add(c);
                }
            }
            mesh.vertices = points.ToArray();
            mesh.colors = colors.ToArray();
            mesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
        }

        void LoadModels(string dir) {
            string baseDir = Path.Combine("polygons", dir);
            int num = 1;
            while (File.Exists(Path.Combine(baseDir, "model_" + num + "_0.ply"))) {
                num++;
            }
            Vector3[][][] tmpPoints = new Vector3[num][][];
            Color[][][] tmpColors = new Color[num][][];
            Parallel.For(0, num, n => {
                var pointlist = new List<Point>[kinectNums];
                for (int i = 0; i < kinectNums; i++) {
                    var plist = new List<Point>();
                    var fileName = Path.Combine(baseDir, "model_" + n + "_" + i + ".ply");
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
                        float diff = (float)CalcY(sourceBorder, destBorder);
                        if (diff < 0.2) {
                            plist = plist.Select(p => p - new Vector3(0, diff, 0)).ToList();
                        }
                    }

                    pointlist[i] = plist;
                }
                tmpPoints[n] = pointlist.Select(v => v.Select(p => p.GetVector3()).ToArray()).ToArray();
                tmpColors[n] = pointlist.Select(c => c.Select(p => p.GetColor()).ToArray()).ToArray();
            });
            walkPoints = tmpPoints.ToList();
            walkColors = tmpColors.ToList();
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
                if (near.Count > 1) {
                    var medians = new List<double>();
                    foreach (var n in near) {
                        var magnitudes = near.Select(n2 => (double)(n.GetVector3() - n2.GetVector3()).sqrMagnitude);
                        medians.Add(magnitudes.Average());
                    }
                    var median = medians.Average();
                    dictionary.Add(new KeyValuePair<double, List<Point>>(median, near));
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
    }
}
