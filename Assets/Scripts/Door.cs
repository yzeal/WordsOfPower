using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour {

	public Vector3 direction = Vector3.down;
	public float openingSpeed = 1f;

	public Switch[] switches;

	public bool open;
	public float openingDistance = 5f;

	private float t;
	private Vector3 startPos;
	private Vector3 endPos;
	private bool moving;



	private Turner turner;

	// Use this for initialization
	void Start () {
		turner = WordsOfPower.Instance.GetComponent<Turner>();
		startPos = transform.position;
		endPos = transform.position + direction * openingDistance; //TODO .y stimmt nicht für jede Richtung!!
//		if(openingDistance != 0f){
//			endPos = transform.position + direction * openingDistance;
//		}
	}
	
	// Update is called once per frame
	void Update () {
		if(switches.Length > 0){
			open = true;
		}
		foreach(Switch doorSwitch in switches){
			if(!doorSwitch.activated){
				open = false;
			}
		}

		if(turner.doorTurning){
			startPos = transform.position;
			endPos = transform.position + direction * openingDistance; //TODO .y stimmt nicht für jede Richtung!!
//			if(openingDistance != 0f){
//				endPos = transform.position + direction * openingDistance;
//			}
		}

		if(!turner.doorTurning){
			if(open && !moving && transform.position != endPos){
				Debug.Log("Door opening: " + startPos + "  " + endPos);
				moving = true;
				t = 0;
			}else if(!open && !moving && transform.position != startPos){
				Debug.Log("Door closing: " + startPos + "  " + transform.position);
				moving = true;
				t = 0;
			}


			if(open){
				//runter
				if(moving){
					transform.position = Vector3.Lerp(startPos, endPos, t*openingSpeed);
					t += Time.deltaTime;
					if(t >= 1){
						moving = false;
						transform.position = endPos;
					}
				}
				
			}else if(transform.position != startPos){
				//hoch
				if(moving){
					transform.position = Vector3.Lerp(endPos, startPos, t*openingSpeed);
					t += Time.deltaTime;
					if(t >= 1){
						moving = false;
						transform.position = startPos;
					}
				}
			}
		}
	}
}
