using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class OneTimeTurner : MonoBehaviour {

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

		GameObject.Find("WordsOfPower").GetComponent<Turner>().Turn();
		Destroy(gameObject);
	}

}
