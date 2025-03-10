using UnityEngine;
using UnityEditor;

namespace CBG.FPSMeshTool {

    public class FPSMeshToolWindow : EditorWindow {
        
        [SerializeField]
        GameObject previewInstance;
        [SerializeField] private FPSMeshContext context;
        [SerializeField] private MeshSettings settings = new MeshSettings(1, 1);

        bool showPreview { get { return previewInstance != null; } }

        bool canShowPreview { get { return context.Exists() && context.CanShowPreview; } }
        bool canHidePreview { get { return previewInstance != null; } }

        // GUI strings and whatnot
        GUIContent wtContent = new GUIContent("Weight Threshold", "Minimum bone weight to count a vertex as being attached to a bone.  Increase this if arms or head are including triangles they shouldn't.");
        GUIContent vtContent = new GUIContent("Vertex Threshold", "Number of attached vertices required to count a triangle as being part of arms or head.  Increase this if arms or head are including triangles they shouldn't.");
        GUIContent saContent = new GUIContent("Separate Arms", "Create separate materials for each arm.  This allows toggling visibility on a single arm at a time if desired.");
        GUIContent plContent = new GUIContent("Process Legs", "In addition to separating out the head and arms, also separate out the legs.");
        GUIContent slContent = new GUIContent("Separate Legs", "Create separate materials for each leg.  This allows toggling visibility on a single leg at a time if desired.");

        // GUI layout variables
        Vector2 scrollPos;
        private bool _initialized;
        
        [MenuItem("Window/FPS Mesh Tool")]
        public static void Init() {
            EditorWindow.GetWindow(typeof(FPSMeshToolWindow), false, "FPS Mesh Tool");
        }

        void OnInspectorUpdate() {
            // This will only get called 10 times per second.
            Repaint();
        }

        private void InitOnce() {
            if (_initialized) return;
            var detectedPipeline = FPSMeshTool.DetectRenderPipeline();
            var currentPipeline = FPSMeshTool.DetectFPSMeshToolPipeline();
            if (detectedPipeline != currentPipeline) {
                FPSMeshTool.AutoConfigureRenderPipeline();
            }
            _initialized = true;
        }
        
