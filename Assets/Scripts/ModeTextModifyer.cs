using UnityEngine;
using System.Collections;

public class ModeTextModifyer : MonoBehaviour {

	public string normalMode;
	public string typingMode;

	private WordsOfPower wop;

	// Use this for initialization
	void Start () {
		wop = GameObject.Find("WordsOfPower").GetComponent<WordsOfPower>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
