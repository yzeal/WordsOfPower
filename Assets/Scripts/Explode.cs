using UnityEngine;
using System.Collections;

public class Explode : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void Kablammo(){
		//TODO: Partikeleffekt instanziieren etc.
		Destroy(gameObject);
	}

	void OnCollisionEnter(Collision other){
		if(other.collider.name.Contains("Bomb")){
			Kablammo();
		}

	}
}
