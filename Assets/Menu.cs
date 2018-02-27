using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour {
    public InputField inputHost;
    public InputField inputPort;

    public void StartCalibration()
    {
        SceneManager.LoadSceneAsync("calibration");
    }
    public void StartGame()
    {
        SceneManager.LoadSceneAsync("main");
    }
	public void StartLevelSelect() {
		SceneManager.LoadSceneAsync("levelselection");
	}
    public void StoreHost()
    {
        PlayerPrefs.SetString("treadmill-host", inputHost.text);
    }
    public void StorePort()
    {
		//TODO: handle invalid integers
		PlayerPrefs.SetInt("treadmill-port", int.Parse(inputPort.text));
    }

    void Start()
    {
        inputHost.text = PlayerPrefs.GetString("treadmill-host");
		inputPort.text = PlayerPrefs.GetInt("treadmill-port").ToString();
    }

	public void QuitGame() {
		Application.Quit ();
	}
}
