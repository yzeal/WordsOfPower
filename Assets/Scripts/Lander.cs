using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.ootii.AI.Controllers;

public class Lander : MonoBehaviour {

	public float minTimeToCast = 3f;

	public int timesBeforeShortcut = 10;

	// Use this for initialization
	void Start () {

		float wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().spellTime;
		Debug.Log(wop);
		
		
		if(wop < minTimeToCast){
			int spell = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().lastSpell;
			Debug.Log("last spell: " + spell);
			try{
				PhraseOfPower phop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().words[spell];

				phop.times++;
				if(phop.times >= timesBeforeShortcut){
					phop.ActivateShortPhrase();
				}
			}catch(System.Exception e){
				//do nothing
			}
		}

		GameObject.FindWithTag("Player").GetComponent<MotionController>().Gravity = new Vector3(0f, -11.81f, 0f);;

		GameObject.FindWithTag("Player").GetComponent<Player>().flying = false;
	}

}
