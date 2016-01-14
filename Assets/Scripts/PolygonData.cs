using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityLib;

namespace EnvironmentMaker {
    class PolygonData {
        public Vector3[][] Positions { get; private set; }
        public Color[][] Colors { get; private set; }
        public List<Point> Complete { get; private set; }
        public List<Point> Merge { get; private set; }
        public Voxel<List<Point>> Voxel { get; private set; }
        public Voxel<List<Point>> AnotherVoxel { get; private set; }
        public double[,,][] Histgram { get; private set; }
        public double[,,][] AnotherHistgram { get; private set; }
        public double[,,][] ColorHistgram { get; private set; }
        public double[,,][] AnotherColorHistgram { get; private set; }
        const int SECTIONNUMBER = 64;
        public Dictionary<JointType, Vector3> Offsets { get; private set; }
        public Dictionary<JointType, Vector3> PartsCorrestion { get; private set; }

        public PolygonData(List<Point>[] points, bool simple = false) {
            Positions = points.Select(v => v.Select(p => p.GetVector3()).ToArray()).ToArray();
            Colors = points.Select(v => v.Select(p => p.GetColor()).ToArray()).ToArray();
            Complete = new List<Point>();
            Merge = new List<Point>();
            for (int i = 0; i < points.Length; i++) {
                Complete = Complete.Concat(points[i]).ToList();
                Merge = Merge.Concat(ReducePoints(points[i])).ToList();
            }
            if (!simple) {
                PointToVoxel(Complete);
                Offsets = new Dictionary<JointType, Vector3>();
                PartsCorrestion = new Dictionary<JointType, Vector3>();
                foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                    Offsets[type] = Vector3.zero;
                    PartsCorrestion[type] = Vector3.zero;
                }
            }
        }

