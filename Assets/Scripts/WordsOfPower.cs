using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using com.ootii.AI.Controllers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class WordsOfPower : MonoBehaviour {
	
	public List<PhraseOfPower> words = new List<PhraseOfPower>();
	
	private float maxTime = 2.0f;
	private float t = 0f;
	public float spellTime = 0f;
	public int lastSpell = 0;
	private int z = 0;
	
	private Turner turner;
	private WoPGUI wopGUI;
	private AudioSource backgroundMusic;
	
	public bool typing = false;
	
	public string normalMode = "Press RETURN to enter Typing Mode :D";
	public string typingMode = "Typing Mode!! Press RETURN to exit. Or try typing the word \"turn\"";
	private Text modeText;
	private Text typedText;
	private Text castingTimeText;
	private Image typingBackground;
	private Image typingBackgroundWords;
	private Image castingTimeBackground;
	
	private MotionController playerController;
	private GameObject player;
	
	private string testString = "";

	private bool wait;

	public static WordsOfPower Instance { get; private set; }
	
	void Awake(){
		
		if(Instance != null && Instance != this)
		{
			Destroy(gameObject);
		}
		
		Instance = this;
		
		DontDestroyOnLoad(gameObject);
	}

	private void LoadStuff(){
		{
			turner = GetComponent<Turner>();
			backgroundMusic = GameObject.FindWithTag("MainCamera").GetComponent<AudioSource>();
			
			typingBackground = GameObject.Find("TypingBackground").GetComponent<Image>();
			typingBackgroundWords = GameObject.Find("TypingBackgroundWords").GetComponent<Image>();
			castingTimeBackground = GameObject.Find("CastingTimeBackground").GetComponent<Image>();
			typingBackground.enabled = false;
			typingBackgroundWords.enabled = false;
			castingTimeBackground.enabled = false;
			modeText = GameObject.Find("ModeText").GetComponent<Text>();
			wopGUI = GameObject.Find("WoPGUI").GetComponent<WoPGUI>();
			typedText = GameObject.Find("TypedText").GetComponent<Text>();
			castingTimeText = GameObject.Find("CastingTimeText").GetComponent<Text>();
			modeText.text = normalMode;
			
			player = GameObject.FindWithTag("Player");
			playerController = player.GetComponent<MotionController>();
			
			wait = false;

		}
	}

	// Use this for initialization
	void Start () {
		LoadStuff();

		words.Add(new PhraseOfPower("Turn around!", "turn", "OneTimeTurner"));
		words.Add(new PhraseOfPower("land", "land", "Lander"));
		words.Add(new PhraseOfPower("float", "float", "Floater"));
		words.Add(new PhraseOfPower("Ice!!", "ice", "Ice"));
		words.Add(new PhraseOfPower("Fire!!", "fire", "Fire"));
		words.Add(new PhraseOfPower("Kablammo!!", "bomb", "Bomb"));
		words.Add(new PhraseOfPower("I summon you, Balfour, harbinger of darkness!", "balfour", "Balfour"));
		words.Add(new PhraseOfPower("bal", "bal", "Balfour"));
		//		words.Add(new WordsOfPower("I am the terror that flaps in the night, I am the scourge that pecks at your nightmares.", "protect", "Protection");
		words.Add(new PhraseOfPower("I am the terror that flaps in the night, I am the itch you cannot reach!", "protect", "Protection"));
		words.Add(new PhraseOfPower("pro", "pro", "Protection"));
		
		//TESTI
		//		for(int i = 0; i < 100000; i++){
		//			words.Add(new PhraseOfPower("I am the terror that flaps in the night, I am the itch you cannot reach!" + i, "protect", "Protection"));
		//		}
	}

	void OnLevelWasLoaded(){
		LoadStuff();
	}
	
	// Update is called once per frame
	void Update () {
		
		//toggle typing mode on or off
		if(Input.GetKeyDown(KeyCode.Return) && !wait && wopGUI.currentState == WoPGUIStates.HUD){
			typing = typing ? false : true;
			if(typing){
				modeText.text = typingMode;
				Wait(0.1f);
				typingBackground.enabled = true;
				typingBackgroundWords.enabled = true;
				castingTimeBackground.enabled = true;
				testString = "";
				typedText.text = "";
				backgroundMusic.volume = 0.25f;
//				playerController.enabled = false;
				
				//TESTI
				//				Time.timeScale = 0f;
			}else{
				modeText.text = normalMode;
				typingBackground.enabled = false;
				typingBackgroundWords.enabled = false;
				castingTimeBackground.enabled = false;
				testString = "";
				typedText.text = "";
				castingTimeText.text = "";
				backgroundMusic.volume = 0.5f;
//				playerController.enabled = true;
				
				//TESTI
				//				Time.timeScale = 1f;
			}
		}
		
		if(typing && !wait && wopGUI.currentState == WoPGUIStates.HUD){
			
			castingTimeText.text = "";
			
			if(testString.Length > 1){
				t += Time.deltaTime;
			}
			
			if(Input.GetKeyDown(KeyCode.Backspace)){
				testString = "";
				t = 0;
			}
			
			//			Debug.Log(t);

			castingTimeText.text = Mathf.RoundToInt(t) + "s";

			if(Input.anyKey){
				foreach (char c in Input.inputString) {						
					testString += c;
					//					Regex.Replace(testString, @"[^a-zA-Z0-9 ]", ""); //evtl nicht nötig
					
				}
//				Debug.Log (testString);
				typedText.text = testString;

				for(int i = 0; i < words.Count; i++){
					PhraseOfPower word = words[i];
						if(testString.Contains(word.phrase) || testString.Contains(word.shortPhrase)){
						spellTime = t;
						if(words[i].bestTime == -1f || spellTime < words[i].bestTime){
							words[i].bestTime = spellTime;
						}
						t = 0f;
						lastSpell = i;
						testString = "";
//						castingTimeText.text = word.phrase + "  " + Mathf.RoundToInt(spellTime) + "s";
						typedText.text = word.phrase;
						Invoke("DeleteCastingTimeText", 2f);
						typing = false;
						modeText.text = normalMode;
						typingBackground.enabled = false;
//						typingBackgroundWords.enabled = false;
//						typedText.text = "";
						//playerController.UseInput = true;
						playerController.enabled = true;
						
						Debug.Log(spellTime);
						Instantiate(Resources.Load(word.magic), player.transform.position, player.transform.rotation);

						//TESTI
						//						Time.timeScale = 1f;
					}
				}
			}
		}
		
	}
	
	private void DeleteCastingTimeText(){
		castingTimeText.text = "";
		typedText.text = "";
		typingBackgroundWords.enabled = false;
		castingTimeBackground.enabled = false;
	}

	public void Wait(float seconds){
		wait = true;
		Invoke("StopWaiting", seconds);
	}

	private void StopWaiting(){
		wait = false;
	}
}



