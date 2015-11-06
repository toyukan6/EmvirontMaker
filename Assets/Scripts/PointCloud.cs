using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        int dotNums = 4;

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
            beforeTime = new int[dotNums];
            pointsNumbers = new int[dotNums];
            for (int i = 0; i < pointsNumbers.Length; i++) {
                pointsNumbers[i] = 0;
            }
            GetComponent<MeshFilter>().mesh = mesh;
            string dir = "result";
            LoadModels(dir, walkPoints, walkColors);
            LoadIndexCSV(dir);
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
            for (int i = 0; i < dotNums; i++) {
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

        void LoadModels(string dir, List<Vector3[][]> points, List<Color[][]> colors) {
            string baseDir = Path.Combine("polygons", dir);
            int num = 1;
            while (File.Exists(Path.Combine(baseDir, "model_" + num + "_0.ply"))) {
                var vllist = new LinkedList<Vector3>[dotNums];
                var cllist = new LinkedList<Color>[dotNums];
                for (int i = 0; i < dotNums; i++) {
                    var plist = new List<Point>();
                    var vlist = new LinkedList<Vector3>();
                    var clist = new LinkedList<Color>();
                    var fileName = Path.Combine(baseDir, "model_" + num  + "_" + i + ".ply");
                    foreach (var p in reader.Load(fileName)) {
                        plist.Add(p);
                    }
                    plist.Sort((p1, p2) => Math.Sign(p1.Y - p2.Y));
                    foreach (var p in plist) {
                        vlist.AddLast(p.GetVector3());
                        clist.AddLast(p.GetColor());
                    }
                    //var slice = RoundSlices(vlist);
                    vllist[i] = vlist;
                    cllist[i] = clist;
                    //foreach (var v in vlist) {
                    //    vecs.AddFirst(v + new Vector3(0, minY - minListY, 0));
                    //}
                    //foreach (var c in clist) {
                    //    cols.AddFirst(c);
                    //}
                }
                Vector3[][] varray = new Vector3[dotNums][];
                Color[][] carray = new Color[dotNums][];
                for (int i = 0; i < vllist.Length; i++) {
                    varray[i] = vllist[i].ToArray();
                }
                for (int i = 0; i < cllist.Length; i++) {
                    carray[i] = cllist[i].ToArray();
                }
                points.Add(varray);
                colors.Add(carray);
                num++;
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
            for (int i = 0; i < data.Count; i += dotNums) {
                var times = new MyTime[dotNums];
                for (int j = 0; j < dotNums; j++) {
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

        void HeadSearch(List<Vector3> vecs) {        }
    }
}
