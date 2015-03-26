using System;
using UnityEngine;

/// <summary>
/// Simple logic for a platform that moves back and forth
/// </summary>
public class PlatformMover4 : MonoBehaviour
{
    private Vector3 mStartPosition = new Vector3();
    private Vector3 mEndPosition = new Vector3();
    private Vector3 mVelocity = new Vector3(1, 0, 0); 

    /// <summary>
    /// Called right before the first frame update
    /// </summary>
    void Start()
    {
        mStartPosition = transform.position;

        mEndPosition = mStartPosition;
        mEndPosition.x -= 5;
    }

    /// <summary>
    /// Called once per frame to update objects. This happens after FixedUpdate().
    /// Reactions to calculations should be handled here.
    /// </summary>
    void Update()
    {
        if (transform.position.x < mEndPosition.x)
        {
            mVelocity.x = 1;
        }
        else if (transform.position.x > mStartPosition.x)
        {
            mVelocity.x = -1;
        }

        if (GetComponent<Rigidbody>() != null)
        {
            GetComponent<Rigidbody>().MovePosition(transform.position + (mVelocity * Time.deltaTime));
        }
        else
        {
            transform.position += mVelocity * Time.deltaTime;
        }
    }
}
