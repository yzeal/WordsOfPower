using System;
using UnityEngine;

/// <summary>
/// Simple logic for a platform that moves back and forth
/// </summary>
public class PlatformMover : MonoBehaviour
{
    private Vector3 mStartPosition = new Vector3();
    private Vector3 mEndPosition = new Vector3();
    private Vector3 mVelocity = new Vector3(1, 0, 0);

    public bool UseFixedUpdate = true;

    public bool Rotate = false;

    public bool Move = false;

    public Vector3 EndPosition = new Vector3(10, 0, 0);

    /// <summary>
    /// Called right before the first frame update
    /// </summary>
    void Start()
    {
        mStartPosition = transform.position;
        mEndPosition = mStartPosition + EndPosition;
        mVelocity = (mEndPosition - mStartPosition) / 4f;
    }

    /// <summary>
    /// Called once per frame to update physics objects.
    /// </summary>
    void FixedUpdate()
    {
        if (!UseFixedUpdate) { return; }
        InternalUpdate();
    }

    /// <summary>
    /// Called once per frame as the heart-beat
    /// </summary>
    void Update()
    {
        if (UseFixedUpdate) { return; }
        InternalUpdate();
    }

    /// <summary>
    /// Moves and rotates the objects
    /// </summary>
    void InternalUpdate()
    {
        if (Move)
        {
            // Determine the destination and the distance
            float lDistance = Vector3.Distance(transform.position, mEndPosition);
            if (lDistance < 0.1f)
            {
                Vector3 lTemp = mEndPosition;
                mEndPosition = mStartPosition;
                mStartPosition = lTemp;

                mVelocity = (mEndPosition - mStartPosition) / 4f;
            }

            // Move the object
            if (GetComponent<Rigidbody>() != null)
            {
                if (GetComponent<Rigidbody>().isKinematic)
                {
                    GetComponent<Rigidbody>().MovePosition(transform.position + (mVelocity * Time.deltaTime));
                }
                else
                {
                    GetComponent<Rigidbody>().MovePosition(transform.position + (mVelocity * Time.deltaTime));
                }
            }
            else
            {
                transform.position += mVelocity * Time.deltaTime;
            }
        }

        // Rotate the object
        if (Rotate)
        {
            if (GetComponent<Rigidbody>() != null)
            {
                if (GetComponent<Rigidbody>().isKinematic)
                {
                    GetComponent<Rigidbody>().MoveRotation(transform.rotation * Quaternion.AngleAxis(45f * Time.deltaTime, Vector3.up));
                }
                else
                {
                    GetComponent<Rigidbody>().MoveRotation(transform.rotation * Quaternion.AngleAxis(45f * Time.deltaTime, Vector3.up));
                    //rigidbody.AddTorque(0f, 2.0f, 0f, ForceMode.Force);
                }
            }
            else
            {
                transform.Rotate(0f, 45f * Time.deltaTime, 0f);
            }
        }
    }
}
