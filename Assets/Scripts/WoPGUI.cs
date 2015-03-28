using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum WoPGUIStates{
	HUD, MENU, LEXICON
}

public class WoPGUI : MonoBehaviour {

	public WoPGUIStates currentState = WoPGUIStates.HUD;

	private Canvas canvasHUD;
	private Canvas canvasMenu;
	private Canvas canvasLexicon;

	// Use this for initialization
	void Start () {
		canvasHUD = GameObject.Find("CanvasHUD").GetComponent<Canvas>();
		canvasMenu = GameObject.Find("CanvasMenu").GetComponent<Canvas>();
		canvasLexicon = GameObject.Find("CanvasLexicon").GetComponent<Canvas>();

		canvasHUD.enabled = true;
		TurnOnButtons(canvasHUD);
		canvasMenu.enabled = false;
		TurnOffButtons(canvasMenu);
		canvasLexicon.enabled = false;
		TurnOffButtons(canvasLexicon);
	}
	
	// Update is called once per frame
	void Update () {

		if(currentState == WoPGUIStates.HUD){
			if(Input.GetKeyDown(KeyCode.Escape)){
				currentState = WoPGUIStates.MENU;
				canvasHUD.enabled = false;
				TurnOffButtons(canvasHUD);
				canvasMenu.enabled = true;
				TurnOnButtons(canvasMenu);
				canvasLexicon.enabled = false;
				TurnOffButtons(canvasLexicon);
				Time.timeScale = 0f;
			}
		}else if(currentState == WoPGUIStates.MENU || currentState == WoPGUIStates.LEXICON){
			if(Input.GetKeyDown(KeyCode.Escape)){
				currentState = WoPGUIStates.HUD;
				WordsOfPower.Instance.Wait(0.1f);
				canvasHUD.enabled = true;
				TurnOnButtons(canvasHUD);
				canvasMenu.enabled = false;
				TurnOffButtons(canvasMenu);
				canvasLexicon.enabled = false;
				TurnOffButtons(canvasLexicon);
				Time.timeScale = 1f;
			}
		}

	}

	public void SwitchToLexicon(){
		currentState = WoPGUIStates.LEXICON;
		canvasHUD.enabled = false;
		TurnOffButtons(canvasHUD);
		canvasMenu.enabled = false;
		TurnOffButtons(canvasMenu);
		canvasLexicon.enabled = true;
		TurnOnButtons(canvasLexicon);
		Time.timeScale = 0f;
	}

	public void SwitchToMenu(){
		currentState = WoPGUIStates.MENU;
		canvasHUD.enabled = false;
		TurnOffButtons(canvasHUD);
		canvasMenu.enabled = true;
		TurnOnButtons(canvasMenu);
		canvasLexicon.enabled = false;
		TurnOffButtons(canvasLexicon);
		Time.timeScale = 0f;
	}

	public void SwitchToHUD(){
		currentState = WoPGUIStates.HUD;
		WordsOfPower.Instance.Wait(0.1f);
		canvasHUD.enabled = true;
		TurnOnButtons(canvasHUD);
		canvasMenu.enabled = false;
		TurnOffButtons(canvasMenu);
		canvasLexicon.enabled = false;
		TurnOffButtons(canvasLexicon);
		Time.timeScale = 1f;
	}

	private void TurnOffButtons(Canvas can){
		Button[] buttons = can.GetComponentsInChildren<Button>();

		for(int i = 0; i < buttons.Length; i++){
			buttons[i].interactable = false;
		}
	}

	private void TurnOnButtons(Canvas can){
		Button[] buttons = can.GetComponentsInChildren<Button>();
		
		for(int i = 0; i < buttons.Length; i++){
			buttons[i].interactable = true;
		}
		if(buttons.Length > 0){
			buttons[0].Select();
		}
	}

	public void QuitGame(){
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#elif UNITY_WEBPLAYER
		Application.OpenURL("http://juliawolf.net/Wortspiel/WordsWeb.html");
		#else
		Application.Quit();
		#endif
	}
}
