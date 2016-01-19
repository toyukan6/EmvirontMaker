using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityLib;

namespace EnvironmentMaker {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PointCloud : MonoBehaviour {

        PlyReader reader;
        List<Vector3> bodyposes;
        int kinectNums = 3;
        public GameObject Pointer;
        List<GameObject> pointers = new List<GameObject>();
        Vector3[] estimates;
        List<Dictionary<JointType, Vector3>> bodyList;
        bool looped = false;
        List<Vector2> route = new List<Vector2>();
        int nextRouteIndex = 0;
        float length = 10f;
        Vector3 firstPosition;
        Vector2? start;
        Vector2? end;
        private PolygonData[] polygonData;
        private int frameAmount;
        private bool loadEnd = false;

        private Mesh mesh;

        int[] beforeTime;
        int[] pointsNumbers;
        List<MyTime[]> fileTimes;

        public string DirName = "newpolygons";
        public GameObject SelectedPrehab;

        // Use this for initialization
        void Start() {
            mesh = new Mesh();
            reader = new PlyReader();
            fileTimes = new List<MyTime[]>();
            GetComponent<MeshFilter>().mesh = mesh;
            SetPolygonData(DirName);
            beforeTime = new int[kinectNums];
            pointsNumbers = new int[kinectNums];
            for (int i = 0; i < pointsNumbers.Length; i++) {
                pointsNumbers[i] = 0;
            }
            frameAmount = polygonData.Length;
            //foreach (JointType type in Enum.GetValues(typeof(JointType))) {
            //    var obj = Instantiate(Pointer) as GameObject;
            //    obj.name = Enum.GetName(typeof(JointType), type);
            //    pointers.Add(obj);
            //}
        }

        private void SetPolygonData(string name) {
            var manager = GameObject.FindObjectOfType<PolygonManager>();
            if (manager.Data.ContainsKey(name)) {
                loadEnd = true;
            } else {
                LoadModels(name);
            }
            PolygonManager.Load(name);
            polygonData = manager.Data[name];
            kinectNums = polygonData[0].Positions.Length;
            LoadIndexCSV(name);
        }

        public void Initialize(Vector2? start, Vector2? end) {
            this.start = start;
            this.end = end;
            if (start.HasValue) {
                var value = start.Value;
                this.transform.position = new Vector3(value.x, this.transform.position.y, value.y);
                Instantiate(SelectedPrehab, this.transform.position, Quaternion.Euler(90, 0, 0));
                if (end.HasValue) {
                    Vector2 diff = end.Value - start.Value;
                    double theta = Math.Atan2(-diff.y, diff.x) - Math.PI / 4;
                    var angle = this.transform.localEulerAngles;
                    this.transform.localEulerAngles = new Vector3(angle.x, (float)(theta * 180 / Math.PI), angle.z);
                    Instantiate(SelectedPrehab, new Vector3(end.Value.x, this.transform.position.y, end.Value.y), Quaternion.Euler(90, 0, 0));
                }
            }
            //var next = route[nextRouteIndex];
            //var nowIndex = pointsNumbers[0];
            //var startAvr = Functions.AverageVector(mergePoints[0].Select(p => p.GetVector3()).ToList());
            //var nowAvr = Functions.AverageVector(mergePoints[nowIndex].Select(p => p.GetVector3()).ToList());
            //var diffAvr = nowAvr - startAvr;
            //var mag = (this.transform.position + diff - new Vector3(next.x, 0, next.y)).sqrMagnitude;
            //if (mag < 10) {
            //    print("changed!");
            //    int before = nextRouteIndex;
            //    nextRouteIndex = (nextRouteIndex + 1) % route.Count;
            //    if (before > nextRouteIndex) {
            //        this.transform.position = firstPosition;
            //        nextRouteIndex = 1;
            //    }
            //    Vector2 target = route[nextRouteIndex];
            //    Vector3 target3 = new Vector3(target.x, this.transform.position.y, target.y);
            //    print(this.transform.position);
            //    this.transform.LookAt(target3);
            //    print(this.transform.position);
            //    this.transform.localEulerAngles -= new Vector3(0, 45, 0);
            //}
            //beforeMag = mag;
        }

        double beforeMag = double.MaxValue;
        void Update() {
        }

