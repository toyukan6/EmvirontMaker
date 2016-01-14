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
        int kinectNums = 4;
        public GameObject Pointer;
        List<GameObject> pointers = new List<GameObject>();
        Vector3[] estimates;
        List<Dictionary<JointType, Vector3>> bodyList;
        bool looped = false;
        List<Vector2> route = new List<Vector2>();
        int nextRouteIndex = 0;
        float length = 10f;
        Vector3 firstPosition;
        Vector3? start;
        Vector3? end;
        private PolygonData[] polygonData;
        private int frameAmount;

        private Mesh mesh;

        int[] beforeTime;
        int[] pointsNumbers;
        List<MyTime[]> fileTimes;

        public string DirName = "newpolygons";

        // Use this for initialization
        void Start() {
            mesh = new Mesh();
            reader = new PlyReader();
            fileTimes = new List<MyTime[]>();
            beforeTime = new int[kinectNums];
            pointsNumbers = new int[kinectNums];
            for (int i = 0; i < pointsNumbers.Length; i++) {
                pointsNumbers[i] = 0;
            }
            GetComponent<MeshFilter>().mesh = mesh;
            LoadIndexCSV(DirName);
            var manager = GameObject.FindObjectOfType<PolygonManager>();
            polygonData = manager.Data[DirName];
            frameAmount = polygonData.Length;
            //foreach (JointType type in Enum.GetValues(typeof(JointType))) {
            //    var obj = Instantiate(Pointer) as GameObject;
            //    obj.name = Enum.GetName(typeof(JointType), type);
            //    pointers.Add(obj);
            //}
        }

        public void Initialize(Vector2? start, Vector2? end) {
            this.start = start;
            this.end = end;
            if (start.HasValue) {
                var value = start.Value;
                this.transform.position = new Vector3(value.x, this.transform.position.y, value.y);
                if (end.HasValue) {
                    Vector2 diff = end.Value - start.Value;
                    double theta = Math.Atan2(-diff.y, diff.x) - Math.PI / 4;
                    var angle = this.transform.localEulerAngles;
                    this.transform.localEulerAngles = new Vector3(angle.x, (float)(theta * 180 / Math.PI), angle.z);
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
                var theta = transform.localEulerAngles.y * Math.PI / 180;
                this.transform.position += diff.RotateXZ(-theta);
                if (end.HasValue) {
                    Vector2 value = end.Value;
                    Vector3 target = new Vector3(value.x, this.transform.position.y, value.y);
                    float sqrMagnitude = (target - this.transform.position).sqrMagnitude;
                    double threshold = 3;
                    print(sqrMagnitude);
                    if (sqrMagnitude < threshold * threshold) {
                        GotoFirst();
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

        private void GotoFirst() {
            this.transform.position = firstPosition;
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
            for (int i = 0; i < data.Count; i += kinectNums) {
                var times = new MyTime[kinectNums];
                for (int j = 0; j < kinectNums; j++) {
                    times[j] = ParseTime(data[i][1]);
                }
                fileTimes.Add(times);
            }
        }

        void LoadBodyDump(string dir) {
            string filePath = $@"polygons\{dir}\SelectedUserBody.dump";
            var bodyList = (List<Dictionary<int, float[]>>)Utility.LoadFromBinary(filePath);
            this.bodyList = bodyList.Select(bl => bl.ToDictionary(d => (JointType)d.Key, d => new Vector3(d.Value[0], d.Value[1], d.Value[2]))).ToList();
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
