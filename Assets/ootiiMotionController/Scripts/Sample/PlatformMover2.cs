using System;
using UnityEngine;

/// <summary>
/// Simple logic for a platform that moves back and forth
/// </summary>
public class PlatformMover2 : MonoBehaviour
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
            if (GetComponent<Rigidbody>().isKinematic)
            {
                GetComponent<Rigidbody>().MovePosition(transform.position + (mVelocity * Time.deltaTime));
                GetComponent<Rigidbody>().MoveRotation(transform.rotation * Quaternion.AngleAxis(45f * Time.deltaTime, Vector3.up));
            }
            else
            {
                GetComponent<Rigidbody>().AddTorque(0f, 2.0f, 0f, ForceMode.Force);
            }
        }
        else
        {
            transform.position += mVelocity * Time.deltaTime;
            transform.rotation = transform.rotation * Quaternion.AngleAxis(45 * Time.deltaTime, Vector3.up);
        }
    }
}