        void PointToVoxel(List<Point> basedata) {
            double maxX = Math.Ceiling(basedata.Max(p => p.GetVector3().x));
            double minX = Math.Floor(basedata.Min(p => p.GetVector3().x));
            double maxY = Math.Ceiling(basedata.Max(p => p.GetVector3().y));
            double minY = Math.Floor(basedata.Min(p => p.GetVector3().y));
            double maxZ = Math.Ceiling(basedata.Max(p => p.GetVector3().z));
            double minZ = Math.Floor(basedata.Min(p => p.GetVector3().z));
            //1 = 1メートル
            double delta = 0.05;
            int inverse = (int)(1 / delta);
            int width = (int)((maxX - minX) * inverse);
            int height = (int)((maxY - minY) * inverse);
            int depth = (int)((maxZ - minZ) * inverse);
            Voxel = new Voxel<List<Point>>(width, height, depth, minX, minY, minZ, delta);
            AnotherVoxel = new Voxel<List<Point>>(width, height, depth, minX - delta * 0.5, minY - delta * 0.5, minZ - delta * 0.5, delta);
            Histgram = new double[width, height, depth][];
            AnotherHistgram = new double[width, height, depth][];
            ColorHistgram = new double[width, height, depth][];
            AnotherColorHistgram = new double[width, height, depth][];
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    for (int k = 0; k < depth; k++) {
                        Voxel[i, j, k] = new List<Point>();
                        AnotherVoxel[i, j, k] = new List<Point>();
                    }
                }
            }
            foreach (var d in basedata) {
                var indexVec = Voxel.GetIndexFromPosition(d.GetVector3());
                Voxel[(int)indexVec.x, (int)indexVec.y, (int)indexVec.z].Add(d);
                var aindex = AnotherVoxel.GetIndexFromPosition(d.GetVector3());
                AnotherVoxel[(int)indexVec.x, (int)indexVec.y, (int)indexVec.z].Add(d);
            }
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    for (int k = 0; k < depth; k++) {
                        Histgram[i, j, k] = Histogram(Voxel[i, j, k].Select(v => v.GetVector3()).ToList());
                        AnotherHistgram[i, j, k] = Histogram(AnotherVoxel[i, j, k].Select(v => v.GetVector3()).ToList());
                        ColorHistgram[i, j, k] = Histogram(Voxel[i, j, k].Select(v => Functions.ColorToVector3(v.GetColor())).ToList());
                        AnotherColorHistgram[i, j, k] = Histogram(AnotherVoxel[i, j, k].Select(v => Functions.ColorToVector3(v.GetColor())).ToList());
                    }
                }
            }
        }

        List<Point> ReducePoints(List<Point> points) {
            var result = new List<Point>();
            var tmp = new List<Point>();
            int max = points.Count / 10;
            var rand = new System.Random();
            foreach (var p in points) {
                tmp.Add(p);
            }
            for (int i = 0; i < max; i++) {
                var point = tmp[rand.Next(tmp.Count)];
                result.Add(point);
                tmp.Remove(point);
            }

            return result;
        }

        double[] Histogram(List<Vector3> vectors) {
            var numericGroup = new List<double>();
            for (int i = 0; i < vectors.Count; i++) {
                for (int j = i + 1; j < vectors.Count; j++) {
                    numericGroup.Add((vectors[i] - vectors[j]).magnitude);
                }
            }
            if (numericGroup.Count < 2) {
                return null;
            } else if (numericGroup.All(n => n == numericGroup[0])) {
                double[] result = new double[SECTIONNUMBER];
                for (int i = 0; i < SECTIONNUMBER; i++) {
                    result[i] = 0;
                }
                result[SECTIONNUMBER / 2] = 1;
                return result;
            } else {
                double average = numericGroup.Average();
                double min = numericGroup.Min();
                double max = numericGroup.Max();
                int[] counts = new int[SECTIONNUMBER];
                for (int i = 0; i < SECTIONNUMBER; i++) {
                    counts[i] = 0;
                }
                double lowDelta = (average - min) / SECTIONNUMBER * 2;
                double highDelta = (max - average) / SECTIONNUMBER * 2;
                foreach (var n in numericGroup) {
                    double index;
                    if (n < average) {
                        index = n - min;
                        index = index / lowDelta;
                    } else {
                        index = n - average;
                        index = index / highDelta;
                        index += SECTIONNUMBER / 2;
                        if (index == SECTIONNUMBER) {
                            index -= 1;
                        }
                    }
                    counts[(int)index]++;
                }
                double sum = counts.Sum();
                double[] histogram = new double[SECTIONNUMBER];
                for (int i = 0; i < SECTIONNUMBER; i++) {
                    histogram[i] = counts[i] / sum;
                }
                return histogram;
            }
        }

        public Vector3 SearchHistgram(double[] histgram, Vector3 index) {
            return SearchHistgramFunction(histgram, Histgram, AnotherHistgram, index);
        }

        public Vector3 SearchColorHistgram(double[] histgram, Vector3 index) {
            return SearchHistgramFunction(histgram, ColorHistgram, AnotherColorHistgram, index);
        }

        Vector3 SearchHistgramFunction(double[] histgram, double[,,][] targetHist, double[,,][] targetAHist, Vector3 index) {
            Vector3 result = Vector3.zero, anotherResult = Vector3.zero;
            double minLength = double.MaxValue, anotherminLength = double.MaxValue;
            int depth = 2;
            int minX = (int)Math.Max(index.x - depth, 0);
            int maxX = (int)Math.Min(index.x + depth, Histgram.GetLength(0));
            int minY = (int)Math.Max(index.y - depth, 0);
            int maxY = (int)Math.Min(index.y + depth, Histgram.GetLength(1));
            int minZ = (int)Math.Max(index.z - depth, 0);
            int maxZ = (int)Math.Min(index.z + depth, Histgram.GetLength(2));
            for (int i = minX; i < maxX; i++) {
                for (int j = minY; j < maxY; j++) {
                    for (int k = minZ; k < maxZ; k++) {
                        double length = 0, alength = 0;
                        if (targetHist[i, j, k] != null) {
                            for (int l = 0; l < histgram.Length; l++) {
                                length += Math.Pow(Histgram[i, j, k][l] - histgram[l], 2);
                            }
                            length *= ((new Vector3(i, j, k) - index).magnitude + 1);
                            if (length < minLength) {
                                minLength = length;
                                result = new Vector3(i, j, k);
                            }
                        }
                        if (targetAHist[i, j, k] != null) {
                            for (int l = 0; l < histgram.Length; l++) {
                                alength += Math.Pow(AnotherHistgram[i, j, k][l] - histgram[l], 2);
                            }
                            alength *= ((new Vector3(i, j, k) - index).magnitude + 1);
                            if (alength < anotherminLength) {
                                anotherminLength = alength;
                                anotherResult = new Vector3(i, j, k);
                            }
                        }
                    }
                }
            }
            if ((result - anotherResult).magnitude > 4) {
                return ((index - result).magnitude < (index - anotherResult).magnitude ? result : anotherResult);
            }
            Vector3 ret = (result + anotherResult) * 0.5f;
            return new Vector3((int)ret.x, (int)ret.y, (int)ret.z);
        }
    }
}
