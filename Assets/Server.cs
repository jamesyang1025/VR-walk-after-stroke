using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class Server : IDisposable {

	private IPAddress _host;
	private int _port;
	private Queue<Message> _messages = new Queue<Message>();
	private Thread _workingThread;
	private TreadmillCommunicator _comm;
	private Subscription _sub;
	private volatile bool _stillWorking;

	public IPAddress Host
	{
		get
		{
			return _host;
		}
	}
	public int Port
	{
		get
		{
			return _port;
		}
	}

	public Server (string host, int port, TreadmillCommunicator comm)
	{
		_host = IPAddress.Parse(host);
		_port = port;
		_comm = comm;
		string[] streams = { "COPx", "COPy" };
		_sub = _comm.Subscribe (streams);
		_workingThread = new Thread (new ThreadStart (Work));
		_workingThread.IsBackground = true;
		_stillWorking = true;
		_workingThread.Start ();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
		{
			if(_workingThread != null)
			{
				_stillWorking = false;
				// _workingThread.Interrupt();
				//_workingThread.Join ();
				_workingThread = null;
			}
		}
	}

	private void Work() {
		bool madeConnection = false;
		Debug.Log("Setting up server at " + _host.ToString() + ":" + _port.ToString());
		TcpListener server = new TcpListener(_host, _port);
		server.Start();
		TcpClient client = null;
		while (_stillWorking) {
			if (madeConnection) {
				client.Close ();
				madeConnection = false;
			}
			try {
				Debug.Log("Waiting for connection...");
				while(_stillWorking) {
					var sock1 = new ArrayList{server.Server}; 
					Socket.Select(sock1, null, null, 1000);
					if(!sock1.Contains(server.Server))
						continue;
					client = server.AcceptTcpClient();
					Debug.Log("Connected to client!");
					madeConnection = true;
					NetworkStream stream = client.GetStream();

					while(_stillWorking){
						_comm.WaitForMessages(_sub);
						_comm.FetchMessages(_sub, _messages);
						while(_messages.Count > 0) {
							string msg = JsonUtility.ToJson(_messages.Dequeue());
							stream.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
						}
					}
				}
			}
			catch(SocketException e)
			{
				Debug.LogError(e);
			}
			catch (IOException e)
			{
				Debug.LogError(e);
			}
			catch (ObjectDisposedException e)
			{
				Debug.LogError(e);
			}
			// catch (ThreadInterruptedException e) 
			// {
			// 	Debug.LogError (e);
			// 	if(madeConnection)
			// 		client.Close ();
			// 	server.Stop();
			// 	return;
			// }
		}
		if(madeConnection) 
			client.Close ();
		server.Stop();
		Debug.Log ("Exiting server thread");
	}
}
