using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Canvas_UI : MonoBehaviour {
	public Text dist_text;
	public Text time_text;
	public Text speed_text;
	private float time;
	private float temp;
	private float start_dis;
	public float distance;
	public float speed;
	// Use this for initialization
	void Start () {
		dist_text.text = "Distance Travelled: 0 m";
		time_text.text = "Time Elapsed: 0 secs";
		speed_text.text = "Current Speed: 0.0 m/s";
		time = 0.0f;
		temp = 0.0f;
		speed = 0f;
		start_dis = 0.0f;
	}
	
	// Update is called once per frame
	void Update () {
		if (start_dis == 0.0f) {
			start_dis = distance;
		}
		temp += Time.deltaTime;
		if (temp > 1f) {
			time += temp;
			Debug.Log (start_dis);
			time_text.text = "Time Elapsed: " + (time).ToString ("F0") + "secs";
			dist_text.text = "Distance Travelled: " + (distance-start_dis).ToString ("F0") + "m";
			speed_text.text = "Current Speed: " + speed.ToString("F2") + "m/s";
			temp = 0f;
		}
	}
}
