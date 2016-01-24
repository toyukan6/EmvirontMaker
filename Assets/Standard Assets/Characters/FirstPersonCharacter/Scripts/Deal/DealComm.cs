using UnityEngine;

using DealFuncPlug;
using System.Collections.Generic;
using System;
using System.Linq;

public class DealComm : MonoBehaviour {

    // Related to DealEmitter
    DealFuncPlugBase unityFuncPlug;
    public static string connectionState = "";
    public static short[] receivedHueVoxData;
    public static List<Dictionary<string, Dictionary<string, double>>> bodyHistoryList = new List<Dictionary<string, Dictionary<string, double>>>();
    public static Dictionary<string, double> receivedBodyDirection;
    public static double MovingState { get; private set; }
    public static double ThrowBallFlag { get; private set; }
    public static double IsRaiseRH { get; private set; }
    public static double IsRaiseLH { get; private set; }
    public static int Degree { get; private set; }
    static HandFlags handFlag;

    // Use this for initialization
    void Start() {
        unityFuncPlug = new DealFuncPlugBase("localhost", 48200);
        connectionState = unityFuncPlug.ConnectionStatus;
        unityFuncPlug.ReceiveFromEmitter += unityFuncPlug_ReceiveFromEmitter;
        unityFuncPlug.RegisterTrigger("MainBodyData", "Dictionary<string, Dictionary<string, double>>", "No");
        unityFuncPlug.RegisterTrigger("BodyDirection", "Dictionary<string, double>", "No");
    }

    List<int> bodyDirDegHistory = new List<int>();

    // Update is called once per frame
    void Update() {
        if (receivedBodyDirection != null) {
            double s;
            int deg;
            s = Math.Acos(receivedBodyDirection["X"] / Math.Sqrt(receivedBodyDirection["X"] * receivedBodyDirection["X"] + receivedBodyDirection["Z"] * receivedBodyDirection["Z"])); // 角度θを求める
            s = (s / Math.PI) * 180.0; // ラジアンを度に変換
            if (receivedBodyDirection["Z"] < 0) s = 360 - s; // θ＞πの時
            deg = (int)Math.Floor(s);
            if ((s - deg) >= 0.5) deg++; // 小数点を四捨五入
            deg = deg - 180;
            bodyDirDegHistory.Add(deg);
            if (bodyDirDegHistory.Count > 10) bodyDirDegHistory.RemoveAt(0);
            // 履歴からメディアンフィルタをかける
            int[] tmpDirHistory = bodyDirDegHistory.ToArray();
            Array.Sort(tmpDirHistory);
            deg = tmpDirHistory[(int)Math.Floor(tmpDirHistory.Length / 2.0)];
            Degree = deg;
        }
        if (bodyHistoryList.Count > 10) {
            handFlag = 0;
            var raiseLeftHands = new List<HandFlags>();
            for (int i = 0; i < bodyHistoryList.Count; i++) {
                // 左手のY座標が左肩からどのぐらい離れているか取得
                try {
                    if (Math.Abs(bodyHistoryList[i]["HandLeft"]["Y"] - bodyHistoryList[i]["ShoulderLeft"]["Y"]) < 0.1) {
                        raiseLeftHands.Add(HandFlags.LeftHandMiddle);
                    } else if (bodyHistoryList[i]["HandLeft"]["Y"] - bodyHistoryList[i]["ShoulderLeft"]["Y"] > 0) {
                        raiseLeftHands.Add(HandFlags.LeftHandUp);
                    } else {
                        raiseLeftHands.Add(HandFlags.LeftHandDown);
                    }
                } catch (NullReferenceException e) {
                    print(e.Message);
                }
            }

            var raiseRightHands = new List<HandFlags>();
            // 右腕の位置関係の把握
            for (int i = 0; i < bodyHistoryList.Count; i++) {
                // 右手のY座標が右肩からどのぐらい離れているか取得
                if (Math.Abs(bodyHistoryList[i]["HandRight"]["Y"] - bodyHistoryList[i]["ShoulderRight"]["Y"]) < 0.1) {
                    raiseRightHands.Add(HandFlags.RightHandMiddle);
                } else if (bodyHistoryList[i]["HandRight"]["Y"] - bodyHistoryList[i]["ShoulderRight"]["Y"] > 0) {
                    raiseRightHands.Add(HandFlags.RightHandUp);
                } else {
                    raiseRightHands.Add(HandFlags.RightHandDown);
                }
            }
            foreach (HandFlags flag in Enum.GetValues(typeof(HandFlags))) {
                if (raiseLeftHands.All(r => r == flag) || raiseRightHands.All(r => r == flag)) {
                    handFlag |= flag;
                }
            }
        }
    }

    public static bool GetHandFlag(HandFlags flag) {
        return (handFlag & flag) == flag;
    }

    public static bool GetHandFlag(params HandFlags[] flag) {
        return flag.All(f => GetHandFlag(f));
    }

    // Data Receiving Event
    void unityFuncPlug_ReceiveFromEmitter(object sender, DealFuncPlug.ReceiveFromEmitterEventArgs e) {
        switch (e.dataIdentifier) {
            case "MainBodyData":
                bodyHistoryList.Add((Dictionary<string, Dictionary<string, double>>)unityFuncPlug.DataDeserializing(e.dataBytes));
                if (bodyHistoryList.Count > 11) bodyHistoryList.RemoveAt(0);
                break;
            case "BodyDirection":
                receivedBodyDirection = (Dictionary<string, double>)unityFuncPlug.DataDeserializing(e.dataBytes);
                break;
        }
    }
}

public enum HandFlags {
    RightHandDown = 1,
    RightHandMiddle = 2,
    RightHandUp = 4,
    LeftHandDown = 8,
    LeftHandMiddle = 16,
    LeftHandUp = 32,
}
