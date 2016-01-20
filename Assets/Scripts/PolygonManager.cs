﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class PolygonManager : MonoBehaviour {
        public static PolygonManager Instance { get; private set; }
        public Dictionary<string, PolygonData[]> Data { get; private set; } = new Dictionary<string, PolygonData[]>();
        public Dictionary<string, double[]> Histgrams { get; private set; } = new Dictionary<string, double[]>();
        static string extensions = ".pldt";
        static string histgramsDataName = "motion.dat";

        private void Awake() {
            if (Instance != null) {
                Destroy(this.gameObject);
            } else {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        private void Start() {
            LoadHistgrams();
        }

        public static void Save() {
            foreach (var d in Instance.Data) {
                using (var stream = new FileStream($"{d.Key}{extensions}", FileMode.OpenOrCreate)) {
                    using (var bwriter = new BinaryWriter(stream)) {
                        bwriter.Write(d.Key);
                        bwriter.Write(d.Value.Length);
                        foreach (var pd in d.Value) {
                            pd.Save(bwriter);
                        }
                    }
                }
            }
            SaveHistgrams();
        }

        public static void Load(string key) {
            string file = $"{key}{extensions}";
            if (File.Exists(file)) {
                using (var stream = new FileStream(file, FileMode.Open)) {
                    using (var breader = new BinaryReader(stream)) {
                        if (key == breader.ReadString()) {
                            int length = breader.ReadInt32();
                            for (int i = 0; i < length; i++) {
                                Instance.Data[key][i].Load(breader);
                            }
                        }
                    }
                }
            }
        }

        public static void SaveHistgrams() {
            using (var stream = new FileStream(histgramsDataName, FileMode.OpenOrCreate)) {
                using (var bwriter = new BinaryWriter(stream)) {
                    bwriter.Write(Instance.Histgrams.Count);
                    foreach (var h in Instance.Histgrams) {
                        bwriter.Write(h.Key);
                        bwriter.Write(h.Value.Length);
                        for (int i = 0; i < h.Value.Length; i++) {
                            bwriter.Write(h.Value[i]);
                        }
                    }
                }
            }
        }

        public static void LoadHistgrams() {
            if (File.Exists(histgramsDataName)) {
                using (var stream = new FileStream(histgramsDataName, FileMode.OpenOrCreate)) {
                    using (var breader = new BinaryReader(stream)) {
                        int hcount = breader.ReadInt32();
                        for (int i = 0; i < hcount; i++) {
                            string key = breader.ReadString();
                            int count = breader.ReadInt32();
                            var histgram = new double[count];
                            for (int j = 0; j < count; j++) {
                                histgram[j] = breader.ReadDouble();
                            }
                            Instance.Histgrams[key] = histgram;
                        }
                    }
                }
            }
        }
    }
}
