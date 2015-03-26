using UnityEngine;
using System.Collections;

public class Schade : MonoBehaviour {

	private Turner turner;

	void Start(){
		turner = GameObject.Find("WordsOfPower").GetComponent<Turner>();
	}

	void OnTriggerEnter(Collider other){
		if(other.CompareTag("Player")){
			if(!turner.Turning){
				Debug.Log("Schade.");
				Application.LoadLevel(Application.loadedLevel);
			}
		}else if(other.gameObject.layer == 8){
			if(!turner.Turning){
				Destroy(other.gameObject);
			}
		}
	}
}
