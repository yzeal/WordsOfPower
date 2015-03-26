using UnityEngine;
using System.Collections;

public class Switch : MonoBehaviour {

	public string[] magic;
	public string[] antiMagic;
	public bool activated;

	public Color inactiveColor;
	public Color activeColor;

	private MeshRenderer renderer;

	// Use this for initialization
	void Start () {
		renderer = GetComponent<MeshRenderer>();
		renderer.material.color = inactiveColor;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider other){
		for(int i = 0; i < magic.Length; i++){
			if(other.name.Contains(magic[i])){
				activated = true;
				renderer.material.color = activeColor;
			}
		}

		for(int i = 0; i < antiMagic.Length; i++){
			if(other.name.Contains(antiMagic[i])){
				activated = false;
				renderer.material.color = inactiveColor;
			}
		}
	}
}
