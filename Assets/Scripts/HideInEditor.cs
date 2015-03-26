using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class HideInEditor : MonoBehaviour {

	void OnEnable(){
		if(Application.isEditor && !Application.isPlaying){
			GetComponent<MeshRenderer>().enabled = false;
		}else{
			GetComponent<MeshRenderer>().enabled = true;
		}
	}


//	void Update(){
//		if (Selection.Contains (gameObject)){
//			GetComponent<MeshRenderer>().enabled = true;
//		}else{
//			GetComponent<MeshRenderer>().enabled = false;
//		}
//	}
}
