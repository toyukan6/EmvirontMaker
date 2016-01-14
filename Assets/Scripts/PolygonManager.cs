using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class PolygonManager : MonoBehaviour {
        public static PolygonManager Instance { get; private set; }
        public Dictionary<string, PolygonData[]> Data;

        private void Awake() {
            if (Instance != null) {
                Destroy(this.gameObject);
            } else {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        private void Start() {
            Data = new Dictionary<string, PolygonData[]>();
        }
    }
}
