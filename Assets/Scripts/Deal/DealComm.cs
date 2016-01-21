using UnityEngine;

using DealFuncPlug;
using System.Collections.Generic;
using System;

public class DealComm : MonoBehaviour {

	// Related to DealEmitter
	DealFuncPlugBase unityFuncPlug;
	public static string connectionState = "";
	public static short[] receivedHueVoxData;
	public static Dictionary<string, Dictionary<string, double>> receivedBodyData;
    public static double MovingState { get; private set; }

    // Use this for initialization
    void Start() {
        unityFuncPlug = new DealFuncPlugBase("localhost", 48200);
        connectionState = unityFuncPlug.ConnectionStatus;
        unityFuncPlug.ReceiveFromEmitter += unityFuncPlug_ReceiveFromEmitter;
        unityFuncPlug.RegisterTrigger("MovingState", "double", "No");
    }
	
	// Update is called once per frame
	void Update () {
    }

    // Data Receiving Event
    void unityFuncPlug_ReceiveFromEmitter(object sender, DealFuncPlug.ReceiveFromEmitterEventArgs e) {
        switch (e.dataIdentifier) {
            case "CreateHueVoxParticles":
                receivedHueVoxData = (short[])unityFuncPlug.DataDeserializing(e.dataBytes);
                break;
            case "KinectBodyData":
                receivedBodyData = (Dictionary<string, Dictionary<string, double>>)unityFuncPlug.DataDeserializing(e.dataBytes);
                break;
            case "MovingState":
                MovingState = (double)unityFuncPlug.DataDeserializing(e.dataBytes);
                break;
        }
	}
}
