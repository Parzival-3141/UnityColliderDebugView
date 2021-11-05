using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrawColliders))]
public class DrawCollidersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //@Incomplete: Doesn't save settings on edit/play mode switch
        var drawCol = (target as DrawColliders);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

        drawCol.SelectedObject = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Parent Object"), drawCol.SelectedObject, typeof(GameObject), true);
        drawCol.SelectChildren = EditorGUILayout.Toggle(new GUIContent("Select Children"), drawCol.SelectChildren);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        drawCol.drawColliders  = EditorGUILayout.Toggle(new GUIContent("Draw Colliders"), drawCol.drawColliders);
        EditorGUILayout.ColorField(new GUIContent("Collider Color"), Color.HSVToRGB(0.32f, 0.44f, 1f));
        
        drawCol.drawTriggers   = EditorGUILayout.Toggle(new GUIContent("Draw Triggers"), drawCol.drawTriggers);
        EditorGUILayout.ColorField(new GUIContent("Trigger Color"), Color.HSVToRGB(0.82f, 0.44f, 1f));
    }
}
