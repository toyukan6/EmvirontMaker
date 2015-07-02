using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson {
    class TextureManager : Singleton<TextureManager> {
        public Texture2D[][] Textures { get; private set; }
        public Vector2[][] StartPoses { get; private set; }
        public Vector2[][] EndPoses { get; private set; }

        void Start() {
            DontDestroyOnLoad(this);
        }

        public void SetTexture(Texture2D[][] textures) {
            Textures = textures;
        }

        public void SetStarts(Vector2[][] start) {
            StartPoses = start;
        }

        public void SetEnds(Vector2[][] ends) {
            EndPoses = ends;
        }
    }
}