        void FixedUpdate() {
            if (loadEnd) {
                var oldMesh = mesh;
                mesh = new Mesh();
                GetComponent<MeshFilter>().mesh = mesh;
                DestroyImmediate(oldMesh);

                var points = new List<Vector3>();
                var colors = new List<Color>();
                var time = Time.deltaTime * 1000;
                for (int i = 0; i < kinectNums; i++) {
                    beforeTime[i] += (int)Math.Floor(time);
                    int index = pointsNumbers[i];
                    var timeDiff = fileTimes[(index + 1) % fileTimes.Count][i].GetMilli() - fileTimes[index][i].GetMilli();
                    if (index == fileTimes.Count - 1) {
                        timeDiff = 1000;
                    }
                    if (beforeTime[i] > timeDiff) {
                        var before = pointsNumbers[i];
                        pointsNumbers[i] = (pointsNumbers[i] + 1) % frameAmount;
                        if (before > pointsNumbers[i]) {
                            looped = true;
                        }
                    }
                    foreach (var v in polygonData[pointsNumbers[i]].Positions[i]) {
                        points.Add(v);
                    }
                    foreach (var c in polygonData[pointsNumbers[i]].Colors[i]) {
                        colors.Add(c);
                    }
                }
                if (looped) {
                    for (int i = 0; i < kinectNums; i++) {
                        pointsNumbers[i] = 0;
                        beforeTime[i] = 0;
                    }
                    var start = polygonData.First().Merge;
                    var last = polygonData.Last().Merge;
                    var startAverage = Functions.AverageVector(start.Select(s => s.GetVector3()).ToList());
                    var lastAverage = Functions.AverageVector(last.Select(s => s.GetVector3()).ToList());
                    var diff = lastAverage - startAverage;
                    diff.y = 0;
                    var theta = transform.localEulerAngles.y * Math.PI / 180;
                    this.transform.position += diff.RotateXZ(-theta);
                    if (end.HasValue) {
                        Vector2 value = end.Value;
                        Vector3 target = new Vector3(value.x, this.transform.position.y, value.y);
                        float sqrMagnitude = (target - this.transform.position).sqrMagnitude;
                        print(sqrMagnitude);
                        if (sqrMagnitude > beforeMag) {
                            GotoFirst();
                        } else {
                            beforeMag = sqrMagnitude;
                        }
                    } else {
                        float sqrMagnitude = (firstPosition - this.transform.position).sqrMagnitude;
                        if (sqrMagnitude > length * length) {
                            GotoFirst();
                        }
                    }
                    looped = false;
                }
                //var positions = new Vector3[pointers.Count];
                //var thisPos = this.transform.position;
                //var pn = pointsNumbers[0];
                //Parallel.ForEach<JointType>(Enum.GetValues(typeof(JointType)).Cast<JointType>(), type => {
                //    var diff = bodyList[pn][type] - bodyList[pn][(int)JointType.SpineBase];
                //    var basePos = estimates[pn % estimates.Length] + diff;
                //    positions[(int)type] = basePos;// ApplyPointerPos(mergePoints[pn].ToList(), basePos, type);
                //});
                //for (int i = 0; i < positions.Length; i++) {
                //if (partsCorrections[pn].ContainsKey((JointType)i)) {
                //    pointers[i].transform.position =  thisPos + partsCorrections[pn][(JointType)i];
                //} else {
                //    pointers[i].transform.position = thisPos + positions[i];
                //}
                //    pointers[i].transform.position = thisPos + positions[i];
                //}
                mesh.vertices = points.ToArray();
                mesh.colors = colors.ToArray();
                mesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
            }
        }

        private void GotoFirst() {
            this.transform.position = firstPosition;
            beforeMag = double.MaxValue;
        }

        public void AddMotion(string motionName) {
            PolygonData[] before = polygonData.Clone() as PolygonData[];
            Vector3 center = Functions.AverageVector(before.Last().Complete.Select(c => c.GetVector3()).ToList());
            SetPolygonData(motionName);
            Vector3 newCenter = Functions.AverageVector(polygonData.First().Complete.Select(c => c.GetVector3()).ToList());
            print(center);
            print(newCenter);
            foreach (var pd in polygonData) {
                for (int i = 0; i < pd.Positions.Length; i++) {
                    for (int j = 0; j < pd.Positions[i].Length; j++) {
                        pd.Positions[i][j] += (center - newCenter);
                    }
                }
            }
            polygonData = before.Concat(polygonData).ToArray();
        }

