using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {

	private WordsOfPower wop;
//	private NavMeshAgent agent;
	private GameObject player;

	// Use this for initialization
	void Start () {
		player = GameObject.FindWithTag("Player");
		wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>();
//		agent = GetComponent<NavMeshAgent>();
//		agent.SetDestination(player.transform.position);
	}
	
	// Update is called once per frame
	void Update () {
		if(wop.typing){
			GetComponent<Rigidbody>().isKinematic = true;
		}else{
			GetComponent<Rigidbody>().isKinematic = false;
		}

		//immer
		if(!wop.typing){

//			agent.SetDestination(player.transform.position);

		}else{
//			agent.SetDestination(transform.position);
		}
	}

	void OnTriggerEnter(Collider other){
		if(other.CompareTag("Player")){
			Application.LoadLevel(Application.loadedLevel);
		}
	}
}
