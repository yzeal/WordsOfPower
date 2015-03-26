using System;
using UnityEngine;

/// <summary>
/// Simple logic for a platform that moves back and forth
/// </summary>
public class ElevatorMover : MonoBehaviour
{
    private Vector3 mStartPosition = new Vector3();
    private Vector3 mEndPosition = new Vector3();
    private Vector3 mVelocity = new Vector3(0, 1, 0); 

    /// <summary>
    /// Called right before the first frame update
    /// </summary>
    void Start()
    {
        mStartPosition = transform.position;

        mEndPosition = mStartPosition;
        mEndPosition.y += 5;
    }

    /// <summary>
    /// Called once per frame to update objects. This happens after FixedUpdate().
    /// Reactions to calculations should be handled here.
    /// </summary>
    void Update()
    {
        if (transform.position.y > mEndPosition.y)
        {
            mVelocity.y = -1;
        }
        else if (transform.position.y < mStartPosition.y)
        {
            mVelocity.y = 1;
        }

        transform.position += mVelocity * Time.deltaTime;
    }
}
