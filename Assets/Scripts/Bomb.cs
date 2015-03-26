using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.ootii.Cameras;

public class Bomb : MonoBehaviour {
	
	public float minForce = 300f;
	public float maxForce = 800f;
	
	private float force = 500f;
	public float duration = 4f;
	public float maxTimeToCast = 6f;
	public float minTimeToCast = 2f;

	public int timesBeforeShortcut = 10;
	
	private AdventureRig camRig;
	
	// Use this for initialization
	void Start () {
		
		float wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().spellTime;
		Debug.Log(wop);

		
		if(wop < minTimeToCast){
			force = maxForce;

			int spell = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().lastSpell;
			PhraseOfPower phop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().words[spell];

			phop.times++;
			if(phop.times >= timesBeforeShortcut){
				phop.ActivateShortPhrase();
			}
		}else if(wop > maxTimeToCast){
			force = minForce;
		}else{
			force = Mathf.Lerp(maxForce, minForce, wop/(maxTimeToCast - minTimeToCast));
		}
		
		duration *= Mathf.Min(maxTimeToCast/wop, duration);
		GetComponent<Rigidbody>().mass *= Mathf.Min(wop/maxTimeToCast, maxTimeToCast) > 0f ? Mathf.Min(wop/maxTimeToCast, maxTimeToCast) : 1f;
		
		//		transform.localScale *= Mathf.Min(maxTimeToCast/wop, transform.localScale.x);
		
		if(wop < minTimeToCast){
			GetComponent<Rigidbody>().useGravity = false;
		}else{
			GetComponent<Rigidbody>().useGravity = true;
		}
		
		camRig = GameObject.Find("AdvCameraRig").GetComponent<AdventureRig>();
		
		if(camRig.pubCamMode == EnumCameraMode.FIRST_PERSON){
			GameObject camObj = GameObject.FindWithTag("MainCamera");
			transform.position = camObj.transform.position;
			transform.rotation = camObj.transform.rotation;
			
			GetComponent<Rigidbody>().AddForce(camObj.transform.forward * force);
		}else{
			transform.position += Vector3.up * 1.5f + GameObject.FindWithTag("Player").transform.forward;
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

	void OnCollisionEnter(Collision other){
		if(other.collider.gameObject.layer == 10){
//			Invoke("Disappear", 0.1f);
			Disappear();
		}else if(other.collider.gameObject.layer == 0){
//			GetComponent<Rigidbody>().isKinematic = true;
			GetComponent<Rigidbody>().velocity = Vector3.zero;
			GetComponent<Rigidbody>().useGravity = true;
			GetComponentInChildren<ParticleSystem>().Stop();
		}
	}
	
}
