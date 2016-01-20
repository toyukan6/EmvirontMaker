using UnityEngine;

using DealFuncPlug;

public class DealComm : MonoBehaviour {

	// Related to DealEmitter
	DealFuncPlugBase unityFuncPlug;
	public static string connectionState = "";
	public static short[] receivedHueVoxData;

	// Use this for initialization
	void Start () {
		unityFuncPlug = new DealFuncPlugBase("localhost", 48200);
		connectionState = unityFuncPlug.ConnectionStatus;
		unityFuncPlug.ReceiveFromEmitter += unityFuncPlug_ReceiveFromEmitter;
		unityFuncPlug.RegisterTrigger ("CreateHueVoxParticles", "short[]", "No");
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
		}
	}
}
