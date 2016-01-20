using UnityEngine;
using System.Collections;
using System;

public class ReceiveFromEmitterEventArgs : EventArgs {
	public string dataIdentifier;
	public string dataType;
	public byte[] dataBytes;
	
	public ReceiveFromEmitterEventArgs(string argIdentifier, string argDataType, byte[] argDataBytes)
	{
		dataIdentifier = argIdentifier;
		dataType = argDataType;
		dataBytes = argDataBytes;
	}
}
