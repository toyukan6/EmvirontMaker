﻿using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityLib;

namespace EnvironmentMaker {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    class AdjustBody : MonoBehaviour {
        PlyReader reader;
        List<Vector3> bodyposes;
        int kinectNums = 4;
        public GameObject Pointer;
        List<GameObject> pointers = new List<GameObject>();
        Vector3[] estimates;
        List<Dictionary<JointType, Vector3>> bodyList;
        bool loadEnd = false;
        bool looped = false;
        List<Vector2> route = new List<Vector2>();
        int nextRouteIndex = 0;
        float length = 10f;
        Vector3 firstPosition;
        TwoDLine bodyLine;
        GameObject cursor;
        JointType selectedType = JointType.SpineBase;
        public GameObject Selected;
        private GameObject selectedPointer;
        public GameObject MainCamera;
        public GameObject OverViewCamera;
        private Vector3 stopPosition;
        private Vector3 stopEularAngle;
        private Vector3 movePosition;
        private Vector3 moveEularAngle;
        private bool stopped = true;
        private int baseIndex;
        private Vector3[,] firstBodyParts;
        private PolygonData[] polygonData;
        private int FrameAmount;
        private PolygonManager manager;

        private Mesh mesh;

        int[] beforeTime;
        int[] pointsNumbers;
        List<MyTime[]> fileTimes;

