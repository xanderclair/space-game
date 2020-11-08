using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DisplayTextureOnPlane))]
public class DisplayNoiseMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DisplayTextureOnPlane displayScript = (DisplayTextureOnPlane)target;
        
        if (DrawDefaultInspector())
        {
            displayScript.display();
        }

        
        if (GUILayout.Button("Generate"))
        {
            displayScript.display();
        }
    }
}
