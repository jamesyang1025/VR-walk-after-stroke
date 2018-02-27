using System;

[Serializable]
public class Message
{
	public enum Commands { None, Authenticate, StartTreadmill, StopTreadmill, SetValue, ActualValue };
	public Commands command;

	public enum Streams { None, Speed, Pitch, Sway, Yaw, Distance, Roll, Surge, Fz, COPx, COPy };
	public Streams stream;

	public int streamID;

	public double value;
	public string stringValue;

	public override string ToString()
	{
		return "c: " + command.ToString() + " s: " + stream.ToString() + " sID: " + streamID + " value: " + value;
	}
}
