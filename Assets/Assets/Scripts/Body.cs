using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {

    class Hips {
        public LegParts LeftLeg { get; private set; }
        public LegParts RightLeg { get; private set; }
        public Spine Spine { get; private set; }
        public Hips(GameObject hips) {
            LeftLeg = new LegParts(hips.transform.FindChild("Character1_LeftUpLeg").gameObject, true);
            RightLeg = new LegParts(hips.transform.FindChild("Character1_RightUpLeg").gameObject, false);
            Spine = new Spine(hips.transform.FindChild("Character1_Spine").gameObject);
        }
    }

    class LegParts {
        public GameObject UpLeg { get; private set; }
        public GameObject Leg { get; private set; }
        public GameObject Foot { get; private set; }
        public GameObject ToeBase { get; private set; }
        public LegParts(GameObject leg, bool left) {
            UpLeg = leg.gameObject;
            string direction = (left) ? "Left" : "Right";
            this.Leg = leg.transform.FindChild("Character1_" + direction + "Leg").gameObject;
            Foot = this.Leg.transform.FindChild("Character1_" + direction + "Foot").gameObject;
            ToeBase = Foot.transform.FindChild("Character1_" + direction + "ToeBase").gameObject;
        }
    }

    class Spine {
        public GameObject Spine1 { get; private set; }
        public GameObject Spine2 { get; private set; }
        public ShoulderParts LeftShoulder { get; private set; }
        public ShoulderParts RightShoulder { get; private set; }
        public NeckParts Neck { get; private set; }
        public Spine(GameObject spine) {
            Spine1 = spine.transform.FindChild("Character1_Spine1").gameObject;
            Spine2 = Spine1.transform.FindChild("Character1_Spine2").gameObject;
            LeftShoulder = new ShoulderParts(Spine2.transform.FindChild("Character1_LeftShoulder").gameObject, true);
            RightShoulder = new ShoulderParts(Spine2.transform.FindChild("Character1_RightShoulder").gameObject, false);
            Neck = new NeckParts(Spine2.transform.FindChild("Character1_Neck").gameObject);
        }
    }

    class ShoulderParts {
        public GameObject Shoulder { get; private set; }
        public GameObject Arm { get; private set; }
        public GameObject ForeArm { get; private set; }
        public GameObject Hand { get; private set; }
        public ShoulderParts(GameObject shoulder, bool left) {
            Shoulder = shoulder;
            string direction = (left) ? "Left" : "Right";
            Arm = shoulder.transform.FindChild("Character1_" + direction + "Arm").gameObject;
            ForeArm = Arm.transform.FindChild("Character1_" + direction + "ForeArm").gameObject;
            Hand = ForeArm.transform.FindChild("Character1_" + direction + "Hand").gameObject;
        }
    }
    class NeckParts {
        public GameObject Neck { get; private set; }
        public GameObject Head { get; private set; }
        public NeckParts(GameObject neck) {
            Neck = neck;
            Head = neck.transform.FindChild("Character1_Head").gameObject;
        }
    }
}
