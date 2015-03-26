using UnityEngine;
using System.Collections;

public class PhraseOfPower{

	public string phrase;
	private string shortCut;
	public string shortPhrase;
	public string magic;
	public int times;
	public float bestTime;

	public PhraseOfPower(){
		this.phrase = "";
		this.shortCut = "";
		this.shortPhrase = "XAXAXAXA";
		this.magic = "";
		this.times = 0;
		this.bestTime = -1f;
	}

	public PhraseOfPower(string phrase, string shortcut, string magic){
		this.phrase = phrase;
		this.shortCut = shortcut;
		this.shortPhrase = "XAXAXAXA";
		this.magic = magic;
		this.times = 0;
		this.bestTime = -1f;
	}

	public void ActivateShortPhrase(){
		this.shortPhrase = this.shortCut;
	}

}