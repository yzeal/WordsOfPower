using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Protection : MonoBehaviour {

	public Vector3 offset;
	public float maxDuration = 30f;
	public float minDuration = 10f;
	public float maxForce = 100f;
	public float minForce = 0f;
	public float maxTimeToCast = 20f;
	public float minTimeToCast = 10f;

	public int timesBeforeShortcut = 10;

	private GameObject player;
	private float wop;

	private float force;

	// Use this for initialization
	void Start () {
		player = GameObject.FindWithTag("Player");
		wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().spellTime;
		transform.position = player.transform.position + offset;
		transform.parent = player.transform;

		float duration = minDuration;

		if(wop < minTimeToCast){
			duration = maxDuration;

			int spell = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().lastSpell;
			PhraseOfPower phop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().words[spell];
			
			phop.times++;
			if(phop.times >= timesBeforeShortcut){
				phop.ActivateShortPhrase();
			}
		}else if(wop > maxTimeToCast){
			duration = minDuration;
		}else{
			duration = Mathf.Lerp(minDuration, maxDuration, wop/(maxTimeToCast - minTimeToCast));
		}

		if(wop < minForce){
			force = maxForce;
		}else if(wop > maxTimeToCast){
			force = minForce;
		}else{
			force = Mathf.Lerp(minForce, maxForce, wop/(maxTimeToCast - minTimeToCast));
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
		Debug.Log ("Enemy collision! " + force);
//		other.gameObject.rigidbody.AddForce(-Vector3.Normalize(other.gameObject.transform.position - player.transform.position)*force);
		if(other.collider.gameObject.layer == 8){
			other.collider.GetComponent<Rigidbody>().AddExplosionForce(force, player.transform.position, 100f);
		}	
	}
}
