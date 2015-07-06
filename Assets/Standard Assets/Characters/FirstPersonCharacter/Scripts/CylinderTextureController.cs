using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson {
    class CylinderTextureController : MonoBehaviour {
        public void SetTexture(Texture2D texture) {
            var tex = new Texture2D(texture.width, texture.height);
            for (int i = 0; i < tex.height; i++) {
                for (int j = 0; j < tex.width; j++) {
                    tex.SetPixel(j, texture.height - 1 - i, texture.GetPixel(j, i));
                }
            }
            tex.Apply();
            this.GetComponent<MeshRenderer>().material.mainTexture = tex;
        }

        void Start() {
            this.transform.localEulerAngles = new Vector3(270, 0, 0);
        }
    }
}
