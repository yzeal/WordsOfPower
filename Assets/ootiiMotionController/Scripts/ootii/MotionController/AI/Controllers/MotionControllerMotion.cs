using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Utilities;

namespace com.ootii.AI.Controllers
{
    /// <summary>
    /// Controller motions represent motions and activities that an
    /// avatar are performing. The idea being that a controller can
    /// have a collection of motions, choose between the one that's appropriate,
    /// and then the motions will handle the details of animating, moving,
    /// and rotating.
    /// </summary>
    public class MotionControllerMotion : BaseObject
    {
        /// <summary>
        /// Tracks the qualified type of the motion
        /// </summary>
        [SerializeField]
        protected string mType = "";
        public string Type
        {
            get { return mType; }
            set { mType = value; }
        }

        /// <summary>
        /// Name of the motion
        /// </summary>
        [SerializeField]
        protected string mName = "";
        public override string Name
        {
            get { return mName; }
            
            set 
            {
                _Name = value;
                mName = value; 
            }
        }

        /// <summary>
        /// Determines if the motion is enabled. If it is
        /// running and then disabled, the motion will finish
        /// </summary>
        [SerializeField]
        protected bool mIsEnabled = true;
        public bool IsEnabled
        {
            get { return mIsEnabled; }
            set { mIsEnabled = value; }
        }

        /// <summary>
        /// Once deactivated, a delay before we start activating again
        /// </summary>
        [SerializeField]
        protected float mReactivationDelay = 0f;

        [MotionTooltip("Once deactivated, adds a delay before the motion can be activated again.")]
        public float ReactivationDelay
        {
            get { return mReactivationDelay; }
            set { mReactivationDelay = value; }
        }

        /// <summary>
        /// Controller this motion is tied to
        /// </summary>
        protected MotionController mController;
        public MotionController Controller
        {
            get { return mController; }
            set { mController = value; }
        }

        /// <summary>
        /// Layer the motion is tied to
        /// </summary>
        protected MotionControllerLayer mMotionLayer;
        public MotionControllerLayer MotionLayer
        {
            get { return mMotionLayer; }
            set 
            { 
                mMotionLayer = value;
                mAnimatorLayerIndex = (mMotionLayer == null ? 0 : mMotionLayer.AnimatorLayerIndex);
            }
        }

        /// <summary>
        /// The phase or state the motion is in. This differs for each
        /// motion and is a way to track the state internally.
        /// </summary>
        protected int mPhase = 0;
        public int Phase
        {
            get { return mPhase; }
            set { mPhase = value; }
        }

        /// <summary>
        /// Flags the motion for activation on the next update.
        /// We stick to activating in the update phase so all states stay valid.
        /// </summary>
        protected bool mQueueActivation = false;
        public bool QueueActivation
        {
            get { return mQueueActivation; }
            set { mQueueActivation = value; }
        }

        /// <summary>
        /// Determines if the motion is capable of being started.
        /// </summary>
        protected bool mIsStartable = false;
        public bool IsStartable
        {
            get { return mIsStartable; }
        }

        /// <summary>
        /// Determines if the motion is currently active
        /// </summary>
        protected bool mIsActive = false;
        public bool IsActive
        {
            get { return mIsActive; }
        }

        /// <summary>
        /// Determines if this is the frame the motion was activated in
        /// </summary>
        protected bool mIsActivatedFrame = false;
        public bool IsActivatedFrame
        {
            get { return mIsActivatedFrame; }
            set { mIsActivatedFrame = value; }
        }

        /// <summary>
        /// Determines if this motion can be interrupted by another motion.
        /// When interrupted, this motion will need to handle it and may
        /// shut down.
        /// </summary>
        protected bool mIsInterruptible = true;
        public bool IsInterruptible
        {
            get { return mIsInterruptible; }
            set { mIsInterruptible = value; }
        }

        /// <summary>
        /// Used to help speed up ground checking. Some motions
        /// simply don't expect to be grounded
        /// </summary>
        protected bool mIsGroundedExpected = false;
        public bool IsGroundedExpected
        {
            get { return mIsGroundedExpected; }
            set { mIsGroundedExpected = value; }
        }

        /// <summary>
        /// Some motions could put the avatar on a new ground layer for
        /// the purposes of the NavMesh. We'll use this flag to determine
        /// if we need to reset the NavMeshAgent when the motion completes
        /// </summary>
        protected bool mIsNavMeshChangeExpected = false;
        public bool IsNavMeshChangeExpected
        {
            get { return mIsNavMeshChangeExpected; }
            set { mIsNavMeshChangeExpected = value; }
        }

