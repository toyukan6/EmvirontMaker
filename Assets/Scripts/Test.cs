using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityLib;

public class Test : MonoBehaviour {
    string filePath = @"C:\Users\higuchi\Documents\修_120331\京都大学工学部履修120404\卒研\SelectedUserBody\SelectedUserBody.dump";
    //string timeDataPath = @"C:\Users\yoshino\Desktop\Data\Filtered\Student23TimeData.dump";
    List<Dictionary<int, float[]>> bodyList;
    List<DateTime> timeStamps;
    int frameIndex = 0;
    Dictionary<JointType, GameObject> BodyObjects;
    int counter = 0;
    bool stop = false;
    public GameObject Model;

    // Use this for initialization
    void Start() {
        bodyList = (List<Dictionary<int, float[]>>)Utility.LoadFromBinary(filePath);
        //timeStamps = (List<DateTime>)Utility.LoadFromBinary (timeDataPath);
        BodyObjects = new Dictionary<JointType, GameObject>();
    }

    void OnGUI() {
        //GUI.TextField(new Rect(800, 300, 100, 50), timeStamps[frameIndex].ToString("mm:ss.fff"));
    }

    void Play() {
        if (stop == false) {
            if (counter == 2) {
                counter = 0;
                RenderBody();
                frameIndex++;
                if (frameIndex >= bodyList.Count) {
                    frameIndex = 0;
                }
            } else {
                counter++;
            }
        }
    }

    void KeyInput() {
        if (Input.GetKeyUp(KeyCode.Space)) {
            if (stop == false) {
                stop = true;
            } else {
                stop = false;
            }
        }
        if (Input.GetKeyUp(KeyCode.RightArrow)) {
            frameIndex++;
            if (frameIndex == bodyList.Count) {
                frameIndex = 0;
            }
            RenderBody();
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow)) {
            frameIndex--;
            if (frameIndex == 0) {
                frameIndex = 0;
            }
            RenderBody();
        }
    }


    // Update is called once per frame
    void Update() {
        Play();
        KeyInput();
    }

    void RenderBody() {
        foreach (GameObject go in BodyObjects.Values) {
            Destroy(go);
        }

        Color color = Color.white;
        Dictionary<int, float[]> joints = bodyList[frameIndex];
        foreach (int jointNum in joints.Keys) {
            float[] point = joints[jointNum];
            if (point.Any(f => f > 1e10))
                continue;
            Vector3 vector = new Vector3(point[0], point[1], point[2]);
            vector *= 2;
            JointType jointType = Utility.ConvertIntToJointType(jointNum);
            GameObject jointObject = CreateJoint(vector);
            jointObject.name = jointType.ToString();
            jointObject.GetComponent<Renderer>().material.color = color;
            BodyObjects[jointType] = jointObject;
        }
    }

    GameObject CreateJoint(Vector3 point) {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.Translate(point);
        sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        return sphere;
    }
}
