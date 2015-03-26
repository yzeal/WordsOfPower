using UnityEngine;
using System.Collections;

/// <summary>
/// Simple class for displaying button and text on screen
/// </summary>
public class UI : MonoBehaviour
{
    private float mBtnWidth = 160;
    private float mBtnHeight = 30;

    private Rect mSwap;

    private GUIText mInstructions;

    /// <summary>
    /// Name of the scene we're currently in
    /// </summary>
    public string SceneName;

    /// <summary>
    /// Used for initialization
    /// </summary>
    void Awake()
    {
        mInstructions = GameObject.Find("TextInstructions").GetComponent<GUIText>();
        mInstructions.text += "\r\n";

        if (SceneName == "Demo")
        {
            mInstructions.text += "Move  - WASD or Left Stick" + "\r\n";
            mInstructions.text += "Walk  - WASD + Middle Mouse Button or Left Stick + Left Trigger" + "\r\n";
            mInstructions.text += "Turn  - Mouse + Right Mouse Button or Right Stick" + "\r\n";
        }
        else
        {
            mInstructions.text += "Move  - WAD or Left Stick" + "\r\n";
            mInstructions.text += "Target - WASD + Middle Mouse Button or Left Stick + Left Trigger" + "\r\n";
            mInstructions.text += "View  - Mouse + Right Mouse Button or Right Stick" + "\r\n";
        }
        
        mInstructions.text += "Jump  - Space or 'A' Button" + "\r\n";
        mInstructions.text += "Drop  - Shift or 'Y' Button" + "\r\n";
        mInstructions.text += "Sneak - T or Right Trigger" + "\r\n";
        mInstructions.text += "" + "\r\n";
        mInstructions.text += "Motions:" + "\r\n";
        mInstructions.text += "  Idle" + "\r\n";
        mInstructions.text += "  Forward" + "\r\n";
        mInstructions.text += "  Walk" + "\r\n";
        mInstructions.text += "  Sneak" + "\r\n";
        mInstructions.text += "  Slide" + "\r\n";
        mInstructions.text += "  Jump" + "\r\n";
        mInstructions.text += "  Fall" + "\r\n";
        mInstructions.text += "  Climb High" + "\r\n";
        mInstructions.text += "  Climb Mid" + "\r\n";

        mSwap = new Rect(10, 100 + ((mBtnHeight + 10) * 0), mBtnWidth, mBtnHeight);
    }

    /// <summary>
    /// Place the buttons and send messages when clicked
    /// </summary>
    void OnGUI()
    {
        string lButtonText = "Load Regular Camera";
        if (SceneName == "Demo") { lButtonText = "Load Adventure Camera"; }

        // Show the buttons
        if (GUI.Button(mSwap, lButtonText))
        {
            if (SceneName == "Demo")
            {
                Application.LoadLevel("AdvDemo");
            }
            else
            {
                Application.LoadLevel("Demo");
            }
        }
    }
}