        /// <summary>
        /// Determines how important this motion is to other
        /// motions. The higher the priority, the higher the
        /// importance.
        /// </summary>
        [SerializeField]
        protected float _Priority = 0;
        public float Priority
        {
            get { return _Priority; }
            set { _Priority = value; }
        }

        /// <summary>
        /// Current velocity caused by the motion. This should be
        /// multiplied by delta-time to create displacement
        /// </summary>
        protected Vector3 mVelocity = Vector3.zero;
        public Vector3 Velocity
        {
            get { return mVelocity; }
        }

        /// <summary>
        /// Amount of rotation caused by the motion. This should be
        /// multiplied by delta-time to create angular displacement
        /// </summary>
        protected Vector3 mAngularVelocity = Vector3.zero;
        public Vector3 AngularVelocity
        {
            get { return mAngularVelocity; }
        }

        /// <summary>
        /// Determines if we'll apply gravity to the avatar
        /// </summary>
        protected bool mIsGravityEnabled = true;
        public bool IsGravityEnabled
        {
            get { return mIsGravityEnabled; }
            set { mIsGravityEnabled = value; }
        }

        /// <summary>
        /// Determines if we use trend data to delay sending speed
        /// to the animator
        /// </summary>
        protected bool mUseTrendData = false;
        public bool UseTrendData
        {
            get { return mUseTrendData; }
            set { mUseTrendData = value; }
        }

