#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CBG.FPSMeshTool {
    public enum RenderPipeline {
        BuiltIn,
        URP,
        HDRP
    }
    
    public static class FPSMeshHelpers {
        public static bool Exists(this FPSMeshContext context) {
            return context != null;
        }

        public static bool IsValid(this FPSMeshContext context) {
            return context != null && context.ValidObject;
        }

        private static GameObject[] GetObjects(this GameObject go) {
            List<GameObject> objects = new List<GameObject>();
            if (go != null) {
                objects.Add(go);
            }
            foreach (Transform child in go.transform) {
                objects.AddRange(child.gameObject.GetObjects());
            }
            return objects.ToArray();
        }

        public static SkinnedMeshRenderer[] GetRenderers(this GameObject go) {
            List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
            SkinnedMeshRenderer renderer;
            GameObject[] objects = go.GetObjects();
            foreach (GameObject currentObject in objects) {
                renderer = currentObject.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null) {
                    renderers.Add(renderer);
                }
            }

            return renderers.ToArray();
        }
    }

    public static class FPSMeshTool {
        const string ASSET_PATH = "FPSMeshTool/";
        const string MESH_SUBPATH = "Meshes/";
        const string MAT_SUBPATH = "Materials/";

        public static string PrefabPath => ASSET_PATH;
        public static string MeshPath => ASSET_PATH + MESH_SUBPATH;
        public static string MaterialPath => ASSET_PATH + MAT_SUBPATH;
        
        // patterns to match bone names
        static string[] headPatterns = {"head"};
        static string[] leftArmPatterns =
            {"left.*arm", "left.*upper.*arm", "l.*upper.*arm", "upper_arm.l", "lshldr", "l.*arm", "l.*shoulder"};
        static string[] rightArmPatterns =
            {"right.*arm", "right.*upper.*arm", "r.*upper.*arm", "upper_arm.r", "rshldr", "r.*arm", "r.*shoulder"};
        static string[] leftLegPatterns =
            {"left.*thigh", "left.*upper.*leg", "left.*up.*leg", "l.*thigh", "l.*upper.*leg", "left.*leg", "l.*leg"};
        static string[] rightLegPatterns = {
            "right.*thigh", "right.*upper.*leg", "right.*up.*leg", "r.*thigh", "r.*upper.*leg", "right.*leg", "r.*leg"
        };

        private static Material headMat = GetMaterialWithName("HeadPreviewMat");
        private static Material bodyMat = GetMaterialWithName("BodyPreviewMat");
        private static Material armsMat = GetMaterialWithName("ArmsPreviewMat");
        private static Material legsMat = GetMaterialWithName("LegsPreviewMat");
        private static Material invisibleMat = GetMaterialWithName("InvisibleMaterial");
        private static Color headColor = Color.red;
        private static Color bodyColor = Color.blue;
        private static Color armsColor = Color.yellow;
        private static Color legsColor = Color.green;
        
        private static Shader visibleShader = Shader.Find("Standard");
        private static Shader urpVisibleShader = Shader.Find("Universal Render Pipeline/Lit");
        private static Shader hrdpVisibleShader = Shader.Find("HDRP/Lit");
        private static Shader invisibleShader = Shader.Find("Transparent/Invisible Shadow Caster");
        private static Shader urpInvisibleShader = Shader.Find("URP/Invisible Shadow Caster");
        private static Shader hdrpInvisibleShader = Shader.Find("Shader Graphs/HDRPInvisibleShadowCaster");
        private static bool URPInstalled => urpVisibleShader != null;
        private static bool HDRPInstalled => hrdpVisibleShader != null;
        
        private static RenderPipeline _renderPipeline = (RenderPipeline)(-1);
        public static RenderPipeline RenderPipeline {
            get {
                if (_renderPipeline == (RenderPipeline)(-1)) {
                    _renderPipeline = DetectFPSMeshToolPipeline();
                }
                return _renderPipeline;
            }
            set => ConfigureRenderPipeline(value);
        }

        public static void AutoConfigureRenderPipeline() {
            var detectedPipeline = DetectRenderPipeline();
            ConfigureRenderPipeline(detectedPipeline);
        }
        
        public static void ConfigureRenderPipeline(RenderPipeline pipeline) {
            Debug.Log($"Configuring FPS Mesh Tool for {pipeline} render pipeline");
            (Shader visibleShader, Shader invisibleShader) shaders = default;
            switch (pipeline) {
                case RenderPipeline.BuiltIn:
                    shaders = (visibleShader, invisibleShader);
                    break;
                case RenderPipeline.URP:
                    shaders = (urpVisibleShader, urpInvisibleShader);
                    break;
                case RenderPipeline.HDRP:
                    shaders = (hrdpVisibleShader, hdrpInvisibleShader);
                    break;
            }
            headMat.shader = shaders.visibleShader;
            headMat.color = headColor;
            bodyMat.shader = shaders.visibleShader;
            bodyMat.color = bodyColor;
            armsMat.shader = shaders.visibleShader;
            armsMat.color = armsColor;
            legsMat.shader = shaders.visibleShader;
            legsMat.color = legsColor;
            invisibleMat.shader = shaders.invisibleShader;
            EditorUtility.SetDirty(headMat);
            EditorUtility.SetDirty(bodyMat);
            EditorUtility.SetDirty(armsMat);
            EditorUtility.SetDirty(legsMat);
            EditorUtility.SetDirty(invisibleMat);
            
            _renderPipeline = pipeline;
        }

        public static RenderPipeline DetectFPSMeshToolPipeline() {
            return DetectPipelineFrom(headMat, bodyMat, armsMat, legsMat, invisibleMat);

            RenderPipeline DetectPipelineFrom(params Material[] materials) {
                RenderPipeline detectedPipeline = DetectPipelineFromMaterial(materials[0]);
                foreach (var mat in materials) {
                    if (DetectPipelineFromMaterial(mat) != detectedPipeline) {
                        return (RenderPipeline) (-1);
                    }
                }
                return detectedPipeline;
            }

            RenderPipeline DetectPipelineFromMaterial(Material mat) {
                if (mat.shader == visibleShader || mat.shader == invisibleShader) {
                    return RenderPipeline.BuiltIn;
                } else if (URPInstalled && (mat.shader == urpVisibleShader || mat.shader == urpInvisibleShader)) {
                    return RenderPipeline.URP;
                } else if (HDRPInstalled && (mat.shader == hrdpVisibleShader || mat.shader == hdrpInvisibleShader)) {
                    return RenderPipeline.HDRP;
                }
                return (RenderPipeline) (-1);
            }
        }

        public static RenderPipeline DetectRenderPipeline() {
            var detectedPipeline = RenderPipeline.BuiltIn;
            if (GraphicsSettings.defaultRenderPipeline != null) {
                if (GraphicsSettings.defaultRenderPipeline.GetType().Name == "UniversalRenderPipelineAsset") {
                    detectedPipeline = RenderPipeline.URP;
                } else if (GraphicsSettings.defaultRenderPipeline.GetType().Name is "HDRenderPipelineAsset" or "HighDefinitionRenderPipelineAsset") {
                    detectedPipeline = RenderPipeline.HDRP;
                }
            }
            return detectedPipeline;
        }
        
        public static Material GetMaterialWithName(string matName) {
            var guids = AssetDatabase.FindAssets(matName + " t:material");
            return guids.Length > 0
                ? (Material) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), typeof(Material))
                : null;
        }

        public static void AddFPSMeshes(FPSMeshContext context) {
            // strip out any renderers with useRenderer set false
            context.SourceRenderers = context.SourceRenderers.Where(r => r.useRenderer).ToArray();
            var createChildObjects = context.SourceRenderers.Length > 1;
            var targetObject = context.SourceObject.CreateChildObject("FPSMeshRenderers");
            foreach (var sourceRend in context.SourceRenderers) {
                var rendObject = createChildObjects ? targetObject.CreateChildObject(sourceRend.renderer.name) : targetObject;
                var targetRenderer = rendObject.AddComponent<SkinnedMeshRenderer>();
                EditorUtility.CopySerialized(sourceRend.renderer, targetRenderer);
            }
            context.SetTargetObject(targetObject);
            ConvertMeshes(context);
        }
        
        public static void CreateAndConvertTargetObject(FPSMeshContext context) {
            CreateTargetObject(context);
            ConvertMeshes(context);
        }

        public static GameObject CompleteFPSMeshCreation(FPSMeshContext context) {
            if (!context.targetObject) {
                if (context.ModifyOriginal) {
                    AddFPSMeshes(context);
                } else {
                    CreateAndConvertTargetObject(context);
                }
            }            
            var prefab = BuildFPSMesh(context);
            Debug.Log($"{(context.ModifyOriginal ? "Added FPS Renderers to" : "Created")} {prefab}", prefab);
            DestroyWorkingObjects(context);
            return prefab;
        }
        
        public static GameObject CreateTargetObject(FPSMeshContext context, Vector3 position = default,
            Quaternion rotation = default) {
            if (rotation == default) rotation = Quaternion.identity;
            var targetObject = Object.Instantiate(context.SourceObject, position, rotation);
            targetObject.name = "FPSMesh - " + context.SourceObject.name;
            Undo.RegisterCreatedObjectUndo(targetObject, $"Created {targetObject.name}");
            context.SetTargetObject(targetObject);
            return targetObject;
        }

        public static void ConvertMeshes(FPSMeshContext context) {
            context.Materials = new();
            context.MaterialNames = new();
            for (int i = 0; i < context.TargetRenderers.Length; i++) {
                if (context.SourceRenderers[i].useRenderer) {
                    ConvertToFPSMesh(context, context.SourceRenderers[i].renderer, context.TargetRenderers[i]);
                }
            }
        }

        // Convert the preview object into a prefab with appropriate materials
        // Copy blend shapes if there are any (Unity 5.3+)
        public static GameObject BuildFPSMesh(FPSMeshContext context) {
            GameObject prefab;
            try {
                EditorUtility.DisplayProgressBar("Building FPS Mesh Prefab", "Creating directories.", 0f);
                // Ensure output directories are set, and that they exist 
                if (string.IsNullOrEmpty(context.PrefabPath)) context.PrefabPath = PrefabPath;
                if (string.IsNullOrEmpty(context.MeshPath)) context.MeshPath = MeshPath;
                if (string.IsNullOrEmpty(context.MaterialPath)) context.MaterialPath = MaterialPath;

                var sourceRenderers = context.SourceRenderers;
                var targetRenderers = context.TargetRenderers;

                int matIndex = 0;
                for (int i = 0; i < context.TargetRenderers.Length; i++) {
                    // Show progress bar
                    EditorUtility.DisplayProgressBar("Building FPS Mesh Prefab",
                        "Processing skinned mesh renderer " + sourceRenderers[i].name,
                        (float) i / targetRenderers.Length);
                    if (sourceRenderers[i].useRenderer) {
                        // Process blend shapes (requires Unity 5.3+)
                        // Do this before saving mesh prefab - blendshapes not properly saved otherwise
                        ProcessBlendShapes(sourceRenderers[i].renderer, targetRenderers[i]);
                        // Create mesh asset if not already present
                        if (!AssetDatabase.Contains(targetRenderers[i].sharedMesh)) {
                            EnsureDirectoryExists(context.MeshPath);
                            EditorUtility.DisplayProgressBar("Building FPS Mesh Prefab",
                                "Creating mesh asset for " + sourceRenderers[i].name,
                                (float) i / targetRenderers.Length);
                            AssetDatabase.CreateAsset(targetRenderers[i].sharedMesh,
                                GetValidAssetName(GenerateMeshFileName(context, sourceRenderers[i].renderer)));
                        }
                        // Add materials to mesh
                        EditorUtility.DisplayProgressBar("Building FPS Mesh Prefab",
                            "Creating materials for " + sourceRenderers[i].name, (float) i / targetRenderers.Length);
                        Material[] mats = new Material[targetRenderers[i].sharedMaterials.Length];
                        for (int rendererMatIndex = 0;
                             rendererMatIndex < targetRenderers[i].sharedMaterials.Length;
                             rendererMatIndex++) {
                            EnsureDirectoryExists(context.MaterialPath);
                            mats[rendererMatIndex] = new Material(context.Materials[matIndex]);
                            AssetDatabase.CreateAsset(mats[rendererMatIndex],
                                GetValidAssetName(GenerateMaterialFileName(context,
                                    context.MaterialNames[matIndex])));
                            matIndex++;
                        }
                        Undo.RecordObject(targetRenderers[i], "Updated renderer materials");
                        targetRenderers[i].sharedMaterials = mats;
                    }
                }
                // Create, or update, prefab, depending on whether targetObject is a child of sourceObject
                if (context.SourceObject.transform == context.targetObject.transform.parent) {
                    var prefabStage = PrefabStageUtility.GetPrefabStage(context.SourceObject);
                    if (prefabStage != null) {
                        // in prefab mode
                        PrefabUtility.SaveAsPrefabAsset(context.SourceObject, prefabStage.assetPath);
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
                    } else {
                        // is a scene object
                        PrefabUtility.ApplyPrefabInstance(context.SourceObject, InteractionMode.AutomatedAction);
                        prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(context.SourceObject);
                    }
                } else {
                    EnsureDirectoryExists(context.PrefabPath);
                    prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(context.targetObject,
                        GetValidAssetName(GeneratePrefabFileName(context)), InteractionMode.UserAction);
                }
                AssetDatabase.SaveAssets();
            } finally {
                EditorUtility.ClearProgressBar();
            }
            return prefab;

            void EnsureDirectoryExists(string subdir) {
                var path = Path.Combine(Application.dataPath, subdir);
                if (!Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            }
        }

        // Copy all blend shapes from one mesh to another.
        // Only works in Unity 5.3 and newer.
        private static void CopyBlendShapes(Mesh sourceMesh, Mesh destMesh) {
            HashSet<string> blendShapeNames = new HashSet<string>();
            // Process blend shapes
            int blendShapeCount = sourceMesh.blendShapeCount;
            int frameCount = 0;
            Vector3[] deltaVertices = new Vector3[sourceMesh.vertexCount];
            Vector3[] deltaNormals = new Vector3[sourceMesh.vertexCount];
            Vector3[] deltaTangents = new Vector3[sourceMesh.vertexCount];

            // Go through blend shapes
            for (int shapeIndex = 0; shapeIndex < blendShapeCount; shapeIndex++) {
                // This can take a while - update the progress bar
                EditorUtility.DisplayProgressBar("Building FPS Mesh Prefab",
                    "Processing blend shapes for " + sourceMesh.name, (float) shapeIndex / blendShapeCount);
                string shapeName = sourceMesh.GetBlendShapeName(shapeIndex);
                if (blendShapeNames.Contains(shapeName)) {
                    var newShapeName = $"{shapeName}_{shapeIndex}";
                    Debug.LogWarning($"Duplicate blend shape on mesh {sourceMesh.name}. Renaming: [{shapeName}]->[{newShapeName}]");
                    shapeName = newShapeName;
                }
                blendShapeNames.Add(shapeName);
                frameCount = sourceMesh.GetBlendShapeFrameCount(shapeIndex);
                // For each frame in the shape, get vertices and add the frame to the new mesh
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++) {
                    try {
                        float frameWeight = sourceMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                        sourceMesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals,
                            deltaTangents);
                        destMesh.AddBlendShapeFrame(shapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                    } catch (Exception e) {
                        Debug.LogError($"Error applying blend shape {shapeName} [Index:{shapeIndex} Frame:{frameIndex}] to mesh {destMesh.name}.  Skipping.");
                        Debug.LogException(e);
                    }
                }
            }
        }

        // Check for blend shapes, and process if found.
        // Only works in Unity 5.3 and newer.
        private static void ProcessBlendShapes(SkinnedMeshRenderer sourceRenderer, SkinnedMeshRenderer targetRenderer) {
            // Check for blend shapes
            if (sourceRenderer.sharedMesh.blendShapeCount > 0) {
                // If found, save undo info on target renderer and copy blend shapes
                Undo.RecordObject(targetRenderer, "Updated renderer blend shapes");
                CopyBlendShapes(sourceRenderer.sharedMesh, targetRenderer.sharedMesh);
            }
        }

        private static string GenerateMaterialFileName(FPSMeshContext context, string matName) =>
            Path.Combine(context.MaterialPath, $"{context.targetObject.name} - {matName}.mat");

        private static string GenerateMeshFileName(FPSMeshContext context, SkinnedMeshRenderer renderer) =>
            Path.Combine(context.MeshPath, $"{context.targetObject.name} - {renderer.sharedMesh.name}.asset");

        private static string GeneratePrefabFileName(FPSMeshContext context) =>
            Path.Combine(context.PrefabPath, $"{context.targetObject.name}.prefab");

        private static string GetValidAssetName(string preferredName) {
            return AssetDatabase.GenerateUniqueAssetPath(Path.Combine("Assets", preferredName));
        }

        private static void ConvertToFPSMesh(FPSMeshContext context, SkinnedMeshRenderer sourceRenderer,
            SkinnedMeshRenderer targetRenderer) {
            Bones contextBones = context.Bones;
            MeshSettings settings = context.Settings;
            Mesh baseMesh = sourceRenderer.sharedMesh;
            BoneWeight[] boneWeights = baseMesh.boneWeights;
            var boneOverrides = context.BoneOverrides;            
            // get all bones
            Transform[] sourceBones = sourceRenderer.bones;
            // collect head bones
            Transform[] headBones = GetBones(contextBones.head, sourceBones, BodyPart.Head, boneOverrides);
            int[] headBoneIndices = GetBoneIndices(headBones, sourceBones);
            // collect left arm bones
            Transform[] leftArmBones = GetBones(contextBones.leftArm, sourceBones, BodyPart.LeftArm, boneOverrides);
            int[] leftArmBoneIndices = GetBoneIndices(leftArmBones, sourceBones);
            // collect right arm bones
            Transform[] rightArmBones = GetBones(contextBones.rightArm, sourceBones, BodyPart.RightArm, boneOverrides);
            int[] rightArmBoneIndices = GetBoneIndices(rightArmBones, sourceBones);
            // collect left leg bones
            Transform[] leftLegBones = GetBones(settings.processLegs ? contextBones.leftLeg : null, sourceBones, BodyPart.LeftLeg, boneOverrides);
            int[] leftLegBoneIndices = GetBoneIndices(leftLegBones, sourceBones);
            // collect right Leg bones
            Transform[] rightLegBones = GetBones(settings.processLegs ? contextBones.rightLeg : null, sourceBones, BodyPart.RightLeg, boneOverrides);
            int[] rightLegBoneIndices = GetBoneIndices(rightLegBones, sourceBones);

            Dictionary<int, int[]> boneDict = GetBoneDict(headBoneIndices, leftArmBoneIndices, rightArmBoneIndices,
                leftLegBoneIndices, rightLegBoneIndices);

            List<int[]> subMeshes = new List<int[]>();
            List<Material> mats = new List<Material>();

            // create new mesh
            Mesh newMesh = new Mesh();
            // copy vertices, uv, boneweights
            newMesh.vertices = baseMesh.vertices;
            newMesh.uv = baseMesh.uv;
            newMesh.normals = baseMesh.normals;
            newMesh.colors = baseMesh.colors;
            newMesh.tangents = baseMesh.tangents;
            newMesh.boneWeights = baseMesh.boneWeights;
            newMesh.bindposes = baseMesh.bindposes;

            // process each submesh (material)
            for (int subMeshID = 0; subMeshID < baseMesh.subMeshCount; subMeshID++) {
                List<Triangle> headTriangles = new List<Triangle>();
                List<Triangle> leftArmTriangles = new List<Triangle>();
                List<Triangle> rightArmTriangles = new List<Triangle>();
                List<Triangle> leftLegTriangles = new List<Triangle>();
                List<Triangle> rightLegTriangles = new List<Triangle>();
                List<Triangle> bodyTriangles = new List<Triangle>();
                // collect triangles for submesh
                int[] triangles = baseMesh.GetTriangles(subMeshID);
                Triangle tri = new Triangle();
                int index = 0;
                // go through each triangle in submesh
                while (index < triangles.Length) {
                    tri = new Triangle(triangles, index);
                    index += 3;
                    // check triangle for connection to head or arms
                    // add to appropriate triangle list
                    int[] belongsTo = Belongs(settings, tri, baseMesh, boneDict, boneWeights);
                    if (belongsTo == headBoneIndices) {
                        headTriangles.Add(tri);
                    } else if (belongsTo == leftArmBoneIndices) {
                        leftArmTriangles.Add(tri);
                    } else if (belongsTo == rightArmBoneIndices) {
                        rightArmTriangles.Add(tri);
                    } else if (belongsTo == leftLegBoneIndices) {
                        leftLegTriangles.Add(tri);
                    } else if (belongsTo == rightLegBoneIndices) {
                        rightLegTriangles.Add(tri);
                    } else {
                        bodyTriangles.Add(tri);
                    }
                }

                var materials = context.Materials;
                var materialNames = context.MaterialNames;
                if (bodyTriangles.Count > 0 && settings.include.body) {
                    AddSubMesh(subMeshes, mats, bodyTriangles.ToArray(), bodyMat);
                    AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID, "body");
                }
                if (headTriangles.Count > 0 && settings.include.head) {
                    AddSubMesh(subMeshes, mats, headTriangles.ToArray(), headMat);
                    AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID, "head");
                }
                if (settings.separateArms) {
                    if (settings.include.leftArm) {
                        if (leftArmTriangles.Count > 0) {
                            AddSubMesh(subMeshes, mats, leftArmTriangles.ToArray(), armsMat);
                            AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID,
                                "left arm");
                        }
                    }
                    if (settings.include.rightArm) {
                        if (rightArmTriangles.Count > 0) {
                            AddSubMesh(subMeshes, mats, rightArmTriangles.ToArray(), armsMat);
                            AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID,
                                "right arm");
                        }
                    }
                } else {
                    List<Triangle> armTris = new List<Triangle>();
                    if (settings.include.leftArm) {
                        armTris.AddRange(leftArmTriangles);
                    }
                    if (settings.include.rightArm) {
                        armTris.AddRange(rightArmTriangles);
                    }
                    if (armTris.Count > 0) {
                        AddSubMesh(subMeshes, mats, armTris.ToArray(), armsMat);
                        AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID, "arms");
                    }
                }
                if (settings.separateLegs) {
                    if (settings.include.leftLeg) {
                        if (leftLegTriangles.Count > 0) {
                            AddSubMesh(subMeshes, mats, leftLegTriangles.ToArray(), legsMat);
                            AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID,
                                "left leg");
                        }
                    }
                    if (settings.include.rightLeg) {
                        if (rightLegTriangles.Count > 0) {
                            AddSubMesh(subMeshes, mats, rightLegTriangles.ToArray(), legsMat);
                            AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID,
                                "right leg");
                        }
                    }
                } else {
                    List<Triangle> legTris = new List<Triangle>();
                    if (settings.include.leftLeg) {
                        legTris.AddRange(leftLegTriangles);
                    }
                    if (settings.include.rightLeg) {
                        legTris.AddRange(rightLegTriangles);
                    }
                    if (legTris.Count > 0) {
                        AddSubMesh(subMeshes, mats, legTris.ToArray(), legsMat);
                        AddMaterial(materials, materialNames, sourceRenderer.sharedMaterials, subMeshID, "legs");
                    }
                }
            }
            context.AssignSubMeshes(targetRenderer, newMesh, subMeshes, mats);
            targetRenderer.sharedMesh = newMesh;
            targetRenderer.sharedMaterials = mats.ToArray();
        }

        private static void AssignSubMeshes(this FPSMeshContext context, SkinnedMeshRenderer meshRenderer, Mesh mesh,
            List<int[]> subMeshes, List<Material> mats) {
            Undo.RecordObject(meshRenderer, "Updated mesh");
            mesh.subMeshCount = subMeshes.Count;
            for (int i = 0; i < mesh.subMeshCount; i++) {
                mesh.SetTriangles(subMeshes[i], i);
            }
            meshRenderer.sharedMaterials = mats.ToArray();
        }

        private static void AddSubMesh(List<int[]> subMeshes, List<Material> mats, Triangle[] tris, Material mat) {
            subMeshes.Add(Flatten(tris));
            mats.Add(mat);
        }

        private static T[] Add<T>(T item, T[] array) {
            T[] newArray = new T[array.Length + 1];
            array.CopyTo(newArray, 0);
            newArray[array.Length] = item;
            return newArray;
        }

        private static int[] Belongs(MeshSettings settings, Triangle tri, Mesh mesh, Dictionary<int, int[]> boneDict,
            BoneWeight[] boneWeights) {
            Dictionary<int[], float> limbWeights = new Dictionary<int[], float>();
            Dictionary<int[], int> limbVerts = new Dictionary<int[], int>();
            float minWeight = Mathf.Max(.00001f, settings.weightThreshold);
            int minVerts = settings.vertexThreshold;
            foreach (int vert in tri.verts) {
                var weights = boneWeights[vert];
                // check all four bone weights for a nonzero value matching anything in the boneDict
                ProcessBoneWeight(weights.boneIndex0, weights.weight0);
                ProcessBoneWeight(weights.boneIndex1, weights.weight1);
                ProcessBoneWeight(weights.boneIndex2, weights.weight2);
                ProcessBoneWeight(weights.boneIndex3, weights.weight3);
                foreach (int[] limb in limbWeights.Keys) {
                    if (limbWeights[limb] >= minWeight) {
                        limbVerts.TryGetValue(limb, out var vertCount);
                        limbVerts[limb] = vertCount + 1;
                    }
                }
            }
            foreach (var limb in limbVerts.Keys) {
                if (limbVerts[limb] >= minVerts) {
                    return limb;
                }
            }
            return null;

            void ProcessBoneWeight(int bone, float weight) {
                if (weight > 0 && boneDict.ContainsKey(bone)) {
                    var limb = boneDict[bone];
                    limbWeights.TryGetValue(limb, out var prevWeight);
                    limbWeights[limb] = weight + prevWeight;
                }
            }
        }

        private static int[] Flatten(Triangle[] sourceTris) {
            List<int> tris = new List<int>();
            foreach (Triangle t in sourceTris) {
                tris.Add(t[0]);
                tris.Add(t[1]);
                tris.Add(t[2]);
            }
            return tris.ToArray();
        }

        private static Dictionary<int, int[]> GetBoneDict(int[] headBoneIndices, int[] leftArmBoneIndices,
            int[] rightArmBoneIndices, int[] leftLegBoneIndices, int[] rightLegBoneIndices) {
            Dictionary<int, int[]> boneDict = new Dictionary<int, int[]>();
            foreach (int bone in headBoneIndices) {
                boneDict[bone] = headBoneIndices;
            }
            foreach (int bone in leftArmBoneIndices) {
                boneDict[bone] = leftArmBoneIndices;
            }
            foreach (int bone in rightArmBoneIndices) {
                boneDict[bone] = rightArmBoneIndices;
            }
            foreach (int bone in leftLegBoneIndices) {
                boneDict[bone] = leftLegBoneIndices;
            }
            foreach (int bone in rightLegBoneIndices) {
                boneDict[bone] = rightLegBoneIndices;
            }
            return boneDict;
        }

        private static int[] GetBoneIndices(Transform[] bones, Transform[] bonesMaster) {
            List<int> boneIndices = new List<int>();
            foreach (Transform bone in bones) {
                boneIndices.Add(System.Array.IndexOf(bonesMaster, bone));
            }
            return boneIndices.ToArray();
        }

        private static Transform[] GetBones(Transform rootBone, Transform[] bones, BodyPart bodyPart,
            List<BoneOverride> boneOverrides) {
            // Collect all bones in the hierarchy
            List<Transform> subBoneList = new List<Transform>();
            if (rootBone != null) {
                foreach (var child in rootBone.GetComponentsInChildren<Transform>()) {
                    if (IsIn(child, bones)) {
                        subBoneList.Add(child);
                    }
                }
            }
            // Apply bone overrides
            if (boneOverrides != null) {
                foreach (var boneOverride in boneOverrides) {
                    if (boneOverride.bodyPart == bodyPart) {
                        // Add any missing bones
                        if (!subBoneList.Contains(boneOverride.bone)) {
                            subBoneList.Add(boneOverride.bone);
                        }
                    } else {
                        // Remove any banned bones
                        subBoneList.Remove(boneOverride.bone);
                    }
                }
            }
            return subBoneList.ToArray();
        }
        
        private static bool IsIn<T>(T lhs, T[] rhs) {
            return (Array.IndexOf(rhs, lhs) != -1);
        }

        public static void GuessBones(this FPSMeshContext context) {
            if (context.ValidObject && context.ValidRenderers) {
                context.Bones.root = context.GetRootBone();
                if (context.ValidRootBone) {
                    if (context.ValidAnimator) {
                        context.Bones.head = context.animator.GetBoneTransform(HumanBodyBones.Head);
                        context.Bones.leftArm = context.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                        context.Bones.rightArm = context.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                        context.Bones.leftLeg = context.animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                        context.Bones.rightLeg = context.animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                    }
                    if (!context.ValidBones) {
                        var root = context.Bones.root;
                        context.Bones.head = GetBoneMatch(root, headPatterns);
                        context.Bones.leftArm = GetBoneMatch(root, leftArmPatterns);
                        context.Bones.rightArm = GetBoneMatch(root, rightArmPatterns);
                        context.Bones.leftLeg = GetBoneMatch(root, leftLegPatterns);
                        context.Bones.rightLeg = GetBoneMatch(root, rightLegPatterns);
                    }
                } else {
                    context.Bones = new Bones();
                }
            } else {
                context.Bones = new Bones();
            }
            if (!context.ValidBones) Debug.LogError($"Unable to determine bones for {context.SourceObject}", context.SourceObject);
        }

        public static Transform GetRootBone(this FPSMeshContext context) {
            Transform rootCandidate = null;
            if (context.ValidAnimator) {
                rootCandidate = context.animator.GetBoneTransform(HumanBodyBones.Hips);
            }
            return GetRoot(rootCandidate, GetRootBone(context.SourceRenderers));
        }

        private static Transform GetRootBone(RendererEntry[] renderers) {
            Transform rootBone = null;
            foreach (var entry in renderers) {
                rootBone = GetRoot(rootBone, entry.renderer.rootBone);
            }
            return rootBone;
        }

        private static Transform GetRoot(Transform t1, Transform t2) {
            if (t1 == null) return t2;
            if (t2 == null) return t1;
            return t1.IsChildOf(t2) ? t2 : t1;
        }

        private static Transform GetBoneMatch(Transform bone, string pattern) {
            Transform boneMatch = null;
            if (System.Text.RegularExpressions.Regex.IsMatch(bone.name, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                return bone;
            }
            foreach (Transform child in bone) {
                boneMatch = GetBoneMatch(child, pattern);
                if (boneMatch != null) {
                    return boneMatch;
                }
            }
            return boneMatch;
        }

        private static Transform GetBoneMatch(Transform bone, string[] patterns) {
            Transform boneMatch = null;
            foreach (string pattern in patterns) {
                boneMatch = GetBoneMatch(bone, pattern);
                if (boneMatch != null) {
                    return boneMatch;
                }
            }
            return boneMatch;
        }

        private static void AddMaterial(List<Material> materials, List<string> materialNames, Material[] mats,
            int matIndex,
            string suffix) {
            Material mat = (mats != null && mats.Length > matIndex) ? mats[matIndex] : null;
            if (mat == null) {
                mat = new Material(Shader.Find("Diffuse"));
                mat.name = "Placeholder";
            }
            materials.Add(mat);
            materialNames.Add(mat.name + " " + suffix);
        }

        public static void DestroyWorkingObjects(FPSMeshContext context) {
            if (!context.IsPrefab) Undo.DestroyObjectImmediate(context.SourceObject);
            if (context.targetObject && !context.targetObject.IsPrefab())
                Undo.DestroyObjectImmediate(context.targetObject);
        }

        public static GameObject GetValidSourceObject(GameObject sourceObject) {
            if (!EditorUtility.IsPersistent(sourceObject)) return sourceObject;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceObject);
            Undo.RegisterCreatedObjectUndo(instance, $"Created {instance}");
            return instance;
        }

        public static GameObject CreateChildObject(this GameObject parent, string childName) {
            var childObject = new GameObject(childName);
            childObject.transform.SetParent(parent.transform, worldPositionStays:false);
            return childObject;
        }

        public static bool IsPrefab(this GameObject gameObject) =>
            EditorUtility.IsPersistent(gameObject) || PrefabStageUtility.GetPrefabStage(gameObject);
    }
    
}
#endif