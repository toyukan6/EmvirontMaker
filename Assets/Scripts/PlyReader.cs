﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class PlyReader {
        

        public PlyReader() {

        }

        public Point[] Load(string path) {
            Point[] points;
            int pointsNumber = 0;
            using(StreamReader reader = new StreamReader(path)) {
                string str = reader.ReadLine();
                if (str != "ply") return null;
                while (str != null) {
                    var split = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length == 3 && split[0] == "element" && split[1] == "vertex") {
                        pointsNumber = int.Parse(split[2]);
                    } else if (split.Length == 1 && split[0] == "end_header") {
                        break;
                    }
                    str = reader.ReadLine();
                }
                str = reader.ReadLine();
                points = new Point[pointsNumber];
                for (int i = 0; i < pointsNumber; i++) {
                    string data = reader.ReadLine();
                    points[i] = MakePoint(data);
                }
            }
            return points;
        }

        public Point MakePoint(string data) {
            var split = data.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 6) {
                return new Point(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), byte.Parse(split[3]), byte.Parse(split[4]), byte.Parse(split[5]));
            } else {
                return null;
            }
        }
    }

    class Point {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public byte Red { get; private set; }
        public byte Green { get; private set; }
        public byte Blue { get; private set; }
        public Point(float x, float y, float z, byte r, byte g, byte b) {
            X = x;
            Y = y;
            Z = z;
            Red = r;
            Green = g;
            Blue = b;
        }

        public Vector3 GetVector3() {
            return new Vector3(X, Y, Z) * 0.001f;
        }

        public Color GetColor() {
            return new Color(Red / (float)byte.MaxValue, Green / (float)byte.MaxValue, Blue / (float)byte.MaxValue, 1);
        }
    }
}