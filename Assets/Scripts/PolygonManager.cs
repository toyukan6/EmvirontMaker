using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class PolygonManager : MonoBehaviour {
        public static PolygonManager Instance { get; private set; }
        public Dictionary<string, PolygonData[]> Data { get; private set; } = new Dictionary<string, PolygonData[]>();
        static string extensions = ".pldt";

        private void Awake() {
            if (Instance != null) {
                Destroy(this.gameObject);
            } else {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        private void Start() { }

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
    }
}