//public class WordsOfPower : MonoBehaviour {
//
//	private List<string[]> words = new List<string[]>();
//
//	private float maxTime = 2.0f;
//	private float t = 0f;
//	public float spellTime = 0f;
//	private int z = 0;
//
//	private Turner turner;
//	private WoPGUI wopGUI;
//
//	public bool typing = false;
//
//	public string normalMode = "Press RETURN to enter Typing Mode :D";
//	public string typingMode = "Typing Mode!! Press RETURN to exit. Or try typing the word \"turn\"";
//	private Text modeText;
//	private Text typedText;
//	private Text castingTimeText;
//	private Image typingBackground;
//
//	private MotionController playerController;
//	private GameObject player;
//	
//	private string testString = "";
//
//
//
//	// Use this for initialization
//	void Start () {
//		turner = GetComponent<Turner>();
//
//		typingBackground = GameObject.Find("TypingBackground").GetComponent<Image>();
//		typingBackground.enabled = false;
//		modeText = GameObject.Find("ModeText").GetComponent<Text>();
//		wopGUI = GameObject.Find("WoPGUI").GetComponent<WoPGUI>();
//		typedText = GameObject.Find("TypedText").GetComponent<Text>();
//		castingTimeText = GameObject.Find("CastingTimeText").GetComponent<Text>();
//		modeText.text = normalMode;
//
//		player = GameObject.FindWithTag("Player");
//		playerController = player.GetComponent<MotionController>();
//
//		words.Add(new string[]{"turn", "OneTimeTurner"});
//		words.Add(new string[]{"land", "Lander"});
//		words.Add(new string[]{"float", "Floater"});
//		words.Add(new string[]{"land", "Lander"});
//		words.Add(new string[]{"Ice!!", "Ice"});
//		words.Add(new string[]{"Fire!!", "Fire"});
//		words.Add(new string[]{"Kablammo!!", "Bomb"});
//		words.Add(new string[]{"I summon you, Balfour, harbinger of darkness!", "Balfour"});
//		words.Add(new string[]{"bal", "Balfour"});
////		words.Add(new string[]{"I am the terror that flaps in the night, I am the scourge that pecks at your nightmares.", "Protection"});
//		words.Add(new string[]{"I am the terror that flaps in the night, I am the itch you cannot reach!", "Protection"});
//		words.Add(new string[]{"pro", "Protection"});
//
//	}
//	
//	// Update is called once per frame
//	void Update () {
//	
//		//toggle typing mode on or off
//		if(Input.GetKeyDown(KeyCode.Return) && wopGUI.currentState == WoPGUIStates.HUD){
//			typing = typing ? false : true;
//			if(typing){
//				modeText.text = typingMode;
//				typingBackground.enabled = true;
//				testString = "";
//				typedText.text = "";
////				playerController.UseInput = false;
//				playerController.enabled = false;
//
//				//TESTI
////				Time.timeScale = 0f;
//			}else{
//				modeText.text = normalMode;
//				typingBackground.enabled = false;
//				testString = "";
//				typedText.text = "";
////				playerController.UseInput = true;
//				playerController.enabled = true;
//
//				//TESTI
////				Time.timeScale = 1f;
//			}
//		}
//
//		if(typing && wopGUI.currentState == WoPGUIStates.HUD){
//
//			DeleteCastingTimeText();
//
//			if(testString.Length > 1){
//				t += Time.deltaTime;
//			}
//
//			if(Input.GetKeyDown(KeyCode.Backspace)){
//				testString = "";
//				t = 0;
//			}
//
////			Debug.Log(t);
//
//			if(Input.anyKey){
//				foreach (char c in Input.inputString) {						
//					testString += c;
////					Regex.Replace(testString, @"[^a-zA-Z0-9 ]", ""); //evtl nicht nötig
//						
//				}
//				Debug.Log (testString);
//				typedText.text = testString;
//				for(int i = 0; i < words.Count; i++){
//					string[] word = words[i];
//					if(testString.Contains(word[0])){
//						spellTime = t;
//						t = 0f;
//						testString = "";
//						castingTimeText.text = word[0] + "  " + Mathf.RoundToInt(spellTime) + "s";
//						Invoke("DeleteCastingTimeText", 2f);
//						typing = false;
//						modeText.text = normalMode;
//						typingBackground.enabled = false;
//						typedText.text = "";
//						//playerController.UseInput = true;
//						playerController.enabled = true;
//
//						Debug.Log(spellTime);
//						Instantiate(Resources.Load(word[1]), player.transform.position, player.transform.rotation);
//
//						//TESTI
////						Time.timeScale = 1f;
//					}
//				}
//			}
//		}
//
//	}
//
//	private void DeleteCastingTimeText(){
//		castingTimeText.text = "";
//	}
//}