        public string DirName = "result";
        private string tmpDirName;

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
            LoadModels(DirName);
            LoadIndexCSV(DirName);
            LoadBodyDump(DirName);
            var array = new int[10];
            foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                var obj = Instantiate(Pointer) as GameObject;
                obj.name = Enum.GetName(typeof(JointType), type);
                obj.GetComponentInChildren<TextMesh>().text = obj.name;
                pointers.Add(obj);
            }
            var positions = new Vector3[pointers.Count];
            var thisPos = this.transform.position;
            foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                pointers[(int)type].transform.position = PartsPosition(type, 0);
            }
            firstBodyParts = new Vector3[FrameAmount, Enum.GetNames(typeof(JointType)).Length];
            baseIndex = 0;
            selectedPointer = Instantiate(Selected);
            stopPosition = this.transform.position;
            stopEularAngle = this.transform.localEulerAngles;
            movePosition = new Vector3(1.1f, 1.4f, -1);
            moveEularAngle = new Vector3(0, 0, 0);
            manager = GameObject.FindObjectOfType<PolygonManager>();
            tmpDirName = DirName;
            UpdateMesh();
        }

        double beforeMag = double.MaxValue;
        void Update() {
            int number = pointsNumbers[0];
            if (stopped) {
                GameObject selectedObj = pointers[(int)selectedType];

                if (Input.GetKey(KeyCode.D)) {
                    polygonData[number].Offsets[selectedType] += new Vector3(0.01f, 0, 0);
                } else if (Input.GetKey(KeyCode.A)) {
                    polygonData[number].Offsets[selectedType] -= new Vector3(0.01f, 0, 0);
                } else if (Input.GetKey(KeyCode.W)) {
                    polygonData[number].Offsets[selectedType] += new Vector3(0, 0, 0.01f);
                } else if (Input.GetKey(KeyCode.S)) {
                    polygonData[number].Offsets[selectedType] -= new Vector3(0, 0, 0.01f);
                } else if (Input.GetKey(KeyCode.X)) {
                    polygonData[number].Offsets[selectedType] += new Vector3(0, 0.01f, 0);
                } else if (Input.GetKey(KeyCode.Z)) {
                    polygonData[number].Offsets[selectedType] -= new Vector3(0, 0.01f, 0);
                }

                //selectedObj.transform.position = firstJoint[number, (int)selectedType] + offsets[number, (int)selectedType];
                selectedPointer.transform.position = selectedObj.transform.position;

                if (Input.GetKeyDown(KeyCode.DownArrow)) {
                    selectedType = (JointType)(((int)selectedType + 1) % Enum.GetNames(typeof(JointType)).Length);
                } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                    selectedType = (JointType)(((int)selectedType - 1 + Enum.GetNames(typeof(JointType)).Length) % Enum.GetNames(typeof(JointType)).Length);
                }

                int before = pointsNumbers[0];
                if (Input.GetKeyDown(KeyCode.RightArrow)) {
                    pointsNumbers[0] = (pointsNumbers[0] + 1) % FrameAmount;
                } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                    pointsNumbers[0] = (pointsNumbers[0] - 1 + FrameAmount) % FrameAmount;
                }
                if (before != pointsNumbers[0]) {
                    AdjustStopCamera(before);
                }
                OverViewCamera.transform.position = pointers[(int)selectedType].transform.position + new Vector3(0, 0.5f, 0);

                if (Input.GetKey(KeyCode.J)) {
                    OverViewCamera.transform.position += new Vector3(0, -0.01f, 0);
                } else if (Input.GetKey(KeyCode.K)) {
                    OverViewCamera.transform.position += new Vector3(0, 0.01f, 0);
                } else if (Input.GetKey(KeyCode.H)) {
                    OverViewCamera.transform.position += new Vector3(-0.01f, 0, 0);
                } else if (Input.GetKey(KeyCode.L)) {
                    OverViewCamera.transform.position += new Vector3(0.01f, 0, 0);
                }
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                stopped = !stopped;
                if (stopped) {
                    OverViewCamera.SetActive(true);
                    selectedPointer.SetActive(true);
                    MainCamera.transform.position = stopPosition;
                    MainCamera.transform.eulerAngles = stopEularAngle;
                    for (int i = 0; i < kinectNums; i++) {
                        pointsNumbers[i] = 0;
                        beforeTime[i] = 0;
                    }
                    var positions = new Vector3[pointers.Count];
                    var thisPos = this.transform.position;
                    var pn = pointsNumbers[0];
                    foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                        pointers[(int)type].transform.position = PartsPosition(type, pn);
                    }
                    UpdateMesh();
                } else {
                    OverViewCamera.SetActive(false);
                    selectedPointer.SetActive(false);
                    MainCamera.transform.position = movePosition;
                    MainCamera.transform.eulerAngles = moveEularAngle;
                    for (int i = 0; i < kinectNums; i++) {
                        pointsNumbers[i] = 0;
                        beforeTime[i] = 0;
                    }
                    CalcCorrection();
                }
            }
        }

        void FixedUpdate() {
            if (loadEnd) {
                bool changed = false;
                var time = Time.deltaTime * 1000;
                for (int i = 0; i < kinectNums; i++) {
                    if (!stopped) {
                        beforeTime[i] += (int)Math.Floor(time);
                        int index = pointsNumbers[i];
                        var timeDiff = fileTimes[(index + 1) % fileTimes.Count][i].GetMilli() - fileTimes[index][i].GetMilli();
                        if (beforeTime[i] > timeDiff) {
                            var before = pointsNumbers[i];
                            pointsNumbers[i] = (pointsNumbers[i] + 1) % FrameAmount;
                            changed = true;
                        }
                    }
                }
                if (changed) {
                    UpdateMesh();
                }
                var positions = new Vector3[pointers.Count];
                var thisPos = this.transform.position;
                var pn = pointsNumbers[0];
                foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                    pointers[(int)type].transform.position = PartsPosition(type, pn);
                }
            }
        }

        void UpdateMesh() {
            var oldMesh = mesh;
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
            DestroyImmediate(oldMesh);

            var points = new List<Vector3>();
            var colors = new List<Color>();
            for (int i = 0; i < kinectNums; i++) {
                foreach (var v in polygonData[pointsNumbers[i]].Positions[i]) {
                    points.Add(v);
                }
                foreach (var c in polygonData[pointsNumbers[i]].Colors[i]) {
                    colors.Add(c);
                }
            }
            mesh.vertices = points.ToArray();
            mesh.colors = colors.ToArray();
            mesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
        }

        private void CalcCorrection() {
            for (int i = 0; i < firstBodyParts.GetLength(0); i++) {
                var positions = new Vector3[pointers.Count];
                var thisPos = this.transform.position;
                foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                    firstBodyParts[i, (int)type] = PartsPosition(type, i);
                }
            }
            var reworkIndexes = new List<int>();
            for (int i = 0; i < FrameAmount; i++) {
                if (polygonData[i].Offsets.Values.ToList().Exists(o => o != Vector3.zero)) {
                    reworkIndexes.Add(i);
                }
            }
            ArmCorrection(reworkIndexes);
            LegCorrection(reworkIndexes);
        }

        private void ArmCorrection(List<int> reworkIndexes) {
            var partsNames = new[] { "Shoulder", "Elbow", "Wrist", "Hand", "HandTip", "Thumb" };
            var partsNamesLeft = new string[partsNames.Length + 1];
            var partsNamesRight = new string[partsNames.Length + 1];
            for (int i = 0; i < partsNames.Length; i++) {
                partsNamesLeft[i + 1] = partsNames[i] + "Left";
                partsNamesRight[i + 1] = partsNames[i] + "Right";
            }
            partsNamesLeft[0] = "SpineShoulder";
            partsNamesRight[0] = "SpineShoulder";
            EitherCorrection(reworkIndexes, partsNamesLeft);
            EitherCorrection(reworkIndexes, partsNamesRight);
        }

        private void LegCorrection(List<int> reworkIndexes) {
            var partsNames = new[] { "Hip", "Knee", "Ankle", "Foot" };
            var partsNamesLeft = new string[partsNames.Length + 1];
            var partsNamesRight = new string[partsNames.Length + 1];
            for (int i = 0; i < partsNames.Length; i++) {
                partsNamesLeft[i + 1] = partsNames[i] + "Left";
                partsNamesRight[i + 1] = partsNames[i] + "Right";
            }
            partsNamesLeft[0] = "SpineBase";
            partsNamesRight[0] = "SpineBase";
            EitherCorrection(reworkIndexes, partsNamesLeft);
            EitherCorrection(reworkIndexes, partsNamesRight);
        }

        private void EitherCorrection(List<int> reworkIndexes, string[] joints) {
            JointType[] partsTypes = joints.Select(p => (JointType)Enum.Parse(typeof(JointType), p)).ToArray();
            var allNumbers = Enumerable.Range(0, FrameAmount).ToList();
            reworkIndexes.ForEach(i => allNumbers.Remove(i));
            for (int i = 0; i < partsTypes.Length; i++) {
                bool handMade = false;
                var histgrams = new List<double[]>();
                var chistgrams = new List<double[]>();
                JointType firstJoint = partsTypes[i];
                var existsIndexes = new List<Vector3>();
                var existsCIndexes = new List<Vector3>();
                for (int j = 0; j < reworkIndexes.Count; j++) {
                    int nextIndex = reworkIndexes[j];
                    if (polygonData[nextIndex].Offsets[firstJoint] != Vector3.zero) {
                        handMade = true;
                        var nextIndexFirstJointIndex = polygonData[nextIndex].Voxel.GetIndexFromPosition(firstBodyParts[nextIndex, (int)firstJoint] - this.transform.position);
                        var nextIndexFirstJointVoxel = polygonData[nextIndex].Voxel[(int)nextIndexFirstJointIndex.x, (int)nextIndexFirstJointIndex.y, (int)nextIndexFirstJointIndex.z];
                        if (nextIndexFirstJointVoxel.Count > 0) {
                            var histgram = polygonData[nextIndex].Histgram[(int)nextIndexFirstJointIndex.x, (int)nextIndexFirstJointIndex.y, (int)nextIndexFirstJointIndex.z];
                            var chistgram = polygonData[nextIndex].ColorHistgram[(int)nextIndexFirstJointIndex.x, (int)nextIndexFirstJointIndex.y, (int)nextIndexFirstJointIndex.z];
                            if (histgram != null) {
                                histgrams.Add(histgram);
                                existsIndexes.Add(nextIndexFirstJointIndex);
                            }
                            if (chistgram != null) {
                                chistgrams.Add(chistgram);
                                existsCIndexes.Add(nextIndexFirstJointIndex);
                            }
                        }
                    }
                }
                Vector3 histVec = Vector3.zero, histCVec = Vector3.zero;
                double[] averageHistgram = null, averageCHistgram = null;
                if (histgrams.Count > 0) {
                    averageHistgram = new double[histgrams[0].Length];
                    for (int j = 0; j < averageHistgram.Length; j++) {
                            averageHistgram[j] = histgrams.Average(h => h[j]);
                    }
                }
                if (chistgrams.Count > 0) {
                    averageCHistgram = new double[chistgrams[0].Length];
                    for (int j = 0; j < averageCHistgram.Length; j++) {
                        averageCHistgram[j] = chistgrams.Average(h => h[j]);
                    }
                }
                var notFoundIndexes = new List<int>();
                foreach (int j in allNumbers) {
                    if (averageHistgram != null)
                        histVec = SearchHistgram(averageHistgram, j, existsIndexes, firstJoint);
                    if (averageCHistgram != null)
                        histCVec = SearchHistgram(averageCHistgram, j, existsIndexes, firstJoint);
                    Vector3 result = Vector3.zero;
                    if (histVec != Vector3.zero && histCVec != Vector3.zero) {
                        result = (histVec + histCVec) / 2;
                    } else if (histCVec != Vector3.zero) {
                        result = histCVec;
                    } else if (histVec != Vector3.zero) {
                        result = histVec;
                    } else {
                        notFoundIndexes.Add(j);
                    }
                    if (result != Vector3.zero)
                        print($"{j}の{firstJoint}:{result}");
                    if (polygonData[j].PartsCorrestion[firstJoint] != Vector3.zero) {
                        float alpha = 0.5f;
                        polygonData[j].PartsCorrestion[firstJoint] *= alpha;
                        polygonData[j].PartsCorrestion[firstJoint] += (1 - alpha) * result;
                    } else {
                        polygonData[j].PartsCorrestion[firstJoint] = result;
                    }
                }
                if (handMade) {
                    var startAndEnd = new List<Tuple<int, int>>();
                    int before = -1, start = -1, end = -1;
                    for (int j = 0; j < notFoundIndexes.Count; j++) {
                        if (before == -1) {
                            start = notFoundIndexes[j];
                        } else {
                            end = notFoundIndexes[j];
                            if (end - before > 1) {
                                startAndEnd.Add(Tuple.Create(start, before));
                                start = end;
                            }
                        }
                        before = notFoundIndexes[j];
                    }
                    startAndEnd.Add(Tuple.Create(start, end));
                    for (int j = 0; j < startAndEnd.Count; j++) {
                        start = startAndEnd[j].First - 1;
                        end = startAndEnd[j].Second;
                        try {
                            Vector3 startPosition = PartsPosition(firstJoint, start);
                            Vector3 endPosition = PartsPosition(firstJoint, end + 1);
                            Vector3 move = (endPosition - startPosition) / (end - start);
                            for (int k = start + 1; k <= end; k++) {
                                print($"{k}の{firstJoint}を補間");
                                polygonData[k].PartsCorrestion[firstJoint] = startPosition + move * (k - start) - PartsPosition(firstJoint, k);
                            }
                        } catch (Exception e) {
                            print(e.Message);
                        }
                    }
                }
            }
        }

        private Vector3 PartsPosition(JointType type, int i) {
            var diff = bodyList[baseIndex][type] - bodyList[baseIndex][JointType.SpineBase];
            var basePos = estimates[i % estimates.Length] + diff + polygonData[i].Offsets[type] + polygonData[i].PartsCorrestion[type];
            return this.transform.position + basePos;
        }

        private Vector3 SearchHistgram(double[] averageHistgram, int k, List<Vector3> existsIndexes, JointType firstJoint) {
            var histgramIndexes = new List<Vector3>();
            foreach (var e in existsIndexes) {
                Vector3 histgramIndex = polygonData[k].SearchHistgram(averageHistgram, e);
                if ((histgramIndex - e).magnitude < 4) {
                    histgramIndexes.Add(histgramIndex);
                }
            }
            if (histgramIndexes.Count > 0) {
                var histgramIndex = Functions.AverageVector(histgramIndexes);
                existsIndexes.Add(histgramIndex);
                Vector3 position = polygonData[k].Voxel.GetPositionFromIndex(histgramIndex);
                return this.transform.position + position - firstBodyParts[k, (int)firstJoint];
            } else return Vector3.zero;
        }

        void LoadModels(string dir) {
            string baseDir = $@"polygons\{dir}";
            int num = 0;
            while (File.Exists($@"{baseDir}\model_{num}_0.ply")) {
                num++;
            }
            FrameAmount = num;
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
                    if (i > 0) {
                        var source = new List<Point>();
                        for (int j = 0; j < i; j++) {
                            pointlist[j].ForEach(p => source.Add(p));
                        }
                        var sourceBorder = BorderPoints(source);
                        var destBorder = BorderPoints(plist);
                        //yield return n;
                        //var sourceLine = CalcLine(SelectPoint(plist, source));
                        //var destLine = CalcLine(SelectPoint(source, plist));
                        float diffY = (float)CalcY(sourceBorder, destBorder);
                        //yield return n;
                        //var diffXZ = CalcXZ(sourceLine, destLine);
                        if (diffY < 0.2) {
                            plist = plist.Select(p => p - new Vector3(0, diffY, 0)).ToList();
                        }
                    }
                    pointlist[i] = plist;
                    //yield return n;
                }
                //ApplyXZ(pointlist);
                polygonData[n] = new PolygonData(pointlist);
                //yield return n;
            }
            var sborder = BorderPoints(polygonData[0].Complete);
            ////yield return 0;
            var ymed = sborder.Min(s => Math.Abs(s.Average(v => v.Y)));
            var standard = sborder.Find(s => Math.Abs(s.Average(v => v.Y)) == ymed);
            estimates[0] = Functions.AverageVector(standard.Select(s => s.GetVector3()).ToList());
            ////yield return 0;
            //InitPartsCorrection();
            for (int i = 1; i < FrameAmount; i++) {
                EstimateHip(standard, polygonData[i].Complete, i);
                //yield return i;
                //CalcPartsCorrection(completeMerge[i].ToList(), i);
            }
            var diff = Functions.AverageVector(polygonData[0].Complete.Select(p => p.GetVector3()).ToList());
            var bayes = new Bayes();
            List<Vector2> result = bayes.BayesEstimate(polygonData[0].Complete.Select(p => p.GetVector3()).Select(p => new Vector2(p.x, p.z)).ToList());
            var firstPoint = result.First();
            var lastPoint = result.Last();
            bodyLine = new TwoDLine((lastPoint.y - firstPoint.y) / (lastPoint.x - firstPoint.x), (firstPoint.y * lastPoint.x - firstPoint.x * lastPoint.y) / (lastPoint.x - firstPoint.x));
            this.transform.position -= diff;
            firstPosition = this.transform.position;
            loadEnd = true;
        }

        Point[] ReducePoints(Point[] points) {
            var result = new List<Point>();
            var tmp = new List<Point>();
            int max = points.Length / 10;
            var rand = new System.Random();
            foreach (var p in points) {
                tmp.Add(p);
            }
            for (int i = 0; i < max; i++) {
                var point = tmp[rand.Next(tmp.Count)];
                result.Add(point);
                tmp.Remove(point);
            }

            return result.ToArray();
        }

        void LoadIndexCSV(string dir) {
            List<string[]> data = new List<string[]>();
            using (StreamReader reader = new StreamReader($@"polygons\{dir}\index.csv")) {
                string str = reader.ReadLine();
                while (str != null) {
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
                while (str != null) {
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

        void ApplyXZ(List<Point>[] points) {
            var correct = new[] { UpDown.Down, UpDown.Up, UpDown.Down, UpDown.Up };
            var bayes = new Bayes();
            var basepoints = new List<Point>();
            points[0].ForEach(p => basepoints.Add(p));
            points[3].ForEach(p => basepoints.Add(p));
            var averageVec = Functions.AverageVector(basepoints.Select(p => p.GetVector3()).ToList());
            for (int i = 0; i < points.Length; i++) {
                var border = BorderPoints(points[i]);
                var minIndex = border.IndexOfMin(s => Math.Abs(s.Average(v => v.Y)));
                var min = border[minIndex];
                var vec3s = min.Select(p => p.GetVector3());
                List<Vector2> bayesResult;
                if (i > 1) {
                    bayesResult = bayes.BayesEstimate(vec3s.Select(v => new Vector2(v.z, v.x)).ToList());
                } else {
                    bayesResult = bayes.BayesEstimate(vec3s.Select(v => new Vector2(v.x, v.z)).ToList());
                }
                var ud = IsUpDown(bayesResult);
                if (ud != correct[i]) {
                    var average = Functions.AverageVector(points[i].Select(p => p.GetVector3()).ToList());
                    var diff = averageVec;
                    diff = new Vector3(diff.x, 0, diff.z);
                    for (int j = 0; j < points[i].Count; j++) {
                        var moved = points[i][j].GetVector3();
                        moved -= diff;
                        moved = moved.RotateXZ(Math.PI);
                        moved += diff;
                        var color = points[i][j].GetColor();
                        points[i][j] = new Point(moved, color);
                    }
                }
            }
        }

        UpDown IsUpDown(List<Vector2> result) {
            var first = result.First();
            var last = result.Last();
            var average = (first + last) / 2;
            var bayesY = result[result.Count / 2].y;
            if (average.y < bayesY) {
                return UpDown.Up;
            } else if (average.y > bayesY) {
                return UpDown.Down;
            } else {
                return UpDown.Equal;
            }
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

        private void OnGUI() {
            GUI.TextArea(new Rect(0, 0, 100, 20), "選択中");
            GUI.TextArea(new Rect(0, 20, 100, 20), Enum.GetName(typeof(JointType), selectedType));

            if (stopped) {
                int before = pointsNumbers[0];
                pointsNumbers[0] = (int)GUI.HorizontalScrollbar(new Rect(0, 580, 800, 20), before, 1, 0, FrameAmount);
                if (before != pointsNumbers[0]) {
                    AdjustStopCamera(before);
                }
                GUI.TextArea(new Rect(100, 0, 100, 20), pointsNumbers[0].ToString());
            }

            if (GUI.Button(new Rect(700, 0, 100, 100), "終わる")) {
                SaveData();
                Application.LoadLevel("Second");
            }

            var beforeDir = tmpDirName;
            GUI.TextArea(new Rect(100, 40, 100, 20), "モーション名");
            tmpDirName = GUI.TextArea(new Rect(100, 60, 100, 20), beforeDir);
            if (beforeDir != tmpDirName && tmpDirName != "" && DirName != tmpDirName && Directory.Exists($@"polygons\{tmpDirName}")) {
                SaveData();
                DirName = tmpDirName;
                LoadModels(DirName);
                LoadIndexCSV(DirName);
                LoadBodyDump(DirName);
            }
        }

        private void SaveData() {
            manager.Data[DirName] = this.polygonData;
        }

        private void AdjustStopCamera(int before) {
            for (int i = 1; i < kinectNums; i++) {
                pointsNumbers[i] = pointsNumbers[0];
            }
            UpdateMesh();
            Vector3 nowCenter = Functions.AverageVector(polygonData[pointsNumbers[0]].Merge.Select(mp => mp.GetVector3()).ToList());
            Vector3 beforeCenter = Functions.AverageVector(polygonData[before].Merge.Select(mp => mp.GetVector3()).ToList());
            Vector3 moved = nowCenter - beforeCenter;
            MainCamera.transform.position += moved;
        }
    }
}
