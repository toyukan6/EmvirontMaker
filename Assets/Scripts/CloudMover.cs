using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class CloudMover : MonoBehaviour {
        Hips hips;
        void Awake() {
            var reference = transform.FindChild("Character1_Reference").gameObject;
            var hip = reference.transform.FindChild("Character1_Hips").gameObject;
            hips = new Hips(hip);
        }
    }
}
