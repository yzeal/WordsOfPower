using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

//	public static Player Instance { get; private set; }

	public bool flying;

//	void Awake(){
//		
//		if(Player.Instance != null && Player.Instance != this)
////		if(Player.Instance != null)
//		{
//			Destroy(gameObject);
//		}
//		
//		Instance = this;
//		
//		DontDestroyOnLoad(gameObject);
//
//	}


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnLevelWasLoaded(){
		flying = false;
	}
}
