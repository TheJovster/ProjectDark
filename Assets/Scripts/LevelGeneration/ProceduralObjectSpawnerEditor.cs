using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(ProceduralObjectSpawner))]
public class ProceduralObjectSpawnerEditor : Editor
{
    private bool showObjectSettings = true;
    private bool showAreaSettings = true;
    private bool showDebugSettings = true;
    
    public override void OnInspectorGUI()
    {
        ProceduralObjectSpawner spawner = (ProceduralObjectSpawner)target;
        
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Objects", GUILayout.Height(30)))
        {
            Undo.RecordObject(spawner, "Spawn Objects");
            spawner.SpawnObjects();
        }
        
        if (GUILayout.Button("Clear Objects", GUILayout.Height(30)))
        {
            Undo.RecordObject(spawner, "Clear Objects");
            spawner.ClearSpawnedObjects();
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Spawnable Objects Section
        showObjectSettings = EditorGUILayout.Foldout(showObjectSettings, "Spawnable Objects", true);
        if (showObjectSettings)
        {
            EditorGUI.indentLevel++;
            
            SerializedProperty objectList = serializedObject.FindProperty("spawnableObjects");
            EditorGUILayout.PropertyField(objectList, true);
            
            if (GUILayout.Button("Add Object"))
            {
                objectList.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Spawn Area Settings
        showAreaSettings = EditorGUILayout.Foldout(showAreaSettings, "Spawn Area Settings", true);
        if (showAreaSettings)
        {
            EditorGUI.indentLevel++;
            
            SerializedProperty spawnArea = serializedObject.FindProperty("spawnArea");
            EditorGUILayout.PropertyField(spawnArea.FindPropertyRelative("areaSize"), new GUIContent("Area Size"));
            EditorGUILayout.PropertyField(spawnArea.FindPropertyRelative("areaOffset"), new GUIContent("Area Offset"));
            EditorGUILayout.PropertyField(spawnArea.FindPropertyRelative("maxObjectsToSpawn"), new GUIContent("Max Objects"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("avoidOverlap"));
            
            if (spawner.avoidOverlap)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minDistanceBetweenObjects"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionLayers"));
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Debug Settings
        showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugGizmos"));
            
            if (spawner.showDebugGizmos)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoColor"));
            }
            
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    private void OnSceneGUI()
    {
        ProceduralObjectSpawner spawner = (ProceduralObjectSpawner)target;
        
        if (!spawner.showDebugGizmos) return;
        
        Handles.matrix = spawner.transform.localToWorldMatrix;
        Handles.color = spawner.gizmoColor;
        
        Vector3 center = spawner.spawnArea.areaOffset;
        Vector3 size = spawner.spawnArea.areaSize;
        
        // Draw handles for area size
        EditorGUI.BeginChangeCheck();
        
        float newSizeX = Handles.ScaleSlider(size.x, center, Vector3.right, Quaternion.identity, HandleUtility.GetHandleSize(center) * 1.5f, 0.1f);
        float newSizeY = Handles.ScaleSlider(size.y, center, Vector3.up, Quaternion.identity, HandleUtility.GetHandleSize(center) * 1.5f, 0.1f);
        float newSizeZ = Handles.ScaleSlider(size.z, center, Vector3.forward, Quaternion.identity, HandleUtility.GetHandleSize(center) * 1.5f, 0.1f);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spawner, "Change Spawn Area Size");
            spawner.spawnArea.areaSize = new Vector3(newSizeX, newSizeY, newSizeZ);
            EditorUtility.SetDirty(spawner);
        }
        
        // Draw handle for area center
        EditorGUI.BeginChangeCheck();
        Vector3 newCenter = Handles.PositionHandle(center, Quaternion.identity);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spawner, "Change Spawn Area Position");
            spawner.spawnArea.areaOffset = newCenter;
            EditorUtility.SetDirty(spawner);
        }
    }
}

// This section has been removed as we no longer need the MinMax property drawer
#endif
