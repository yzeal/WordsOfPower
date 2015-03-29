using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PianoKeys : MonoBehaviour {

	private WordsOfPower wop;

	public AudioSource[] pianoKeys;
	public string[] keyboardKeys;

	private WoPGUI wopGUI;

	private Dictionary<string, AudioSource> keyboardToPiano = new Dictionary<string, AudioSource>();


	// Use this for initialization
	void Start () {
		wop = WordsOfPower.Instance;
		wopGUI = GameObject.Find("WoPGUI").GetComponent<WoPGUI>();

		for(int i = 0; i < keyboardKeys.Length; i++){
			keyboardToPiano.Add(keyboardKeys[i], pianoKeys[i]);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(wop.typing && wopGUI.currentState == WoPGUIStates.HUD){

			foreach(string keyString in keyboardKeys){
				try{
					if(Input.GetKeyDown(keyString)){
						if(!keyboardToPiano[keyString].isPlaying){
							keyboardToPiano[keyString].Play();
						}
					}
					if(Input.GetKeyUp(keyString)){
						keyboardToPiano[keyString].Stop();
					}
				}catch(System.Exception e){
					//do nothing
				}
			}


			if(Input.anyKeyDown){
				string pressedKey = Input.inputString;
				if(pressedKey.Contains("!")){
					if(!keyboardToPiano["!"].isPlaying){
						keyboardToPiano["!"].Play();
					}
				}else{
					keyboardToPiano["!"].Stop();
				}
				if(pressedKey.Contains("?")){
					if(!keyboardToPiano["?"].isPlaying){
						keyboardToPiano["?"].Play();
					}
				}else{
					keyboardToPiano["?"].Stop();
				}
				if(pressedKey.Contains("ä") || pressedKey.Contains("Ä")){
					if(!keyboardToPiano["ä"].isPlaying){
						keyboardToPiano["ä"].Play();
					}
				}else{
					keyboardToPiano["ä"].Stop();
				}
				if(pressedKey.Contains("ö") || pressedKey.Contains("Ö")){
					if(!keyboardToPiano["ö"].isPlaying){
						keyboardToPiano["ö"].Play();
					}
				}else{
					keyboardToPiano["ö"].Stop();
				}
				if(pressedKey.Contains("ü") || pressedKey.Contains("Ü")){
					if(!keyboardToPiano["ü"].isPlaying){
						keyboardToPiano["ü"].Play();
					}
				}else{
					keyboardToPiano["ü"].Stop();
				}
			}

			if(!Input.anyKey){
				keyboardToPiano["ä"].Stop();
				keyboardToPiano["ö"].Stop();
				keyboardToPiano["ü"].Stop();
				keyboardToPiano["!"].Stop();
				keyboardToPiano["?"].Stop();
			}

			//			if(Input.anyKey){
			//				string pressedKey = Input.inputString;
			//				if(Input.GetKeyDown(".")){
			//					keyboardToPiano["."].loop = true;
			//					keyboardToPiano["."].Play();
			//				}
			//				if(Input.GetKeyUp(".")){
			//					keyboardToPiano["."].Stop();
			//				}
			//				try{
			//					AudioSource pianoKey = keyboardToPiano[pressedKey];
			//					if(pianoKey != null){
			//						pianoKey.Play();
			//					}

			
			//				}catch(System.Exception e){
			//					//do nothing
			//				}
			//			}
		}
	}
}
