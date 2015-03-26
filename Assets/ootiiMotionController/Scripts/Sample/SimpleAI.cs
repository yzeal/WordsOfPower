using UnityEngine;
using System.Collections;
using com.ootii.AI.Controllers;

/// <summary>
/// Simple test to show controllering a character with the 
/// Motion Controller through script
/// </summary>
public class SimpleAI : MonoBehaviour
{
    /// <summary>
    /// Reference to the motion controller for the avatar
    /// </summary>
    private MotionController mController = null;

    /// <summary>
    /// Called right before the first frame update
    /// </summary>
    void Start()
    {
        mController = GetComponent<MotionController>();
    }

    /// <summary>
    /// Called once per frame to update objects. This happens after FixedUpdate().
    /// Reactions to calculations should be handled here.
    /// </summary>
    void Update()
    {
        if (mController == null) { return; }
        if (mController.GetAnimatorStateName(0) == "") { return; }

        if (transform.position.x >= -1)
        {
            mController.SetTargetPosition(new Vector3(-35, 0, 48f), 1f);
        }
        else if (transform.position.x <= -29)
        {
            mController.SetTargetPosition(new Vector3(5, 0, 48f), 1f);
        }
        else if (transform.position.x < -15 && transform.position.x > -16)
        {
            MotionControllerMotion lJump = mController.GetMotion(0, typeof(Jump));
            if (lJump != null) { mController.ActivateMotion(lJump); }
        }
    }
}
