using UnityEngine;
using System.Collections;
using System;

using MultiNetLinkDeal;

// 今は使っていないが使うことになるかもしれないので残しておく
// ReceiveFromEmitterEventArgs.csとセット
public class DealFuncPlugUnity {

	public delegate void ReceiveFromEmitterEventHandler(object sender, ReceiveFromEmitterEventArgs e);
	public event ReceiveFromEmitterEventHandler ReceiveFromEmitter;

	protected virtual void OnReceiveFromEmitter(ReceiveFromEmitterEventArgs e)
	{
		if (ReceiveFromEmitter != null)
		{
			ReceiveFromEmitter(this, e);
		}
	}

	private MultiNetLink linkerForDealEmitter;
	private string linkedClientID;
	char[] deleteChars = { ' ', '\r', '\n', '\t', '\0' };

	public string ConnectionStatus { get; set; }

	public void InitFuncPlug()
	{
		linkerForDealEmitter = new MultiNetLink();
		linkerForDealEmitter.DataReceived += linkerForDealEmitter_DataReceived;
		linkedClientID = linkerForDealEmitter.InitDualPath("127.0.0.1", 48200);
		if (linkedClientID == "error" || linkedClientID == "timeout")
		{
			ConnectionStatus = linkedClientID;
			linkedClientID = "";
		}
		else
		{
			ConnectionStatus = "connected";
		}
	}

	public void RegisterTrigger(string deTriggerName, string dataType, string remoteFetch)
	{
		if (ConnectionStatus != "connected") return;
		
		byte[] curIdentifier = System.Text.Encoding.Unicode.GetBytes("deTrigger");
		byte[] curSepalator = System.Text.Encoding.Unicode.GetBytes("^_^");
		byte[] curDataType = System.Text.Encoding.Unicode.GetBytes(typeof(string).ToString());
		byte[] curData = System.Text.Encoding.Unicode.GetBytes(deTriggerName + ">_<" + dataType + ">_<" + remoteFetch);

		int sendMessageLength = 256 + 3 + 256 + 3 + curData.Length;
		byte[] sendMessage = new byte[sendMessageLength];

		curIdentifier.CopyTo(sendMessage, 0);
		curSepalator.CopyTo(sendMessage, 256);
		curDataType.CopyTo(sendMessage, 256 + 3);
		curSepalator.CopyTo(sendMessage, 256 + 3 + 256);
		curData.CopyTo(sendMessage, 256 + 3 + 256 + 3);

		linkerForDealEmitter.SendSerializedData(linkedClientID, sendMessage);
	}
	
	public void SendToEmitter(string deTriggerName, string dataType, byte[] dataBytes)
	{
		if (ConnectionStatus != "connected") return;
		
		byte[] curIdentifier = System.Text.Encoding.Unicode.GetBytes(deTriggerName);
		byte[] curSepalator = System.Text.Encoding.Unicode.GetBytes("^_^");
		byte[] curDataType = System.Text.Encoding.Unicode.GetBytes(dataType);

		int sendMessageLength = 256 + 3 + 256 + 3 + dataBytes.Length;
		byte[] sendMessage = new byte[sendMessageLength];

		curIdentifier.CopyTo(sendMessage, 0);
		curSepalator.CopyTo(sendMessage, 256);
		curDataType.CopyTo(sendMessage, 256 + 3);
		curSepalator.CopyTo(sendMessage, 256 + 3 + 256);
		dataBytes.CopyTo(sendMessage, 256 + 3 + 256 + 3);

		linkerForDealEmitter.SendSerializedData(linkedClientID, sendMessage);
	}
	
	void linkerForDealEmitter_DataReceived(object sender, DataReceivedMNLEventArgs e)
	{
		byte[] receivedData = (byte[])e.dataContents;
		
		byte[] curIdentifierByte = new byte[256];
		byte[] curDataTypeByte = new byte[256];
		byte[] curDataByte = new byte[receivedData.Length - 256 - 3 - 256 - 3];
		
		Array.Copy(receivedData, 0, curIdentifierByte, 0, 256);
		Array.Copy(receivedData, 256 + 3, curDataTypeByte, 0, 256);
		Array.Copy(receivedData, 256 + 3 + 256 + 3, curDataByte, 0, curDataByte.Length);
		
		string curIdentifier = System.Text.Encoding.Unicode.GetString(curIdentifierByte);
		curIdentifier = curIdentifier.TrimEnd(deleteChars);
		string curDataType = System.Text.Encoding.Unicode.GetString(curDataTypeByte);
		curDataType = curDataType.TrimEnd(deleteChars);
		
		switch (curIdentifier)
		{
		case "ctrlMessage":
			string[] curCtrlData = (System.Text.Encoding.Unicode.GetString(curDataByte)).Split(new string[] { ">_<" }, StringSplitOptions.None);
			switch (curCtrlData[0])
			{
			case "reportPlace":
				ReportPlace();
				break;
			}
			break;
			
		default:
			OnReceiveFromEmitter(new ReceiveFromEmitterEventArgs(curIdentifier, curDataType, curDataByte));
			break;
		}
	}

	private void ReportPlace()
	{
		string curAppPath = System.Reflection.Assembly.GetEntryAssembly().Location;
		SendToEmitter("ctrlMessage", typeof(string).ToString(), System.Text.Encoding.Unicode.GetBytes("reportPlace>_<" + curAppPath));
	}
}