        void OnGUI() {
            InitOnce();
            // sanity checks //
            // if preview is showing, but one of the source objects is invalid
            if (showPreview && (!context.Exists() || !context.CanShowPreview)) {
                RemovePreview();
            }

            // create window as a large scrolling area
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Render pipeline validation and auto configuration
            var currentPipeline = FPSMeshTool.RenderPipeline;
            var detectedPipeline = FPSMeshTool.DetectRenderPipeline();
            if (currentPipeline != detectedPipeline) {
                var pipelineName = currentPipeline >= RenderPipeline.BuiltIn ? currentPipeline.ToString() : "[UNKNOWN]";
                EditorGUILayout.HelpBox($"Configured render pipeline {pipelineName} does not match detected render pipeline {detectedPipeline}.  Please update.", MessageType.Warning);
                if (GUILayout.Button("Fix")) {
                    FPSMeshTool.AutoConfigureRenderPipeline();
                }
            }
            // Render pipeline configuration
            EditorGUI.BeginChangeCheck();
            var newPipeline = (RenderPipeline)EditorGUILayout.EnumPopup("Render Pipeline", FPSMeshTool.RenderPipeline);
            if (EditorGUI.EndChangeCheck()) {
                FPSMeshTool.RenderPipeline = newPipeline;
            }
            
            EditorGUI.BeginChangeCheck();
            var newSourceObject = EditorGUILayout.ObjectField("Object or Prefab", context.Exists() ? context.SourceObject : null, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(this, "sourceObject changed");
                context = new FPSMeshContext(newSourceObject, settings);
                OnSourceObjectUpdated();
            }
            if (!context.Exists() || !context.ValidObject) {
                if (context.Exists() && context.IsPrefab) {
                    EditorGUILayout.HelpBox("Source Object is a prefab.  Press Instantiate to continue.", MessageType.Warning);
                    if (GUILayout.Button("Instantiate")) {
                        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(context.SourceObject);
                        Undo.RegisterCreatedObjectUndo(go, "Instantiated " + go.name);
                        go.name = context.SourceObject.name;
                        Undo.RecordObject(this, "switched sourceObject to instance");
                        context = new FPSMeshContext(go, settings);
                        // sourceObject = go;
                        OnSourceObjectUpdated();
                    }
                } else {
                    EditorGUILayout.HelpBox("Select a GameObject or prefab to begin.", MessageType.Warning);
                }
            }

            if (!context.Exists()) {
                EditorGUILayout.EndScrollView();
                return;
            }
            
            EditorGUI.BeginChangeCheck();
            var newWeightThreshold = EditorGUILayout.Slider(wtContent, settings.weightThreshold, 0f, 1f);
            var newVertexThreshold = EditorGUILayout.IntSlider(vtContent, settings.vertexThreshold, 1, 3);

            if (EditorGUI.EndChangeCheck()) {
                if (newWeightThreshold != settings.weightThreshold) {
                    Undo.RecordObject(this, "Adjusted weight threshold");
                    settings.weightThreshold = newWeightThreshold;
                }
                if (newVertexThreshold != settings.vertexThreshold) {
                    Undo.RecordObject(this, "Adjusted vertex threshold");
                    settings.vertexThreshold = newVertexThreshold;
                }
                OnThresholdUpdated();
            }

            EditorGUILayout.Space();

            if (context.Exists() && context.ValidObject) {
                if (context.ValidRenderers) {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Label("Choose Renderers", EditorStyles.boldLabel);
                    for (int i = 0; i < context.SourceRenderers.Length; i++) {
                        var entry = context.SourceRenderers[i];
                        var useRenderer = EditorGUILayout.Toggle(entry.name, entry.useRenderer);
                        if (useRenderer != entry.useRenderer) {
                            Undo.RecordObject(this, "Updated useRenderer");
                            entry.useRenderer = useRenderer;
                            context.SourceRenderers[i] = entry;
                        }
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("None")) {
                        for (int i = 0; i < context.SourceRenderers.Length; i++) {
                            var entry = context.SourceRenderers[i];
                            entry.useRenderer = false;
                            context.SourceRenderers[i] = entry;
                        }
                        Undo.RecordObject(this, "Updated useRenderer");
                    }
                    if (GUILayout.Button("All")) {
                        for (int i = 0; i < context.SourceRenderers.Length; i++) {
                            var entry = context.SourceRenderers[i];
                            entry.useRenderer = true;
                            context.SourceRenderers[i] = entry;
                        }
                        Undo.RecordObject(this, "Updated useRenderer");
                    }
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck()) {
                        OnRenderersUpdated();
                    }
                } else {
                    EditorGUILayout.HelpBox("Object doesn't contain any skinned mesh renderers.", MessageType.Warning);
                    if (showPreview) {
                        RemovePreview();
                    }
                }
                EditorGUILayout.Space();
            }

