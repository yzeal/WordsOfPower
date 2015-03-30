using UnityEngine;
using System.Collections;

public class EnemyDeath : MonoBehaviour {

	private Rigidbody[] parts;

	// Use this for initialization
	void Start () {
		parts = GetComponentsInChildren<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//TODO: richtiges Kampfsystem; public Schadenvariable bei Zauber?
//	void OnCollisionEnter(Collision other){
//		if(other.collider.gameObject.layer == 9){
//			Debug.Log("Enemy hit.");
//			foreach(Rigidbody part in parts){
//				part.isKinematic = false;
//			}
//		}
//	}
	void OnTriggerEnter(Collider other){
		if(other.gameObject.layer == 9){
			Debug.Log("Enemy hit.");
			foreach(Rigidbody part in parts){
				part.isKinematic = false;
			}
		}
	}
}
