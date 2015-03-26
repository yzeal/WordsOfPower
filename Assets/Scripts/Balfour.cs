using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.ootii.Cameras;

public class Balfour : MonoBehaviour {

	public float force = 500f;
	public float duration = 4f;

	public float maxTimeToCast = 20f;
	public float minTimeToCast = 12f;

	public int timesBeforeShortcut = 10;

	private AdventureRig camRig;


	// Use this for initialization
	void Start () {

		float wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().spellTime;
		Debug.Log(wop);
		
		
		if(wop < minTimeToCast){			
			int spell = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().lastSpell;
			PhraseOfPower phop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().words[spell];
			
			phop.times++;
			if(phop.times >= timesBeforeShortcut){
				phop.ActivateShortPhrase();
			}
		}

		camRig = GameObject.Find("AdvCameraRig").GetComponent<AdventureRig>();

		if(camRig.pubCamMode == EnumCameraMode.FIRST_PERSON){
			GameObject camObj = GameObject.FindWithTag("MainCamera");
			transform.position = camObj.transform.position;
			transform.rotation = camObj.transform.rotation;

			GetComponent<Rigidbody>().AddForce(camObj.transform.forward * force);
		}else{
			transform.position += Vector3.up * 1f + GameObject.FindWithTag("Player").transform.forward;
			GetComponent<Rigidbody>().AddForce(GameObject.FindWithTag("Player").transform.forward * force);
		}

		Invoke("Disappear", duration);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	private void Disappear(){
		Destroy(gameObject);
	}
}
