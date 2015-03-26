using UnityEngine;
using System.Collections;

public class ResetCamera : MonoBehaviour {

	private GameObject camRig;

	// Use this for initialization
	void Start () {
		camRig = GameObject.FindWithTag("MainCameraRig");
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetButtonDown("ResetCamera")){
			camRig.transform.position = transform.position - 2.4f * transform.forward + 1.7f * Vector3.up;
		}		
	}
}