        void LoadIndexCSV(string dir) {
            List<string[]> data = new List<string[]>();
            using (StreamReader reader = new StreamReader($@"polygons\{dir}\index.csv")) {
                string str = reader.ReadLine();
                while(str != null) {
                    var split = str.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length > 0)
                        data.Add(split);
                    str = reader.ReadLine();
                }
            }
            var addTimes = new List<MyTime[]>();
            for (int i = 0; i < data.Count; i += kinectNums) {
                var times = new MyTime[kinectNums];
                for (int j = 0; j < kinectNums; j++) {
                    times[j] = ParseTime(data[i + j][1]);
                }
                addTimes.Add(times);
            }
            if (fileTimes.Count == 0) {
                fileTimes = addTimes;
            } else {
                MyTime[] lastTime = fileTimes.Last();
                MyTime[] firstTime = addTimes.First();
                int[] timeDiffs = new int[kinectNums];
                for (int i = 0; i < kinectNums; i++) {
                    timeDiffs[i] = lastTime[i].GetMilli() - firstTime[i].GetMilli();
                }
                for (int i = 0; i < addTimes.Count; i++) {
                    for (int j = 0; j < kinectNums; j++) {
                        addTimes[i][j].AddMilli(timeDiffs[j]);
                    }
                }
                fileTimes = fileTimes.Concat(addTimes).ToList();
            }
        }

        void LoadModels(string dir) {
            string baseDir = $@"polygons\{dir}";
            int num = 0;
            while (File.Exists($@"{baseDir}\model_{num}_0.ply")) {
                num++;
            }
            while (File.Exists($@"{baseDir}\model_0_{kinectNums - 1}.ply")) {
                kinectNums++;
            }
            kinectNums -= 1;
            frameAmount = num;
            polygonData = new PolygonData[num];
            var points = new Point[num][];
            estimates = new Vector3[num];
            for (int n = 0; n < num; n++) {
                var pointlist = new List<Point>[kinectNums];
                for (int i = 0; i < kinectNums; i++) {
                    var plist = new List<Point>();
                    var fileName = $@"{baseDir}\model_{n}_{i}.ply";
                    foreach (var p in reader.Load(fileName)) {
                        plist.Add(p);
                    }
                    //yield return n;
                    pointlist[i] = plist;
                    //yield return n;
                }
                //ApplyXZ(pointlist);
                polygonData[n] = new PolygonData(pointlist, true);
                //yield return n;
            }
            var manager = GameObject.FindObjectOfType<PolygonManager>();
            manager.Data[dir] = polygonData;
            Vector3 center = Functions.AverageVector(polygonData[0].Complete.Select(p => p.GetVector3()).ToList());
            foreach (var pd in polygonData) {
                for (int i = 0; i < pd.Positions.Length; i++) {
                    for (int j = 0; j < pd.Positions[i].Length; j++) {
                        pd.Positions[i][j] -= center;
                    }
                }
            }
            firstPosition = this.transform.position;
            loadEnd = true;
        }

