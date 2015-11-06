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
        List<Vector3[]> walkPoints;
        List<Color[]> walkColors;
        List<Vector3> walkHeadPos;
        List<Vector3[]> jojoPoints;
        List<Color[]> jojoColors;
        List<Vector3> jojoHeadPos;
        List<Vector3> bodyposes;
        bool walk = true;

        private Mesh mesh;

        int pointsNumber = 0;

        // Use this for initialization
        void Start() {
            mesh = new Mesh();
            reader = new PlyReader();
            walkPoints = new List<Vector3[]>();
            walkColors = new List<Color[]>();
            walkHeadPos = new List<Vector3>();
            jojoPoints = new List<Vector3[]>();
            jojoColors = new List<Color[]>();
            jojoHeadPos = new List<Vector3>();

            GetComponent<MeshFilter>().mesh = mesh;
            LoadModels("result", walkPoints, walkColors);
            LoadModels("difficult", jojoPoints, jojoColors);
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

            var points = (walk) ? walkPoints : jojoPoints;
            var colors = (walk) ? walkColors : jojoColors;
            pointsNumber %= points.Count;
            mesh.vertices = points[pointsNumber];
            mesh.colors = colors[pointsNumber];
            mesh.SetIndices(Enumerable.Range(0, points[pointsNumber].Length).ToArray(), MeshTopology.Points, 0);
            pointsNumber = (pointsNumber + 1) % walkPoints.Count;
        }

        void LoadModels(string dir, List<Vector3[]> points, List<Color[]> colors) {
            string baseDir = Path.Combine("polygons", dir);
            int num = 1;
            while (File.Exists(Path.Combine(baseDir, "model_" + num + "_0.ply"))) {
                var vecs = new LinkedList<Vector3>();
                var cols = new LinkedList<Color>();
                for (int i = 0; i < 4; i++) {
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
                    float minY = 0, minListY = 0;
                    //var slices = RoundSlices(vecs);
                    if (vecs.Count > 0) {
                        minY = vecs.Min(v => v.y);
                        minListY = vlist.Min(v => v.y);
                    }
                    foreach (var v in vlist) {
                        vecs.AddFirst(v + new Vector3(0, minY - minListY, 0));
                    }
                    foreach (var c in clist) {
                        cols.AddFirst(c);
                    }
                }
                points.Add(vecs.ToArray());
                colors.Add(cols.ToArray());
                num++;
            }
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