        /// <summary>
        /// Adjusts the camera anchor position based on the
        /// root motion or animation
        /// </summary>
        public virtual Vector3 RootMotionCameraOffset
        {
            get
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Determines the index of the layer in the mechanim animator
        /// where the corresponding animations lies. This is for convience as
        /// all motions should have the same index as their Motion Layer
        /// </summary>
        protected int mAnimatorLayerIndex = 0;
        public int AnimatorLayerIndex
        {
            get { return mAnimatorLayerIndex; }
        }

        /// <summary>
        /// Tracks the last time the motion was deactivate
        /// </summary>
        protected float mDeactivationTime = 0f;
        public float DeactivationTime
        {
            get { return mDeactivationTime; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MotionControllerMotion()
            : base()
        {
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public MotionControllerMotion(MotionController rController)
            : base()
        {
            mController = rController;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public virtual void LoadAnimatorData()
        {
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns>Boolean that determines if the motion should start</returns>
        public virtual bool TestActivate()
        {
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public virtual bool TestUpdate()
        {
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public virtual bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Flag the motion as active
            mIsActive = true;
            mIsActivatedFrame = true;
            mIsStartable = false;

            // Report that we're good enter the motion
            return true;
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public virtual void Deactivate()
        {
            mIsActive = false;
            mIsStartable = true;
            mDeactivationTime = Time.time;
            mVelocity = Vector3.zero;
            mAngularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Allows the motion to modify the ground and support information
        /// </summary>
        /// <param name="rState">Current state whose support info can be modified</param>
        /// <returns>Boolean that determines if the avatar is grounded</returns>
        public virtual bool DetermineGrounding(ref ControllerState rState)
        {
            return rState.IsGrounded;
        }

        /// <summary>
        /// Allows the motion to modify the velocity and rotation before it is applied.
        /// </summary>
        public virtual void CleanRootMotion(ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        public virtual void UpdateMotion()
        {
            // Default is to simply deactivate the motion
            Deactivate();
        }

        /// <summary>
        /// Updates the motion over time (during the LateUpdate function). 
        /// This is called by the controller every update cycle so animations and stages can be updated.
        /// </summary>
        public virtual void LateUpdateMotion()
        {
        }

        /// <summary>
        /// Allows the motion to process IK animations
        /// </summary>
        public virtual void UpdateIK()
        {
        }

        /// <summary>
        /// Raised when the animator's state has changed
        /// </summary>
        public virtual void OnAnimatorStateChange(int rLastStateID, int rNewStateID)
        {
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public virtual bool OnInterruption(MotionControllerMotion rMotion)
        {
            return true;
        }

        /// <summary>
        /// Allow the motion to render debug info
        /// </summary>
        public virtual void OnDrawGizmos()
        {
        }

        /// <summary>
        /// Used internally to calculate the velocity of the motion. Root motion
        /// is handled by the controller directly and won't come through this function.
        /// </summary>
        /// <returns>Vector3 representing the current velocity</returns>
        protected virtual Vector3 DetermineVelocity()
        {
            return Vector3.zero;
        }

        /// <summary>
        /// Returns the current angular velocity of the motion.  Root motion
        /// is handled by the controller directly and won't come through this function.
        /// </summary>
        protected virtual Vector3 DetermineAngularVelocity()
        {
            return Vector3.zero;
        }

        /// <summary>
        /// Creates a JSON string that represents the motion's serialized state. We
        /// do this since Unity can't handle putting lists of derived objects into
        /// prefabs.
        /// </summary>
        /// <returns>JSON string representing the object</returns>
        public virtual string SerializeMotion()
        {
            if (mType.Length == 0) { mType = this.GetType().AssemblyQualifiedName; }

            StringBuilder lStringBuilder = new StringBuilder();
            lStringBuilder.Append("{");

            // These four properties are important from the base MotionControllerMotion
            lStringBuilder.Append(", \"Type\" : \"" + mType + "\"");
            lStringBuilder.Append(", \"Name\" : \"" + mName + "\"");
            lStringBuilder.Append(", \"Priority\" : \"" + _Priority.ToString() + "\"");
            lStringBuilder.Append(", \"IsEnabled\" : \"" + mIsEnabled.ToString() + "\"");
            lStringBuilder.Append(", \"ReactivationDelay\" : \"" + mReactivationDelay.ToString() + "\"");

            // Cycle through all the properties. 
            // Unfortunately Binding flags don't seem to be working. So,
            // we need to ensure we don't include base properties
            PropertyInfo[] lBaseProperties = typeof(MotionControllerMotion).GetProperties();
            PropertyInfo[] lProperties = this.GetType().GetProperties();
            foreach (PropertyInfo lProperty in lProperties)
            {
                if (!lProperty.CanWrite) { continue; }

                bool lAdd = true;
                for (int i = 0; i < lBaseProperties.Length; i++) 
                { 
                    if (lProperty.Name == lBaseProperties[i].Name) 
                    {
                        lAdd = false;
                        break;
                    } 
                }

                if (!lAdd || lProperty.GetValue(this, null) == null) { continue; }

                object lValue = lProperty.GetValue(this, null);
                if (lProperty.PropertyType == typeof(Vector2))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + ((Vector2)lValue).ToString("G8") + "\"");
                }
                else if (lProperty.PropertyType == typeof(Vector3))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + ((Vector3)lValue).ToString("G8") + "\"");
                }
                else if (lProperty.PropertyType == typeof(Vector4))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + ((Vector4)lValue).ToString("G8") + "\"");
                }
                else
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + lValue.ToString() + "\"");
                }
            }

            lStringBuilder.Append("}");

            return lStringBuilder.ToString();
        }

        /// <summary>
        /// Gieven a JSON string that is the definition of the object, we parse
        /// out the properties and set them.
        /// </summary>
        /// <param name="rDefinition">JSON string</param>
        public virtual void DeserializeMotion(string rDefinition)
        {
            JSONNode lDefinitionNode = JSONNode.Parse(rDefinition);
            if (lDefinitionNode == null) { return; }

            // Cycle through the properties and load the values we can
            PropertyInfo[] lProperties = this.GetType().GetProperties();
            foreach (PropertyInfo lProperty in lProperties)
            {
                if (!lProperty.CanWrite) { continue; }
                if (lProperty.GetValue(this, null) == null) { continue; }

                JSONNode lValueNode = lDefinitionNode[lProperty.Name];
                if (lValueNode == null) { continue; }

                if (lProperty.PropertyType == typeof(string))
                {
                    lProperty.SetValue(this, lValueNode.Value, null);
                }
                else if (lProperty.PropertyType == typeof(int))
                {
                    lProperty.SetValue(this, lValueNode.AsInt, null);
                }
                else if (lProperty.PropertyType == typeof(float))
                {
                    lProperty.SetValue(this, lValueNode.AsFloat, null);
                }
                else if (lProperty.PropertyType == typeof(bool))
                {
                    lProperty.SetValue(this, lValueNode.AsBool, null);
                }
                else if (lProperty.PropertyType == typeof(Vector2))
                {
                    Vector2 lVector2Value = Vector2.zero;
                    lVector2Value = lVector2Value.FromString(lValueNode.Value);

                    lProperty.SetValue(this, lVector2Value, null);
                }
                else if (lProperty.PropertyType == typeof(Vector3))
                {
                    Vector3 lVector3Value = Vector3.zero;
                    lVector3Value = lVector3Value.FromString(lValueNode.Value);

                    lProperty.SetValue(this, lVector3Value, null);
                }
                else if (lProperty.PropertyType == typeof(Vector4))
                {
                    Vector4 lVector4Value = Vector4.zero;
                    lVector4Value = lVector4Value.FromString(lValueNode.Value);

                    lProperty.SetValue(this, lVector4Value, null);
                }
                else
                {
                    //JSONClass lObject = lValueNode.AsObject;
                }
            }
        }
    }
}
