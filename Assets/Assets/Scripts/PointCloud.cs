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
            while (Directory.Exists(Path.Combine(baseDir, num.ToString()))) {
                string dirName = Path.Combine(baseDir, num.ToString());
            var vecs = new LinkedList<Vector3>();
                var cols = new LinkedList<Color>();
                for (int i = 0; i < 4; i++) {
                    var fileName = Path.Combine(dirName, "model_" + i + ".ply");
                    foreach (var p in reader.Load(fileName)) {
                        var v = p.GetVector3();
                        vecs.AddFirst(v);
                        cols.AddFirst(p.GetColor());
                    }
                }
                Vector3[] array = new Vector3[vecs.Count];
                vecs.CopyTo(array, 0);
                HeadSearch(array.ToList());
                points.Add(vecs.ToArray());
                colors.Add(cols.ToArray());
                num++;
            }
        }

        void HeadSearch(List<Vector3> vecs) {
        }
    }
}
