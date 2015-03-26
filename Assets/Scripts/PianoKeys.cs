﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PianoKeys : MonoBehaviour {

	private WordsOfPower wop;

	public AudioSource[] pianoKeys;
	public string[] keyboardKeys;

	private Dictionary<string, AudioSource> keyboardToPiano = new Dictionary<string, AudioSource>();


	// Use this for initialization
	void Start () {
		wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>();

		for(int i = 0; i < keyboardKeys.Length; i++){
			keyboardToPiano.Add(keyboardKeys[i], pianoKeys[i]);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(wop.typing){
			if(Input.anyKey){
				string pressedKey = Input.inputString;
				try{
					AudioSource pianoKey = keyboardToPiano[pressedKey];
					if(pianoKey != null){
						pianoKey.Play();
					}
				}catch(System.Exception e){
					//do nothing
				}
			}
		}
	}
}