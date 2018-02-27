using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InterpretTreadmillData : MonoBehaviour {

	public string serverHost;
	public int serverPort;
    public bool useKeyboard;

	public TreadmillCommunicator Communicator {
		get {
			return _comm;
		}
	}

    private TreadmillCommunicator _comm;
	private Server _serv;
    private Queue<Message> _messages = new Queue<Message>();
	private Queue<Message> _messages_dis = new Queue<Message>();
	private Subscription subscription;
	private Subscription subscription_dis;
    private Scene _scene;

    public GameObject coordinatorObject;
	public GameObject UI;
    private HashSet<GameObject> _rootObjectsFixedToLab;
    //private PGCoordinator _coordinator;
	private GameObject path;

    private double currSpeed = 0;
	private double distance = 0;
    //public GameObject cube;

	void Start () {
		_scene = SceneManager.GetActiveScene();
		/*
		_comm = new TreadmillCommunicator(PlayerPrefs.GetString("treadmill-host"), PlayerPrefs.GetInt("treadmill-port"),
			serverHost, serverPort);
			*/
		_comm = new TreadmillCommunicator (serverHost, serverPort, serverHost, serverPort);
		string[] stream = { "Speed" }; 
		string[] stream_dis = { "Distance" };
		subscription = _comm.Subscribe (stream);
		subscription_dis = _comm.Subscribe (stream_dis);
		_serv = new Server (serverHost, serverPort, _comm);

        //_coordinator = coordinatorObject.GetComponent<PGCoordinator>();
		path = coordinatorObject;
		_rootObjectsFixedToLab = new HashSet<GameObject>();

		/*
		GameObject[] gameObjs = _scene.GetRootGameObjects() as GameObject[];
		for (int i = 0; i < gameObjs.Length; i++) {
			if (gameObjs [i].GetComponent<FixedToLab> () != null) {
				_rootObjectsFixedToLab.Add (gameObjs [i]);
			}
		}
		*/

	}

  	void Update() {
		mehUpdate ();
		
        Vector3 offset = new Vector3 (0f, 0f, (float) (-currSpeed * Time.deltaTime));
        if(useKeyboard)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                offset += new Vector3(0f, 0f, -20*Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                offset += new Vector3(0f, 0f, 20*Time.deltaTime);
            }
        }

		/*
        GameObject[] gameObjs = _scene.GetRootGameObjects() as GameObject[];
        for (int i = 0; i < gameObjs.Length; i++)
        {
            if (gameObjs[i].GetComponent<FixedToLab>())
                continue;
            gameObjs[i].transform.Translate(offset, Space.World);
        }
		*/

		//_coordinator.Shift(offset);
		path.GetComponent<CPC_CameraPath>().velocity = (float) currSpeed;
		path.GetComponent<CPC_CameraPath>().distance = (float) distance;
		UI.GetComponent<Canvas_UI>().distance = (float)distance;
		UI.GetComponent<Canvas_UI>().speed = (float)currSpeed;
		//Debug.Log ("currspeed:" + currSpeed);
  }

	void mehUpdate () {
        // Fetch messages
		_comm.FetchMessages(subscription, _messages);

		_comm.FetchMessages(subscription_dis, _messages_dis);

        // Now process them.
        // In this example, we just print them one by one (and remove them from the inbox)
        while(_messages.Count > 0)
        {
			Message msg = _messages.Dequeue();
			currSpeed = msg.value;
			// Debug.Log("TREADMILL: " + msg.ToString());
        }

		while (_messages_dis.Count > 0) {
			Message msg = _messages_dis.Dequeue ();
			distance = msg.value;
		}
	}

    void OnDestroy()
    {
        // Don't forget to call Dispose() on the TreadmillCommunicator when we're finished
        _comm.Dispose();
		_serv.Dispose ();
    }
}
