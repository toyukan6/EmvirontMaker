﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class Voxel<T> {
        T[,,] voxel;

        public T this[int i, int j, int k] {
            get { return voxel[i, j, k]; }
            set { voxel[i, j, k] = value; }
        }

        double startX;
        double startY;
        double startZ;
        double delta;

        public Voxel(int width, int height, int depth, double startX, double startY, double startZ, double delta) {
            voxel = new T[width, height, depth];
            this.startX = startX;
            this.startY = startY;
            this.startZ = startZ;
            this.delta = delta;
        }

        public T GetVoxelFromPosition(Vector3 position) {
            for (int i = 0; i < voxel.GetLength(0); i++) {
                double x = startX + delta * (i + 1);
                if (x > position.x) {
                    for (int j = 0; j < voxel.GetLength(1); j++) {
                        double y = startY + delta * (j + 1);
                        if (y > position.y) {
                            for (int k = 0; k < voxel.GetLength(2); k++) {
                                double z = startZ + delta * (k + 1);
                                if (z > position.z) {
                                    return voxel[i, j, k];
                                }
                            }
                        }
                    }
                }
            }
            return default(T);
        }

        public bool IsWithinArray(Vector3 index) {
            return (index.x >= 0 && index.y >= 0 && index.z >= 0 && index.x < voxel.GetLength(0) && index.y < voxel.GetLength(1) && index.z < voxel.GetLength(2));
        }

        public Vector3 GetIndexFromPosition(Vector3 position) {
            for (int i = 0; i < voxel.GetLength(0); i++) {
                double x = startX + delta * (i + 1);
                if (x > position.x) {
                    for (int j = 0; j < voxel.GetLength(1); j++) {
                        double y = startY + delta * (j + 1);
                        if (y > position.y) {
                            for (int k = 0; k < voxel.GetLength(2); k++) {
                                double z = startZ + delta * (k + 1);
                                if (z > position.z) {
                                    return new Vector3(i, j, k);
                                }
                            }
                        }
                    }
                }
            }
            return Vector3.zero;
        }

        public Vector3 GetPositionFromIndex(Vector3 index) {
            return new Vector3((float)(startX + delta * index.x), (float)(startY + delta * index.y), (float)(startZ + delta * index.z));
        }
    }
}