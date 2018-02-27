using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour {
    public GameObject player;

	// Use this for initialization
	void Start () {
        //player.transform.position = new Vector3(137, 1.2f, 496);
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 forward = new Vector3(transform.forward[0], 0, transform.forward[2]);
        Vector3 right = new Vector3(transform.right[0], 0, transform.right[2]);
        player.transform.position += forward * Input.GetAxis("Vertical") * 0.02f + right * Input.GetAxis("Horizontal") * 0.02f;

    }
}
