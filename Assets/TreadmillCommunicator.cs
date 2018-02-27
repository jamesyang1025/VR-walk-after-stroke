using System;
//using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/* How to use this class:
 * - create a new TreadmillCommunicator, supplying the correct hostname and port
 * - use FetchMessages to fetch new messages
 * - when you're done with the TreadmillCommunicator, call its Dispose() method
 */

public class TreadmillCommunicator : IDisposable {

	private string _host;
	private int _port;
	private List<Queue<Message>> _queues;
	private Thread _workingThread;
	private List<Subscription> _subscriptions;
	private volatile bool _stillWorking;

	private bool _connected;
	private object _connectedLock = new object();
	public bool Connected {
		get {
			lock (_connectedLock) {
				return _connected;
			}
		}
	}

	public string Host
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

	public Subscription Subscribe(string[] streams) {
		Subscription sub = null;
		Queue<Message> queue = null;
		lock (_subscriptions) {
			sub = new Subscription(_subscriptions.Count, streams);
			lock (_subscriptions) {
				_subscriptions.Add (sub);
			}
			queue = new Queue<Message> ();
			lock (_queues) {
				_queues.Add (queue);
			}
		}
		return sub;
	}

	public TreadmillCommunicator(string host, int port, string serverHost, int serverPort)
	{
		_subscriptions = new List<Subscription> ();
		_queues = new List<Queue<Message>> ();

		_host = host;
		//get treadmill IP address
		//_host = "128.174.14.138";


		_port = port;
		//get treadmill IP port
		//_port = 8089;

		_workingThread = new Thread(new ThreadStart(Work));
		_workingThread.IsBackground = true;
		_stillWorking = true;
		_workingThread.Start();
	}

	// Adds all messages that have arrived since the last call to FetchMessages
	// (or all messages ever, if this is the first call to FetchMessages)
	// to inbox.
	public void FetchMessages(Subscription subscription, Queue<Message> inbox)
	{
		Queue<Message> currQueue = null;
		lock (_queues) {
			currQueue = _queues [subscription.Index];
		}
		lock (currQueue) {
			while (currQueue.Count > 0) {
				inbox.Enqueue (currQueue.Dequeue ());
			}
		}
	}

	public void WaitForMessages(Subscription sub) {
		Queue<Message> queue = null;
		lock (_queues) {
			queue = _queues [sub.Index];
		}

		lock (queue) {
			if (queue.Count > 0) {
				return;
			} else {
				Monitor.Wait (queue);
			}
		}
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
				//_workingThread.Interrupt();
				//_workingThread.Join ();
				_workingThread = null;
			}
		}
	}

	private void Work()
	{
		lock (_connectedLock) {
			_connected = false;
		}
		bool madeConnection = false;
		TcpClient client = null;
		while(_stillWorking)
		{
			try
			{
				Thread.Sleep (1000);
				if(madeConnection) {
					client.Close();
					madeConnection = false;
				}
				Debug.Log("Connecting to treadmill at " + _host + ":" + _port.ToString());
				client = new TcpClient(_host, _port);
				lock(_connectedLock) {
					_connected = true;
				}
				Debug.Log("Connected to treadmill!");
				madeConnection = true;
				NetworkStream stream = client.GetStream();
				stream.ReadTimeout = 1000; // milliseconds
				while(_stillWorking)
				{
					string msg = stream.ReadUntil('}');
					Message parsedMsg = JsonUtility.FromJson<Message>(msg);
					int queueLength = 0;
					lock(_queues) {
						queueLength = _queues.Count;
					}
					for(int i = 0; i < queueLength; i++) {
						string[] currStreams = null;
						lock(_subscriptions) {
							currStreams = _subscriptions[i].Streams;
						}
						Queue<Message> currQueue = null;
						lock (_queues) {
							currQueue = _queues[i];
						}
						bool newMessage = false;
						for(int j = 0; j < currStreams.Length; j++) {
							if(parsedMsg.stream.ToString().Equals(currStreams[j])) {
								lock(currQueue) {
									currQueue.Enqueue(parsedMsg);
								}
								newMessage = true;
							}
						}
						if(newMessage) {
							lock (currQueue) {
								Monitor.Pulse(currQueue);
							}
						}
					}
				}
			}
			catch(SocketException e)
			{
				lock(_connectedLock) {
					_connected = false;
				}
				Debug.LogError(e);
			}
			catch (IOException e)
			{
				lock(_connectedLock) {
					_connected = false;
				}
				Debug.LogError(e);
			}
			catch (ObjectDisposedException e)
			{
				lock(_connectedLock) {
					_connected = false;
				}
				Debug.LogError(e);
			}
			//catch (ThreadInterruptedException e) 
			//{
			//	Debug.Log ("Entered interrupt exception");
			//	Debug.LogError (e);
			//	if(madeConnection) 
			//		client.Close ();
			//	Debug.Log ("Exiting treadmill thread");
			//	return;
			//}
		}
		if (madeConnection) {
			client.Close ();
			lock(_connectedLock) {
				_connected = false;
			}
		}
		Debug.Log ("Exiting treadmill thread");
	}
}

public static class ExtensionsToNetworkStream
{
	//TODO: maybe implement a maximum size of message that is read into memory before giving up
	//      on the message
	//      (so that a stream that never sends delimiter doesn't get to eat up all the memory)
	public static string ReadUntil(this NetworkStream stream, char delimiter)
	{
		List<char> chars = new List<char>();
		while (true)
		{
			char c = (char)stream.ReadByte();
			chars.Add(c);
			if (c == delimiter)
			{
				return new string(chars.ToArray());
			}
		}
	}
}

public class Subscription {
	private int index;
	private string[] streams;

	internal Subscription(int i, string[] streams) {
		index = i;
		this.streams = (string[]) streams.Clone();
	}

	public int Index {
		get {
			return index;
		}
	}

	public string[] Streams{
		get{ 
			return streams;
		}
	}
}