            if (context.ValidObject && context.ValidRenderers) {
                if (!context.ValidRootBone) {
                    // Reguess bones in case root bone has been set since last failure
                    context.GuessBones();
                }
                string boneMessage;
                if (context.ValidRootBone) {
                    boneMessage = "Select a bone that is a child of " + context.Bones.root.name;
                } else {
                    boneMessage = "Please set the root bone in your skinned mesh renderer.";
                }
                GUILayout.Label("Bones", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                var bones = context.Bones;
                var newHeadBone = (Transform)EditorGUILayout.ObjectField("Head", bones.head, typeof(Transform), true);
                if (!context.ValidHeadBone) EditorGUILayout.HelpBox(boneMessage, MessageType.None);
                var newLeftArmBone = (Transform)EditorGUILayout.ObjectField("Left Arm", bones.leftArm, typeof(Transform), true);
                if (!context.ValidLeftArmBone) EditorGUILayout.HelpBox(boneMessage, MessageType.None);
                var newRightArmBone = (Transform)EditorGUILayout.ObjectField("Right Arm", bones.rightArm, typeof(Transform), true);
                if (!context.ValidRightArmBone) EditorGUILayout.HelpBox(boneMessage, MessageType.None);
                if (EditorGUI.EndChangeCheck()) {
                    if (newHeadBone != bones.head) {
                        Undo.RecordObject(this, "Changed head bone");
                        bones.head = newHeadBone;
                    }
                    if (newLeftArmBone != bones.leftArm) {
                        Undo.RecordObject(this, "Changed left arm bone");
                        bones.leftArm = newLeftArmBone;
                    }
                    if (newRightArmBone != bones.rightArm) {
                        Undo.RecordObject(this, "Changed right arm bone");
                        bones.rightArm = newRightArmBone;
                    }
                    context.Bones = bones;
                    OnBonesUpdated();
                }
                // separate arms
                EditorGUI.BeginChangeCheck();
                var newSeparateArms = EditorGUILayout.Toggle(saContent, settings.separateArms);
                if (EditorGUI.EndChangeCheck()) {
                    if (newSeparateArms != settings.separateArms) {
                        Undo.RecordObject(this, "Toggled Separate Arms");
                        settings.separateArms = newSeparateArms;
                    }
                    OnBonesUpdated();
                }
                // show leg section
                EditorGUI.BeginChangeCheck();
                var newProcessLegs = EditorGUILayout.Toggle(plContent, settings.processLegs);
                bool newSeparateLegs = false;
                Transform newLeftLegBone = null;
                Transform newRightLegBone = null;
                if (settings.processLegs) {
                    newSeparateLegs = EditorGUILayout.Toggle(slContent, settings.separateLegs);
                    newLeftLegBone = (Transform)EditorGUILayout.ObjectField("Left Leg", context.Bones.leftLeg, typeof(Transform), true);
                    if (!context.ValidLeftLegBone) EditorGUILayout.HelpBox(boneMessage, MessageType.None);
                    newRightLegBone = (Transform)EditorGUILayout.ObjectField("Right Leg", context.Bones.rightLeg, typeof(Transform), true);
                    if (!context.ValidRightLegBone) EditorGUILayout.HelpBox(boneMessage, MessageType.None);
                }
                if (EditorGUI.EndChangeCheck()) {
                    if (newProcessLegs != settings.processLegs) {
                        Undo.RecordObject(this, "Toggled Process Legs");
                        settings.processLegs = newProcessLegs;
                    } else {
                        if (newSeparateLegs != settings.separateLegs) {
                            Undo.RecordObject(this, "Toggled Separate Legs");
                            settings.separateLegs = newSeparateLegs;
                        }
                        if (newLeftLegBone != context.Bones.leftLeg) {
                            Undo.RecordObject(this, "Changed left leg bone");
                            context.Bones.leftLeg = newLeftLegBone;
                        }
                        if (newRightLegBone != context.Bones.rightLeg) {
                            Undo.RecordObject(this, "Changed right leg bone");
                            context.Bones.rightLeg = newRightLegBone;
                        }
                    }
                    OnBonesUpdated();
                }
                // show "Include xxxxx" section
                GUILayout.Label("Final Mesh Contents", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                bool newIncludeHead = false;
                bool newIncludeBody = false;
                bool newIncludeLeftArm = false;
                bool newIncludeRightArm = false;
                bool newIncludeLeftLeg = false;
                bool newIncludeRightLeg = false;
                var newIncludeAll = EditorGUILayout.Toggle("Include All", settings.include.All);
                if (!settings.include.All) {
                    newIncludeHead = EditorGUILayout.Toggle("Include Head", settings.include.head);
                    newIncludeBody = EditorGUILayout.Toggle("Include Body", settings.include.body);
                    newIncludeLeftArm = EditorGUILayout.Toggle("Include LeftArm", settings.include.leftArm);
                    newIncludeRightArm = EditorGUILayout.Toggle("Include RightArm", settings.include.rightArm);
                    if (settings.processLegs) {
                        newIncludeLeftLeg = EditorGUILayout.Toggle("Include LeftLeg", settings.include.leftLeg);
                        newIncludeRightLeg = EditorGUILayout.Toggle("Include RightLeg", settings.include.rightLeg);
                    }
                }
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(this, "Changed include toggles");
                    if (newIncludeAll != settings.include.All) {
                        settings.include.All = newIncludeAll;
                    } else {
                        settings.include.head = newIncludeHead;
                        settings.include.body = newIncludeBody;
                        settings.include.leftArm = newIncludeLeftArm;
                        settings.include.rightArm = newIncludeRightArm;
                        if (settings.processLegs) {
                            settings.include.leftLeg = newIncludeLeftLeg;
                            settings.include.rightLeg = newIncludeRightLeg;
                        }
                    }
                    OnIncludeUpdated();
                }

            }


            if (canHidePreview) {
                if (GUILayout.Button("Remove Preview")) {
                    RemovePreview();
                }
            } else if (canShowPreview) {
                if (GUILayout.Button("Create Preview")) {
                    CreatePreview();
                }
            }

            if (showPreview) {
                if (GUILayout.Button("Build FPS mesh")) {
                    FPSMeshTool.BuildFPSMesh(context);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void OnSourceObjectUpdated() {
            if (context.SourceObject != null) {
                if (EditorUtility.IsPersistent(context.SourceObject)) {
                    // Remove preview if sourceObject is a prefab
                    RemovePreview();
                }
                Undo.RecordObject(this, "changed source renderers");
            } else {
                // source object removed - clear preview and renderers
                RemovePreview();
                Undo.RecordObject(this, "changed source renderers");
                context = null;
            }
            // GuessBones();
            OnRenderersUpdated();
        }

        void OnThresholdUpdated() {
            if (showPreview) {
                UpdatePreview();
            }
        }

        void OnIncludeUpdated() {
            if (showPreview) {
                UpdatePreview();
            }
        }

        void OnRenderersUpdated() {
            if (showPreview) {
                UpdatePreview();
            }
        }

        void OnBonesUpdated() {
            if (showPreview) {
                UpdatePreview();
            }
        }

        // Build preview meshes
        void BuildPreviewMeshes() {
            if (canShowPreview) {
                //float startTime = Time.realtimeSinceStartup;

                // reset material and material name lists
                Undo.RecordObject(this, "Changed materials");
                context.Settings = settings;
                FPSMeshTool.ConvertMeshes(context);
                //float endTime = Time.realtimeSinceStartup;
                //Debug.Log("Elapsed Time: " + (endTime - startTime));
            }
        }

        void UpdatePreview() {
            var sourceObject = context.SourceObject;
            Vector3 pos = sourceObject.transform.position + sourceObject.transform.forward;
            Quaternion rot = sourceObject.transform.rotation;
            if (previewInstance != null) {
                pos = previewInstance.transform.position;
                rot = previewInstance.transform.rotation;
                RemovePreview();
            }
            Undo.RecordObject(this, "switched previewInstance");
            previewInstance = FPSMeshTool.CreateTargetObject(context, pos, rot);
            // previewInstance = Instantiate(context.SourceObject, pos, rot);
            // Undo.RegisterCreatedObjectUndo(previewInstance, "Created previewInstance");
            // previewInstance.name = "FPSMesh - " + sourceObject.name;
            // context.SetTargetObject(previewInstance);
            BuildPreviewMeshes();
        }

        void CreatePreview() {
            UpdatePreview();
        }

        void RemovePreview() {
            if (showPreview) {
                Undo.RecordObject(this, "Removed previewInstance");
                Undo.DestroyObjectImmediate(previewInstance);
            }
        }

    }
}