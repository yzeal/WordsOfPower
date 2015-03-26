using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.ootii.AI.Controllers;

public class Floater : MonoBehaviour {
	
	public float minTimeToCast = 3f;

	public int timesBeforeShortcut = 10;
	// Use this for initialization
	void Start () {

		float wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().spellTime;
		Debug.Log(wop);
		
		
		if(wop < minTimeToCast){			
			int spell = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().lastSpell;
			PhraseOfPower phop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>().words[spell];
			
			phop.times++;
			if(phop.times >= timesBeforeShortcut){
				phop.ActivateShortPhrase();
			}
		}

		GameObject.FindWithTag("Player").transform.position += Vector3.up;
		GameObject.FindWithTag("Player").GetComponent<MotionController>().Gravity = Vector3.zero;

		GameObject.FindWithTag("Player").GetComponent<Player>().flying = true;
	}

}
