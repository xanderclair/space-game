using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PreviewMesh))]
public class PreviewMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PreviewMesh script = (PreviewMesh)target;

        if (DrawDefaultInspector())
        {
            script.CreateAndDisplayMesh();
        }

        if (GUILayout.Button("Generate"))
        {
            script.CreateAndDisplayMesh();
        }
    }
}
