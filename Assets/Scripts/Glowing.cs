using UnityEngine;
using System.Collections;

public class Glowing : MonoBehaviour {

	public Color colorDark;
	public Color colorLight;

	public float time = 1f;

	private float t;

	private Color currentColor;

	private Renderer renderer;

	private bool up;

	// Use this for initialization
	void Start () {
		renderer = GetComponent<Renderer>();
		Debug.Log(renderer);
		currentColor = colorLight;
	}

	// Update is called once per frame
	void Update () {
		DynamicGI.SetEmissive(renderer, currentColor);

		if(up){
			t += Time.deltaTime*time;
			currentColor = Color.Lerp(colorDark, colorLight, t);
			if(t >= 1){
				t = 1;
				up = false;
			}
		}else{
			t -= Time.deltaTime*time;
			currentColor = Color.Lerp(colorDark, colorLight, t);
			if(t <= 0){
				t = 0;
				up = true;
			}
		}
	}
}
