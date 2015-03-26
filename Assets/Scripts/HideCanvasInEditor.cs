using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class HideCanvasInEditor : MonoBehaviour {

	public bool hide;

	void OnEnabled(){
		if(hide && Application.isEditor && !Application.isPlaying){
			GetComponent<Canvas>().enabled = false;
		}else{
			GetComponent<Canvas>().enabled = true;
		}
	}

	void Update(){
		if(Application.isEditor && !Application.isPlaying){
			if(hide){
				GetComponent<Canvas>().enabled = false;
			}else{
				GetComponent<Canvas>().enabled = true;
			}
		}
	}
}
