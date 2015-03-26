﻿using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour {

	public Vector3 direction = Vector3.down;
	public float openingSpeed = 1f;

	public Switch[] switches;

	public bool open;

	private float t;
	private Vector3 startPos;
	private Vector3 endPos;
	private bool moving;
	// Use this for initialization
	void Start () {
		startPos = transform.position;
		endPos = transform.position + direction * transform.localScale.y; //TODO .y stimmt nicht für jede Richtung!!
	}
	
	// Update is called once per frame
	void Update () {
		open = true;
		foreach(Switch doorSwitch in switches){
			if(!doorSwitch.activated){
				open = false;
			}
		}

		if(open && !moving && transform.position != endPos){
			moving = true;
			t = 0;
		}else if(!open && !moving && transform.position != startPos){
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