﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Turner : MonoBehaviour {

//	public GameObject[] objectsToTurn;
	public float orbitDegrees = 3f;

	private bool turning;

	public bool Turning {
		get {
			return turning;
		}
	}

	private float degrees;
	private GameObject player;
	private GameObject rotationPivot;
	private Vector3 playerStartPos = Vector3.zero;
	private List<Transform> objectsToTurn = new List<Transform>();
	private List<GameObject> enemiesToTurn = new List<GameObject>();
	private GameObject level;

	// Use this for initialization
	void Start () {
		player = GameObject.FindWithTag("Player");
		rotationPivot = GameObject.Find("RotationPivot");

		level = GameObject.Find("Level");
		int levelChildren = level.transform.childCount;
		for(int i = 0; i < levelChildren; i++){
			objectsToTurn.Add(level.transform.GetChild(i).transform);
		}

		GameObject enemies = GameObject.Find("Enemies");
		int enemyChildren = enemies.transform.childCount;
		for(int i = 0; i < enemyChildren; i++){
			enemiesToTurn.Add(enemies.transform.GetChild(i).gameObject);
		}

	}
	
	// Update is called once per frame
	void Update () {
		if(turning){
//			foreach(Transform obj in objectsToTurn){
//				obj.RotateAround(rotationPivot.transform.position, rotationPivot.transform.forward, orbitDegrees);
//			}

			foreach(GameObject enemy in enemiesToTurn){
				if(enemy != null){
					enemy.GetComponent<Rigidbody>().isKinematic = true;
					enemy.GetComponent<Collider>().enabled = false;
				}
			}

			level.transform.RotateAround(rotationPivot.transform.position, rotationPivot.transform.forward, orbitDegrees);

			player.transform.position = playerStartPos;
			degrees += orbitDegrees;
			if(degrees == 180){
				turning = false;
				degrees = 0;
				foreach(GameObject enemy in enemiesToTurn){
					if(enemy != null){
	//					enemy.useGravity = true;
						enemy.GetComponent<Rigidbody>().isKinematic = false;
						enemy.GetComponent<Collider>().enabled = true;
					}
				}
			}
		}

		//TESTI
//		if(!turning && Input.GetKeyUp("t")){
//			Turn ();
//		}
	}

	public void Turn(){
		playerStartPos = player.transform.position;
		turning = true;

		foreach(GameObject enemy in enemiesToTurn){
			if(enemy != null){
	//			enemy.useGravity = false;
				enemy.GetComponent<Rigidbody>().isKinematic = true;
				enemy.GetComponent<Collider>().enabled = false;
			}
		}
	}
}