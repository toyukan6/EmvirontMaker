using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    static class Functions {
        static System.Random rand = new System.Random();

        public static int GetRandomInt(int max) => rand.Next(max);

        public static List<T3> ZipWith<T1, T2, T3>(List<T1> list1, List<T2> list2, Func<T1, T2, T3> func) {
            var result = new List<T3>();
            int max = Math.Min(list1.Count, list2.Count);
            for (int i = 0; i < max; i++) {
                result.Add(func(list1[i], list2[i]));
            }
            return result;
        }

        public static Color SubColor(Color c1, Color c2) {
            float red, green, blue, alpha;
            red = Math.Abs(c1.r - c2.r);
            green = Math.Abs(c1.g - c2.g);
            blue = Math.Abs(c1.b - c2.b);
            alpha = Math.Abs(c1.a - c2.a);
            return new Color(red, green, blue, alpha);
        }

        public static Vector3 AverageVector(List<Vector3> vecs) {
            if (vecs.Count > 0) {
                float x = 0, y = 0, z = 0;
                foreach (var v in vecs) {
                    x += v.x;
                    y += v.y;
                    z += v.z;
                }
                return new Vector3(x, y, z) / vecs.Count;
            } else {
                return Vector3.zero;
            }
        }

        public static Color AverageColor(List<Color> colors) {
            if (colors.Count > 0) {
                float red = 0, green = 0, blue = 0, alpha = 0;
                foreach (var c in colors) {
                    red += c.r;
                    green += c.g;
                    blue += c.b;
                    alpha += c.a;
                }
                red /= colors.Count;
                green /= colors.Count;
                blue /= colors.Count;
                alpha /= colors.Count;
                return new Color(red, green, blue, alpha);
            } else {
                return new Color(0, 0, 0, 0);
            }
        }

        public static Vector3 CrossProduct(Vector3 v1, Vector3 v2) {
            return new Vector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y - v2.x);
        }
    }
}
