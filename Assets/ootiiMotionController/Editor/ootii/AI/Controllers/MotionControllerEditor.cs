using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using com.ootii.AI.Controllers;
using com.ootii.Cameras;
using com.ootii.Utilities;

[CanEditMultipleObjects]
[CustomEditor(typeof(MotionController))]
public class MotionControllerEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // Images for various controls
    private Texture mBackground;
    private Texture mItemSelector;
    private Texture mItemBorder;

    // Row styles
    private GUIStyle mRowStyle;
    private GUIStyle mSelectedRowStyle;

    // The actual class we're stroing
    private MotionController mMotionController;
    private SerializedObject mMotionControllerSO;

    // Currently selected items
    private int mSelectedLayerIndex = 0;
    private int mSelectedMotionIndex = 0;

    // Drop down values
    private List<Type> mControllerMotionTypes = new List<Type>();
    private List<String> mControllerMotionNames = new List<string>();

    // Determines if we show the layers and motions
    private bool mShowLayers = false;

    // Help text
    private string mLayerHelp = "Add a layer to assign motions. Multiple layers allow multiple motions to run at the same time.";
    private string mMotionHelp = "Add a motion to the selected layer to control the character. Motions with a higher priority take precedence.";
    private string mPropertyHelp = "Select a motion to modify its properties.";

    // Unfortunately Binding flags don't seem to be working. So,
    // we need to ensure we don't include base properties
    private PropertyInfo[] mBaseProperties = null;

    /// <summary>
    /// Called when the script object is loaded
    /// </summary>
    void OnEnable()
    {
        // Grab the serialized objects
        mMotionController = (MotionController)target;
        mMotionControllerSO = new SerializedObject(target);

        // Load the textures
        mBackground = Resources.Load<Texture>("MotionController/mc_background");
        mItemSelector = Resources.Load<Texture>("MotionController/mc_dot");
        mItemBorder = Resources.Load<Texture>("MotionController/Border");

        // Grab the list of motion types
        mControllerMotionTypes.Clear();
        mControllerMotionNames.Clear();

        // Styles for selected rows
        mRowStyle = new GUIStyle();
        mRowStyle.border = new RectOffset(1, 1, 1, 1);
        mRowStyle.margin = new RectOffset(0, 0, 0, 0);
        mRowStyle.padding = new RectOffset(0, 0, 0, 0);

        mSelectedRowStyle = new GUIStyle();
        mSelectedRowStyle.normal.background = (Texture2D)mItemBorder;
        mSelectedRowStyle.border = new RectOffset(1, 1, 1, 1);
        mSelectedRowStyle.margin = new RectOffset(0, 0, 0, 0);
        mSelectedRowStyle.padding = new RectOffset(0, 0, 0, 0);

        // Dropdown values
        mControllerMotionTypes.Add(null);
        mControllerMotionNames.Add("Select Motion");

        // Generate the list of motions to display
        Assembly lAssembly = Assembly.GetAssembly(typeof(MotionController));
        foreach (Type lType in lAssembly.GetTypes())
        {
            if (typeof(MotionControllerMotion).IsAssignableFrom(lType))
            {
                if (lType != typeof(MotionControllerMotion))
                {
                    mControllerMotionTypes.Add(lType);
                    mControllerMotionNames.Add(lType.Name);
                }
            }
        }

        // List of general motion properties
        mBaseProperties = typeof(MotionControllerMotion).GetProperties();
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mMotionControllerSO.Update();

        // Update the motion controller layers so they can update with the definitions
        for (int i = 0; i < mMotionController.MotionLayers.Count; i++)
        {
            mMotionController.MotionLayers[i].Controller = mMotionController;
            mMotionController.MotionLayers[i].InstanciateMotions();
        }

        // Grab the position of the editor. Theis is a goofy hack, but works.
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        Rect lArea = GUILayoutUtility.GetLastRect();

        // Store the positions
        float lEditorY = lArea.y;
        float lEditorWidth = Screen.width - 20f;

        // We want the BG aligned: Top Center. We'll cut off any piece that is too arge
        Rect lBGCrop = new Rect(0, 0, mBackground.width, mBackground.height - 110);
        Vector2 lBGPosition = new Vector2(lEditorWidth - (mBackground.width * 0.8f), lEditorY + 5f);

        GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
        GUI.BeginGroup(new Rect(lBGPosition.x, lBGPosition.y, lBGCrop.width, lBGCrop.height));
        GUI.DrawTexture(new Rect(-lBGCrop.x, -lBGCrop.y, mBackground.width, mBackground.height), mBackground);
        GUI.EndGroup();
        GUILayout.EndArea();

        // Start putting in the properites
        GUILayout.Space(10);

        SerializedProperty lUseInputSP = mMotionControllerSO.FindProperty("_UseInput");
        bool lUseInput = EditorGUILayout.Toggle(new GUIContent("Use Input", "Determines if this avatar will respond to input from the user."), lUseInputSP.boolValue);

        SerializedProperty lRigTransformSP = mMotionControllerSO.FindProperty("_CameraTransform");
        Transform lRigTransform = EditorGUILayout.ObjectField(new GUIContent("Camera Transform", "Camera that this avatar will manage."), lRigTransformSP.objectReferenceValue, typeof(Transform), true) as Transform;

        //SerializedProperty lRigSP = mMotionControllerSO.FindProperty("_CameraRig");
        //CameraRig lRig = EditorGUILayout.ObjectField("Camera Rig", lRigSP.objectReferenceValue, typeof(CameraRig), true) as CameraRig;

        SerializedProperty lRigOffsetSP = mMotionControllerSO.FindProperty("_CameraRigOffset");
        Vector3 lRigOffset = EditorGUILayout.Vector3Field(new GUIContent("Camera Offset", "Offset from avatar's position the camera will look at. Typically the head."), lRigOffsetSP.vector3Value);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Camera VLerp", "Multiplication factor used to modify the camera's vertical speed."));

        if (Screen.width <= 332)
        {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
        }

        EditorGUILayout.LabelField("U", GUILayout.Width(12));
        SerializedProperty lRigLerpUpSP = mMotionControllerSO.FindProperty("_CameraRigUpLerp");
        float lRigLerpUp = EditorGUILayout.FloatField(lRigLerpUpSP.floatValue);

        EditorGUILayout.LabelField("D", GUILayout.Width(12));
        SerializedProperty lRigLerpDownSP = mMotionControllerSO.FindProperty("_CameraRigDownLerp");
        float lRigLerpDown = EditorGUILayout.FloatField(lRigLerpDownSP.floatValue);

        EditorGUILayout.EndHorizontal();

        SerializedProperty lGravitySP = mMotionControllerSO.FindProperty("_Gravity");
        Vector3 lGravity = EditorGUILayout.Vector3Field(new GUIContent("Gravity", "Gravity this avatar will use."), lGravitySP.vector3Value);

        SerializedProperty lForwardBumperSP = mMotionControllerSO.FindProperty("_ForwardBumper");
        Vector3 lForwardBumper = EditorGUILayout.Vector3Field(new GUIContent("Forward Bumper", "Projects a ray forward to test for something blocking. Set to 0 to disable."), lForwardBumperSP.vector3Value);

        SerializedProperty lForwardBumperBlendSP = mMotionControllerSO.FindProperty("_ForwardBumperBlendAngle");
        float lForwardBumperBlend = EditorGUILayout.FloatField(new GUIContent("Forward Bumper Blend Angle", "Angle to blend movement when blocked. Assume a head on collision is 0 angle."), lForwardBumperBlendSP.floatValue);

        SerializedProperty lMassSP = mMotionControllerSO.FindProperty("_Mass");
        float lMass = EditorGUILayout.FloatField(new GUIContent("Mass", "Mass of the avatar used for physics calculations like jumping"), lMassSP.floatValue);

        SerializedProperty lMinSlideAngleSP = mMotionControllerSO.FindProperty("_MinSlideAngle");
        float lMinSlideAngle = EditorGUILayout.FloatField(new GUIContent("Min Slide Angle", "Minimum angle the avatar will start sliding down."), lMinSlideAngleSP.floatValue);

        SerializedProperty lMaxSpeedSP = mMotionControllerSO.FindProperty("_MaxSpeed");
        float lMaxSpeed = EditorGUILayout.FloatField(new GUIContent("Max Speed", "Determines how quickly the avatar can move in meters per second."), lMaxSpeedSP.floatValue);

        SerializedProperty lRotationSpeedSP = mMotionControllerSO.FindProperty("_RotationSpeed");
        float lRotationSpeed = EditorGUILayout.FloatField(new GUIContent("Rotation Speed", "Determines how quickly the avatar can rotate in degrees per second."), lRotationSpeedSP.floatValue);

        // Show the Layers
        GUILayout.Space(10);

        EditorGUI.indentLevel++;
        mShowLayers = EditorGUILayout.Foldout(mShowLayers, new GUIContent("Motion Layers"));
        EditorGUI.indentLevel--;

        if (mShowLayers)
        {
            GUILayout.BeginVertical("Layers", GUI.skin.window, GUILayout.Height(100));

            RenderLayerList();

            GUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("+", "Add Layer"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
            {
                AddLayer();
            }

            if (GUILayout.Button(new GUIContent("-", "Delete Layer"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
            {
                mSelectedLayerIndex = RemoveLayer(mSelectedLayerIndex);
            }

            EditorGUILayout.EndHorizontal();

            // Show the layer motions
            GUILayout.Space(10);
            GUILayout.BeginVertical("Layer Motions", GUI.skin.window, GUILayout.Height(100));

            RenderLayerMotionList(mSelectedLayerIndex);

            GUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("+", "Add Motion"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
            {
                AddMotion(mSelectedLayerIndex);
            }

            if (GUILayout.Button(new GUIContent("-", "Delete Motion"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
            {
                mSelectedMotionIndex = RemoveMotion(mSelectedLayerIndex, mSelectedMotionIndex);
            }

            EditorGUILayout.EndHorizontal();

            // Show the layer motions
            GUILayout.Space(10);
            GUILayout.BeginVertical("Layer Motion Properties", GUI.skin.window, GUILayout.Height(100));

            RenderLayerMotionProperties(mSelectedLayerIndex, mSelectedMotionIndex);

            GUILayout.EndVertical();

            // Add some space at the bottom
            GUILayout.Space(10);
        }

        // If there is a change... update.
        if (GUI.changed)
        {
            bool lIsDirty = false;
            lIsDirty = (lIsDirty || (lUseInputSP.boolValue != lUseInput));
            lIsDirty = (lIsDirty || (lRigTransformSP.objectReferenceValue != lRigTransform));
            lIsDirty = (lIsDirty || (lRigOffsetSP.vector3Value != lRigOffset));
            lIsDirty = (lIsDirty || (lRigLerpUpSP.floatValue != lRigLerpUp));
            lIsDirty = (lIsDirty || (lRigLerpDownSP.floatValue != lRigLerpDown));
            lIsDirty = (lIsDirty || (lGravitySP.vector3Value != lGravity));
            lIsDirty = (lIsDirty || (lForwardBumperSP.vector3Value != lForwardBumper));
            lIsDirty = (lIsDirty || (lForwardBumperBlendSP.floatValue != lForwardBumperBlend));
            lIsDirty = (lIsDirty || (lMassSP.floatValue != lMass));
            lIsDirty = (lIsDirty || (lMinSlideAngleSP.floatValue != lMinSlideAngle));
            lIsDirty = (lIsDirty || (lMaxSpeedSP.floatValue != lMaxSpeed));
            lIsDirty = (lIsDirty || (lRotationSpeedSP.floatValue != lRotationSpeed));

            if (lIsDirty)
            {
                mIsDirty = true;

                lUseInputSP.boolValue = lUseInput;
                lRigTransformSP.objectReferenceValue = lRigTransform;
                lRigOffsetSP.vector3Value = lRigOffset;
                lRigLerpUpSP.floatValue = lRigLerpUp;
                lRigLerpDownSP.floatValue = lRigLerpDown;
                lGravitySP.vector3Value = lGravity;
                lForwardBumperSP.vector3Value = lForwardBumper;
                lForwardBumperBlendSP.floatValue = lForwardBumperBlend;
                lMassSP.floatValue = lMass;
                lMinSlideAngleSP.floatValue = lMinSlideAngle;
                lMaxSpeedSP.floatValue = lMaxSpeed;
                lRotationSpeedSP.floatValue = lRotationSpeed;
            }
        }

        // If there is a change... update.
        if (mIsDirty)
        {
            // Flag the object as needing to be saved
            EditorUtility.SetDirty(mMotionController);

            // Pushes the values back to the runtime so it has the changes
            mMotionControllerSO.ApplyModifiedProperties();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }

    /// <summary>
    /// Render the list of layers that exist
    /// </summary>
    /// <param name="rName">Property name containing the layer list</param>
    private void RenderLayerList()
    {
        // If we don't have items in the list, display some help
        if (mMotionController.MotionLayers.Count == 0)
        {
            EditorGUILayout.HelpBox(mLayerHelp, MessageType.Info, true);
            return;
        }

        // Add a row for titles
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(24);
        EditorGUILayout.LabelField("Name", GUILayout.MinWidth(100));
        EditorGUILayout.LabelField("A. Layer", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // Cycle through each layer
        for (int i = 0; i < mMotionController.MotionLayers.Count; i++)
        {
            MotionControllerLayer lMotionLayer = mMotionController.MotionLayers[i];

            GUIStyle lRowStyle = (mSelectedLayerIndex == i ? mSelectedRowStyle : mRowStyle);
            EditorGUILayout.BeginHorizontal(lRowStyle);

            // Select based on a click
            if (GUILayout.Button(new GUIContent(mItemSelector), GUI.skin.label, GUILayout.Width(16)))
            {
                mSelectedLayerIndex = i;
                mSelectedMotionIndex = 0;
            }

            string lName = EditorGUILayout.TextField(lMotionLayer.Name, GUILayout.MinWidth(100));
            int lIndex = EditorGUILayout.IntField(lMotionLayer.AnimatorLayerIndex, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();

            // If the name changed, select the record
            if (GUI.changed)
            {
                bool lIsDirty = false;
                lIsDirty = (lIsDirty || (lMotionLayer.Name != lName));
                lIsDirty = (lIsDirty || (lMotionLayer.AnimatorLayerIndex != lIndex));

                if (lIsDirty)
                {
                    mIsDirty = true;

                    mSelectedLayerIndex = i;
                    mSelectedMotionIndex = 0;

                    lMotionLayer.Name = lName;
                    lMotionLayer.AnimatorLayerIndex = lIndex;
                }
            }
        }
    }

    /// <summary>
    /// Renders the motions for the specified list's index
    /// </summary>
    /// <param name="rName"></param>
    /// <param name="rLayerIndex"></param>
    private void RenderLayerMotionList(int rLayerIndex)
    {
        int lMotionCount = 0;

        if (rLayerIndex >= 0)
        {
            if (mMotionController.MotionLayers.Count > rLayerIndex)
            {
                lMotionCount = mMotionController.MotionLayers[rLayerIndex].MotionDefinitions.Count;
            }
        }

        // If we don't have items in the list, display some help
        if (lMotionCount == 0)
        {
            EditorGUILayout.HelpBox(mMotionHelp, MessageType.Info, true);
            return;
        }

        // Add a row for titles
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(24);
        EditorGUILayout.LabelField("Type", GUILayout.MinWidth(50));
        EditorGUILayout.LabelField("Name", GUILayout.Width(60));
        EditorGUILayout.LabelField("Priority", GUILayout.Width(50));
        EditorGUILayout.LabelField("Act", GUILayout.Width(30));
        EditorGUILayout.EndHorizontal();

        // Cycle through the motions
        MotionControllerLayer lMotionLayer = mMotionController.MotionLayers[rLayerIndex];
        for (int i = 0; i < lMotionLayer.Motions.Count; i++)
        {
            MotionControllerMotion lMotion = lMotionLayer.Motions[i];

            GUIStyle lRowStyle = (mSelectedMotionIndex == i ? mSelectedRowStyle : mRowStyle);
            EditorGUILayout.BeginHorizontal(lRowStyle);

            if (GUILayout.Button(new GUIContent(mItemSelector), GUI.skin.label, GUILayout.Width(16)))
            {
                mSelectedMotionIndex = i;
            }


            Type lMotionType = lMotion.GetType();
            int lOriginalSelectedType = GetMotionIndex(lMotionType);
            int lSelectedType = EditorGUILayout.Popup(lOriginalSelectedType, mControllerMotionNames.ToArray(), GUILayout.MinWidth(50));

            string lMotionName = EditorGUILayout.TextField(lMotion.Name, GUILayout.Width(60));
            float lMotionPriority = EditorGUILayout.FloatField(lMotion.Priority, GUILayout.Width(60));
            bool lMotionIsEnabled = EditorGUILayout.Toggle(lMotion.IsEnabled, GUILayout.Width(20));

            EditorGUILayout.EndHorizontal();

            // If the name changed, select the record
            if (GUI.changed)
            {
                bool lIsDirty = false;
                lIsDirty = (lIsDirty || (lSelectedType != lOriginalSelectedType));
                lIsDirty = (lIsDirty || (lMotionName != lMotion.Name));
                lIsDirty = (lIsDirty || (lMotionPriority != lMotion.Priority));
                lIsDirty = (lIsDirty || (lMotionIsEnabled != lMotion.IsEnabled));

                if (lIsDirty)
                {
                    mIsDirty = true;

                    lMotion.Type = mControllerMotionTypes[lSelectedType].AssemblyQualifiedName;
                    lMotion.Name = lMotionName;
                    lMotion.Priority = lMotionPriority;
                    lMotion.IsEnabled = lMotionIsEnabled;

                    string lDefinition = lMotion.SerializeMotion();
                    lMotionLayer.MotionDefinitions[i] = lDefinition;
                }
            }
        }        
    }

    /// <summary>
    /// Renders the properties of the motion so they can be changed here
    /// </summary>
    /// <param name="rLayerIndex">Layer the motion belongs to</param>
    /// <param name="rMotionIndex">Motions whose properites are to be listed</param>
    private void RenderLayerMotionProperties(int rLayerIndex, int rMotionIndex)
    {
        bool lExit = false;
        if (!lExit && rLayerIndex < 0) { lExit = true; }
        if (!lExit && rMotionIndex < 0) { lExit = true; }
        if (!lExit && rLayerIndex >= mMotionController.MotionLayers.Count) { lExit = true; }
        if (!lExit && rMotionIndex >= mMotionController.MotionLayers[rLayerIndex].Motions.Count) { lExit = true; }

        // If we don't have items in the list, display some help
        if (lExit)
        {
            EditorGUILayout.HelpBox(mPropertyHelp, MessageType.Info, true);
            return;
        }

        // Tracks if we change the motion values
        bool lIsDirty = false;

        // Grab the layer
        MotionControllerLayer lLayer = mMotionController.MotionLayers[rLayerIndex];

        // Grab the motion
        MotionControllerMotion lMotion = lLayer.Motions[rMotionIndex];
        if (lMotion == null) { return; }

        object[] lMotionAttributes = lMotion.GetType().GetCustomAttributes(typeof(MotionTooltipAttribute), true);
        foreach (MotionTooltipAttribute lAttribute in lMotionAttributes)
        {
            EditorGUILayout.HelpBox(lAttribute.Tooltip, MessageType.None, true);
        }

        EditorGUILayout.LabelField(new GUIContent("Type", "Identifies the type of motion."), new GUIContent(lMotion.GetType().Name));
        EditorGUILayout.LabelField(new GUIContent("Namespace", "Specifies the container the motion belongs to."), new GUIContent(lMotion.GetType().Namespace));

        // Force the name at the top
        string lMotionName = EditorGUILayout.TextField(new GUIContent("Name", "Friendly name of the motion that can be searched for."), lMotion.Name);
        if (lMotionName != lMotion.Name)
        {
            lIsDirty = true;
            lMotion.Name = lMotionName;
        }

        // Reactivation delay
        float lReactivationDelay = EditorGUILayout.FloatField(new GUIContent("Reactivation Delay", "Once deactivated, seconds before activation can occur again."), lMotion.ReactivationDelay);
        if (lReactivationDelay != lMotion.ReactivationDelay)
        {
            lIsDirty = true;
            lMotion.ReactivationDelay = lReactivationDelay;
        }
        
        // Render out the accessable properties using reflection
        PropertyInfo[] lProperties = lMotion.GetType().GetProperties();
        foreach (PropertyInfo lProperty in lProperties)
        {
            if (!lProperty.CanWrite) { continue; }

            string lTooltip = "";
            object[] lAttributes = lProperty.GetCustomAttributes(typeof(MotionTooltipAttribute), true);
            foreach (MotionTooltipAttribute lAttribute in lAttributes)
            {
                lTooltip = lAttribute.Tooltip;
            }

            // Unfortunately Binding flags don't seem to be working. So,
            // we need to ensure we don't include base properties
            bool lAdd = true;
            for (int i = 0; i < mBaseProperties.Length; i++)
            {
                if (lProperty.Name == mBaseProperties[i].Name)
                {
                    lAdd = false;
                    break;
                }
            }

            if (!lAdd) { continue; }

            // Grab the current value
            object lOldValue = lProperty.GetValue(lMotion, null);

            // Based on the type, show an edit field
            if (lProperty.PropertyType == typeof(string))
            {
                string lNewValue = EditorGUILayout.TextField(new GUIContent(lProperty.Name, lTooltip), (string)lOldValue);
                if (lNewValue != (string)lOldValue)
                {
                    lIsDirty = true;
                    lProperty.SetValue(lMotion, lNewValue, null);
                }
            }
            else if (lProperty.PropertyType == typeof(int))
            {
                int lNewValue = EditorGUILayout.IntField(new GUIContent(lProperty.Name, lTooltip), (int)lOldValue);
                if (lNewValue != (int)lOldValue)
                {
                    lIsDirty = true;
                    lProperty.SetValue(lMotion, lNewValue, null);
                }
            }
            else if (lProperty.PropertyType == typeof(float))
            {
                float lNewValue = EditorGUILayout.FloatField(new GUIContent(lProperty.Name, lTooltip), (float)lOldValue);
                if (lNewValue != (float)lOldValue)
                {
                    lIsDirty = true;
                    lProperty.SetValue(lMotion, lNewValue, null);
                }
            }
            else if (lProperty.PropertyType == typeof(bool))
            {
                bool lNewValue = EditorGUILayout.Toggle(new GUIContent(lProperty.Name, lTooltip), (bool)lOldValue);
                if (lNewValue != (bool)lOldValue)
                {
                    lIsDirty = true;
                    lProperty.SetValue(lMotion, lNewValue, null);
                }
            }
            else if (lProperty.PropertyType == typeof(Vector2))
            {
                Vector2 lNewValue = EditorGUILayout.Vector2Field(new GUIContent(lProperty.Name, lTooltip), (Vector2)lOldValue);
                if (lNewValue != (Vector2)lOldValue)
                {
                    lIsDirty = true;
                    lProperty.SetValue(lMotion, lNewValue, null);
                }
            }
            else if (lProperty.PropertyType == typeof(Vector3))
            {
                Vector3 lNewValue = EditorGUILayout.Vector3Field(new GUIContent(lProperty.Name, lTooltip), (Vector3)lOldValue);
                if (lNewValue != (Vector3)lOldValue)
                {
                    lIsDirty = true;
                    lProperty.SetValue(lMotion, lNewValue, null);
                }
            }
            else if (lProperty.PropertyType == typeof(Vector4))
            {
                Vector4 lNewValue = EditorGUILayout.Vector4Field(lProperty.Name, (Vector4)lOldValue);
                if (lNewValue != (Vector4)lOldValue)
                {
                    lIsDirty = true;
                    lProperty.SetValue(lMotion, lNewValue, null);
                }
            }
        }

        // Update the motion if there's a change
        if (lIsDirty)
        {
            mIsDirty = true;
            lLayer.MotionDefinitions[rMotionIndex] = lMotion.SerializeMotion();
        }
    }

    /// <summary>
    /// Adds a new layer
    /// </summary>
    /// <returns>Index of the new layer</returns>
    private int AddLayer()
    {
        mIsDirty = true; 

        MotionControllerLayer lLayer = new MotionControllerLayer(mMotionController);
        lLayer.Index = mMotionController.MotionLayers.Count;

        mMotionController.MotionLayers.Add(lLayer);

        mSelectedLayerIndex = lLayer.Index;
        return lLayer.Index;
    }

    /// <summary>
    /// Removes the specified layer
    /// </summary>
    /// <param name="rLayerIndex">Index of the layer to remove</param>
    private int RemoveLayer(int rLayerIndex)
    {
        if (rLayerIndex < 0) { return rLayerIndex; }
        if (rLayerIndex >= mMotionController.MotionLayers.Count) { return rLayerIndex; }

        mIsDirty = true;

        mMotionController.MotionLayers.RemoveAt(rLayerIndex);
        if (rLayerIndex >= mMotionController.MotionLayers.Count)
        {
            rLayerIndex--;
            mSelectedMotionIndex = 0;
        }

        return rLayerIndex;
    }

    /// <summary>
    /// Adds a new motion
    /// </summary>
    /// <param name="rLayerIndex">Layer index to add the motion to</param>
    /// <returns>Index of the new motion</returns>
    private int AddMotion(int rLayerIndex)
    {
        if (rLayerIndex < 0) { return -1; }
        if (rLayerIndex >= mMotionController.MotionLayers.Count) { return -1; }

        mIsDirty = true; 

        mMotionController.MotionLayers[rLayerIndex].MotionDefinitions.Add("{ \"Type\" : \"" + typeof(MotionControllerMotion).AssemblyQualifiedName + "\" }");
        mSelectedMotionIndex = mMotionController.MotionLayers[rLayerIndex].MotionDefinitions.Count - 1;

        // Return the new index
        return mSelectedMotionIndex;
    }

    /// <summary>
    /// Removes the specified motion
    /// </summary>
    /// <param name="rLayerIndex">Index of the layer the motion belongs to</param>
    /// <param name="rMotionIndex">Index of the motion to remove</param>
    private int RemoveMotion(int rLayerIndex, int rMotionIndex)
    {
        if (rLayerIndex < 0) { return rMotionIndex; }
        if (rMotionIndex < 0) { return rMotionIndex; }
        if (rLayerIndex >= mMotionController.MotionLayers.Count) { return rMotionIndex; }
        if (rMotionIndex >= mMotionController.MotionLayers[rLayerIndex].MotionDefinitions.Count) { return rMotionIndex; }

        mIsDirty = true;

        mMotionController.MotionLayers[rLayerIndex].Motions.RemoveAt(rMotionIndex);
        mMotionController.MotionLayers[rLayerIndex].MotionDefinitions.RemoveAt(rMotionIndex);

        if (rMotionIndex >= mMotionController.MotionLayers[rLayerIndex].MotionDefinitions.Count) { rMotionIndex--; }

        return rMotionIndex;
    }

    /// <summary>
    /// Changes the current motion to a new type.
    /// </summary>
    /// <param name="rLayerIndex">Layer index to add the motion to</param>
    /// <param name="rMotionIndex">Index of the new motion</param>
    /// <param name="rNewType">Type to change the motion to</param>
    private void ChangeMotionType(int rLayerIndex, int rMotionIndex, Type rNewType)
    {
        if (rLayerIndex < 0) { return; }
        if (rMotionIndex < 0) { return; }
        if (rLayerIndex >= mMotionController.MotionLayers.Count) { return; }
        if (rMotionIndex >= mMotionController.MotionLayers[rLayerIndex].Motions.Count) { return; }

        mIsDirty = true;

        // Keep track of the old motion so we can move properties overs
        MotionControllerMotion lOldMotion = mMotionController.MotionLayers[rLayerIndex].Motions[rMotionIndex];

        // Create the new instance
        //MotionControllerMotion lNewMotion = MotionControllerMotion.CreateInstance(rNewType.Name) as MotionControllerMotion;
        MotionControllerMotion lNewMotion = Activator.CreateInstance(rNewType) as MotionControllerMotion;

        if (lNewMotion != null)
        {
            lNewMotion.Controller = mMotionController;

            // Copy over what we can
            if (lOldMotion != null)
            {
                if (lOldMotion.Name.Length > 0) { lNewMotion.Name = lOldMotion.Name; }

                // Don't copy priority. It causes too many issues when the priority is
                // accidentally left what it was, but for a new motion.
                //if (lOldMotion.Priority > 0) { lNewMotion.Priority = lOldMotion.Priority; }
            }

            // Initialize it
            lNewMotion.LoadAnimatorData();
            mMotionController.MotionLayers[rLayerIndex].Motions[rMotionIndex] = lNewMotion;

            // Finally, clean up
            if (lOldMotion != null) { lOldMotion.Controller = null; }
        }
    }

    /// <summary>
    /// Given the object type, grab the matching motion index
    /// </summary>
    /// <param name="rObject"></param>
    /// <returns></returns>
    private int GetMotionIndex(Type rType)
    {
        for (int i = 0; i < mControllerMotionTypes.Count; i++)
        {
            if (rType == mControllerMotionTypes[i])
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// Retrieves the serialized property that represents the motion
    /// </summary>
    /// <param name="rLayerIndex"></param>
    /// <param name="rMotionIndex"></param>
    /// <returns></returns>
    private SerializedProperty GetSerializedMotion(int rLayerIndex, int rMotionIndex)
    {
        // Now we need to update the serialized object
        SerializedProperty lLayerListSP = mMotionControllerSO.FindProperty("MotionLayers");
        if (lLayerListSP != null)
        {
            SerializedProperty lLayerItemSP = lLayerListSP.GetArrayElementAtIndex(rLayerIndex);
            if (lLayerItemSP != null)
            {
                SerializedProperty lMotionListSP = lLayerItemSP.FindPropertyRelative("Motions");
                if (lMotionListSP != null)
                {
                    if (lMotionListSP.arraySize > rMotionIndex)
                    {
                        SerializedProperty lMotionItemSP = lMotionListSP.GetArrayElementAtIndex(rMotionIndex);
                        if (lMotionItemSP != null)
                        {
                            return lMotionItemSP;
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a texture given the specified color
    /// </summary>
    /// <param name="rWidth">Width of the texture</param>
    /// <param name="rHeight">Height of the texture</param>
    /// <param name="rColor">Color of the texture</param>
    /// <returns></returns>
    private Texture2D CreateTexture(int rWidth, int rHeight, Color rColor)
    {
        Color[] lPixels = new Color[rWidth * rHeight];
        for (int i = 0; i < lPixels.Length; i++)
        {
            lPixels[i] = rColor;
        }

        Texture2D result = new Texture2D(rWidth, rHeight);
        result.SetPixels(lPixels);
        result.Apply();

        return result;
    }
}
