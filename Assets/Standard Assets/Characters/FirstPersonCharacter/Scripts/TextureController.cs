using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class TextureController : MonoBehaviour {

        private List<Vector3> positions;
        private Texture2D texture;
        private Vector3 startPosition;

        private void Start() {
            positions = new List<Vector3>();
            Texture2D tex = (Texture2D)GetComponent<MeshRenderer>().material.mainTexture;
            texture = new Texture2D(tex.width, tex.height);
            texture.SetPixels(tex.GetPixels());
            texture.Apply();
            startPosition = this.transform.position;
        }

        public Texture2D GetTexture() { return texture; }

        public void SetTexture2D(Texture2D texture) {
            this.texture = texture;
            GetComponent<MeshRenderer>().material.mainTexture = this.texture;
        }

        private void Update() {

        }

        private void FixedUpdate() {

        }
    }
}