        double CalcY(List<List<Point>> source, List<List<Point>> destination) {
            var diffs = new List<double>();
            Func<List<Point>, List<Point>, double> f = (p1, p2) =>
                (Functions.AverageColor(p1.Select(s => s.GetColor()).ToList()) - Functions.AverageColor(p2.Select(s => s.GetColor()).ToList())).SqrLength()
                 + (Functions.AverageVector(p1.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(p2.Select(s => s.GetVector3()).ToList())).sqrMagnitude;
            for (int i = 0; i < source.Count; i++) {
                var spoints = source[i];
                var index = destination.IndexOfMin(b => f(b, spoints));
                var dpoints = destination[index];
                var diff = Functions.AverageVector(dpoints.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(spoints.Select(s => s.GetVector3()).ToList());
                diffs.Add(diff.y);
            }

            return diffs.Median();
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
                    var lengthes = new List<double>();
                    var tmp2 = new Point[near.Count];
                    near.CopyTo(tmp2);
                    var max = Math.Sqrt(near.Count);
                    for (int i = 0; i < max && near.Count > 0; i++) {
                        var first = near.First();
                        var last = near.Last();
                        near.Remove(first);
                        near.Remove(last);
                        lengthes.Add((first.GetColor() - last.GetColor()).SqrLength());
                    }
                    var median = lengthes.Median();
                    dictionary.Add(new KeyValuePair<double, List<Point>>(median, tmp2.ToList()));
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

        void EstimateHip(List<Point> standard, List<Point> dest, int index) {
            var dborder = BorderPoints(dest);
            if (dborder.Count > 0) {
                Func<List<Point>, List<Point>, double> f = (p1, p2) =>
    Functions.SubColor(Functions.AverageColor(p1.Select(s => s.GetColor()).ToList()), Functions.AverageColor(p2.Select(s => s.GetColor()).ToList())).SqrLength()
     + (Functions.AverageVector(p1.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(p2.Select(s => s.GetVector3()).ToList())).sqrMagnitude;

                var min = dborder.Min(de => f(standard, de));
                var target = dborder.Find(d => Math.Abs(f(standard, d) - min) < 0.0000001);
                if (target == null && dborder.Count > 0) {
                    dborder.ForEach(d => print(Math.Abs(f(standard, d) - min)));
                }
                var vecs = target.Select(t => t.GetVector3()).ToList();
                var vec = Functions.AverageVector(vecs);
                if (Math.Abs(vec.y) < 0.1) {
                    estimates[index] = vec;
                } else {
                    estimates[index] = Functions.AverageVector(dest.Select(s => s.GetVector3()).ToList());
                }
            } else {
                estimates[index] = Vector3.zero;
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
            using (StreamReader reader = new StreamReader($@"polygons\{dir}\bodyposes.txt")) {
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

        public void Save(BinaryWriter writer) {
            writer.Write(DirName);
            writer.Write(start.HasValue);
            if (start.HasValue) {
                writer.Write(start.Value.x);
                writer.Write(start.Value.y);
            }
            writer.Write(end.HasValue);
            if (end.HasValue) {
                writer.Write(end.Value.x);
                writer.Write(end.Value.y);
            }
        }

        public static Tuple<string, Vector2?, Vector2?> Load(BinaryReader reader) {
            Vector2? start = null, end = null;
            string str = reader.ReadString();
            bool startHasValue = reader.ReadBoolean();
            if (startHasValue) {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                start = new Vector2(x, y);                
            }
            bool endHasValue = reader.ReadBoolean();
            if (endHasValue) {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                end = new Vector2(x, y);
            }
            return Tuple.Create(str, start, end);
        }
    }

    class TwoDLine {
        public double A { get; private set; }
        public double B { get; private set; }
        public List<Vector2> Vectors { get; private set; }
        public TwoDLine(double a, double b, List<Vector2> vectors) {
            A = a;
            B = b;
            Vectors = vectors;
        }

        public TwoDLine(double a, double b)
            : this(a, b, new List<Vector2>()) {
        }

        public double Calc(double x, double y) {
            return A * x - y + B;
        }

        public float CalcY(double x) {
            return (float)(A * x + B);
        }

        public double Calc(Vector2 vec) {
            return Calc(vec.x, vec.y);
        }

        public static TwoDLine LeastSquareMethod(List<Vector2> vectors) {
            double a, b;
            double sumX = vectors.Sum(v => v.x), sumY = vectors.Sum(v => v.y),
                sumXY = vectors.Sum(v => v.x * v.y), sumXX = vectors.Sum(v => v.x * v.x);
            int n = vectors.Count;
            a = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
            b = (sumXX * sumY - sumX * sumXY) / (n * sumXX - sumX * sumX);
            return new TwoDLine(a, b, vectors);
        }

        public override string ToString() {
            return "z = " + A + "x + " + B;
        }
    }

    static class IListExtension {
        public static int IndexOfMax<T>(this IList<T> list, Func<T, double> predicate) {
            double maxValue = double.MinValue;
            int maxIndex = -1;
            for (int i = 0; i < list.Count; i++) {
                var value = predicate(list[i]);
                if (value > maxValue) {
                    maxValue = value;
                    maxIndex = i;
                }
            }
            return maxIndex;
        }

        public static int IndexOfMin<T>(this IList<T> list, Func<T, double> predicate) {
            double minValue = double.MaxValue;
            int minIndex = -1;
            for (int i = 0; i < list.Count; i++) {
                var value = predicate(list[i]);
                if (value < minValue) {
                    minValue = value;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        public static double Median<T>(this IList<T> lists, Func<T, double> selector) {
            var list = lists.Select(p => selector(p)).ToList();
            return list.Median();
        }
    }

    static class Vector3Extension {
        public static Vector3 RotateXZ(this Vector3 vector, double theta) {
            var rotateX = Math.Cos(theta) * vector.x - Math.Sin(theta) * vector.z;
            var rotateZ = Math.Sin(theta) * vector.x + Math.Cos(theta) * vector.z;
            return new Vector3((float)rotateX, vector.y, (float)rotateZ);
        }
    }

    enum UpDown {
        Up,
        Down,
        Equal
    }
}
