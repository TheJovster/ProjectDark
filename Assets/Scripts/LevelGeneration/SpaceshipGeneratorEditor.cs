using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(SpaceshipGenerator))]
public class SpaceshipGeneratorEditor : Editor
{
    private SpaceshipGenerator generator;
    private string[] layoutFiles = new string[0];
    private int selectedLayoutIndex = -1;
    private bool showRoomSettings = true;
    private bool showGenerationSettings = true;
    private bool showDebugSettings = true;
    private bool showSaveLoadSettings = true;
    
    // Section toggles
    private Dictionary<string, bool> roomTypeToggles = new Dictionary<string, bool>();
    
    private void OnEnable()
    {
        generator = (SpaceshipGenerator)target;
        RefreshLayoutFiles();
    }
    
    private void RefreshLayoutFiles()
    {
        string layoutPath = "Assets/SpaceshipLayouts/";
        if (Directory.Exists(layoutPath))
        {
            string[] files = Directory.GetFiles(layoutPath, "*.json");
            List<string> fileNames = new List<string>();
            
            foreach (string file in files)
            {
                fileNames.Add(Path.GetFileNameWithoutExtension(file));
            }
            
            layoutFiles = fileNames.ToArray();
        }
        else
        {
            layoutFiles = new string[0];
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Spaceship", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Generate Spaceship");
            generator.GenerateSpaceship();
        }
        
        if (GUILayout.Button("Clear All", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Clear Spaceship");
            generator.CleanupPreviousGeneration();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Room Types Section
        showRoomSettings = EditorGUILayout.Foldout(showRoomSettings, "Room Types", true);
        if (showRoomSettings)
        {
            EditorGUI.indentLevel++;
            
            SerializedProperty roomTypesProp = serializedObject.FindProperty("roomTypes");
            EditorGUILayout.PropertyField(roomTypesProp, new GUIContent("Room Types"), false);
            
            if (roomTypesProp.isExpanded)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Room Type"))
                {
                    roomTypesProp.arraySize++;
                    SerializedProperty newRoomType = roomTypesProp.GetArrayElementAtIndex(roomTypesProp.arraySize - 1);
                    newRoomType.FindPropertyRelative("name").stringValue = "NewRoom";
                    newRoomType.FindPropertyRelative("isRequired").boolValue = false;
                    newRoomType.FindPropertyRelative("maxCount").intValue = 1;
                }
                
                if (GUILayout.Button("Expand All"))
                {
                    for (int i = 0; i < roomTypesProp.arraySize; i++)
                    {
                        roomTypesProp.GetArrayElementAtIndex(i).isExpanded = true;
                    }
                }
                
                if (GUILayout.Button("Collapse All"))
                {
                    for (int i = 0; i < roomTypesProp.arraySize; i++)
                    {
                        roomTypesProp.GetArrayElementAtIndex(i).isExpanded = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Draw each room type
                for (int i = 0; i < roomTypesProp.arraySize; i++)
                {
                    SerializedProperty roomTypeProp = roomTypesProp.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = roomTypeProp.FindPropertyRelative("name");
                    
                    string roomName = nameProp.stringValue;
                    if (string.IsNullOrEmpty(roomName))
                        roomName = "Room " + i;
                        
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // Room header with delete button
                    EditorGUILayout.BeginHorizontal();
                    roomTypeProp.isExpanded = EditorGUILayout.Foldout(roomTypeProp.isExpanded, roomName, true);
                    
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        roomTypesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (roomTypeProp.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.PropertyField(nameProp);
                        EditorGUILayout.PropertyField(roomTypeProp.FindPropertyRelative("prefab"));
                        EditorGUILayout.PropertyField(roomTypeProp.FindPropertyRelative("minSize"));
                        EditorGUILayout.PropertyField(roomTypeProp.FindPropertyRelative("maxSize"));
                        EditorGUILayout.PropertyField(roomTypeProp.FindPropertyRelative("isRequired"));
                        EditorGUILayout.PropertyField(roomTypeProp.FindPropertyRelative("maxCount"));
                        
                        // Required Connections
                        SerializedProperty reqConnectionsProp = roomTypeProp.FindPropertyRelative("requiredConnections");
                        EditorGUILayout.PropertyField(reqConnectionsProp);
                        
                        // Optional Connections
                        SerializedProperty optConnectionsProp = roomTypeProp.FindPropertyRelative("optionalConnections");
                        EditorGUILayout.PropertyField(optConnectionsProp);
                        
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        // Generation Settings
        showGenerationSettings = EditorGUILayout.Foldout(showGenerationSettings, "Generation Settings", true);
        if (showGenerationSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("doorwayWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("doorwayHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("doorPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("corridorPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minRooms"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRooms"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("corridorWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shipSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useCorridors"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roomSpacing"));
            
            EditorGUI.indentLevel--;
        }
        
        // Debug Settings
        showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugVisuals"));
            
            EditorGUI.indentLevel--;
        }
        
        // Save/Load Settings
        showSaveLoadSettings = EditorGUILayout.Foldout(showSaveLoadSettings, "Save/Load Settings", true);
        if (showSaveLoadSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("layoutName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("saveAfterGeneration"));
            
            EditorGUILayout.Space(5);
            
            // Save button
            if (GUILayout.Button("Save Current Layout"))
            {
                generator.SaveLayout(generator.layoutName);
                RefreshLayoutFiles();
            }
            
            EditorGUILayout.Space(5);
            
            // Load section
            EditorGUILayout.LabelField("Load Layout", EditorStyles.boldLabel);
            
            if (layoutFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("No saved layouts found. Generate and save a layout first.", MessageType.Info);
            }
            else
            {
                int newSelectedIndex = EditorGUILayout.Popup("Select Layout", selectedLayoutIndex, layoutFiles);
                if (newSelectedIndex != selectedLayoutIndex)
                {
                    selectedLayoutIndex = newSelectedIndex;
                }
                
                if (selectedLayoutIndex >= 0 && selectedLayoutIndex < layoutFiles.Length)
                {
                    if (GUILayout.Button("Load Selected Layout"))
                    {
                        generator.LoadLayout(layoutFiles[selectedLayoutIndex]);
                    }
                }
            }
            
            if (GUILayout.Button("Refresh Layout List"))
            {
                RefreshLayoutFiles();
            }
            
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
