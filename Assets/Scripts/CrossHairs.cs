using UnityEngine;
using System.Collections;
using com.ootii.Cameras;
using UnityEngine.UI;

public class CrossHairs : MonoBehaviour {

	private Image crossHairs;
	private AdventureRig advCam;

	// Use this for initialization
	void Start () {
		crossHairs = GetComponent<Image>();
		crossHairs.enabled = false;
		advCam = GameObject.Find("AdvCameraRig").GetComponent<AdventureRig>();
	}
	
	// Update is called once per frame
	void Update () {
		if(advCam.pubCamMode == EnumCameraMode.FIRST_PERSON){
			crossHairs.enabled = true;
		}else{
			crossHairs.enabled = false;
		}
	}
}
