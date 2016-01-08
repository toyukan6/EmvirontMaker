using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class PolygonData {
        Vector3[][] positions;
        Color[][] colors;
        Voxel<List<Point>> voxel;
        Voxel<List<Point>> anothervoxel;
        double[,,][] histgram;
        double[,,][] anotherhistgram;

        public PolygonData(List<Point>[] points) {
            positions = points.Select(v => v.Select(p => p.GetVector3()).ToArray()).ToArray();
            colors = points.Select(v => v.Select(p => p.GetColor()).ToArray()).ToArray();
        }
    }
}
