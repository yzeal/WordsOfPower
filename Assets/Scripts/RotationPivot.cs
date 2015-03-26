using UnityEngine;
using System.Collections;

public class RotationPivot : MonoBehaviour {

	private GameObject player;

	// Use this for initialization
	void Start () {
		player = GameObject.FindWithTag("Player");
	}
	
	// Update is called once per frame
	void Update () {
		transform.position = player.transform.position + Vector3.up*0.722f;
	}
}
