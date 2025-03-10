#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CBG.FPSMeshTool {
    [Serializable]
    public struct RendererEntry {
        public SkinnedMeshRenderer renderer;
        public bool useRenderer;
        public string name {
            get { return renderer ? renderer.name : string.Empty; }
        }

        public RendererEntry(SkinnedMeshRenderer renderer, bool useRenderer = true) {
            this.renderer = renderer;
            this.useRenderer = useRenderer;
        }
    }

    [Serializable]
    public struct Bones {
        public Transform root;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
    }

    public enum BodyPart {
        Head,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg
    }
    
    [Serializable]
    public struct BoneOverride {
        public Transform bone;
        public BodyPart bodyPart;
    }
    
    [Serializable]
    public class FPSMeshContext {
        public GameObject SourceObject;
        public RendererEntry[] SourceRenderers;
        public SkinnedMeshRenderer[] TargetRenderers;
        public List<Material> Materials;
        public List<string> MaterialNames;
        public Bones Bones;
        public MeshSettings Settings;
        
        public GameObject targetObject;
        public Animator animator;

        public string PrefabPath;
        public string MeshPath;
        public string MaterialPath;
        public bool ModifyOriginal;
        public List<BoneOverride> BoneOverrides;

        public bool ValidObject => SourceObject && !EditorUtility.IsPersistent(SourceObject);
        public bool IsPrefab => SourceObject != null &&
                (EditorUtility.IsPersistent(SourceObject) || PrefabStageUtility.GetPrefabStage(SourceObject));
        public bool ValidRenderers =>
            SourceRenderers != null && SourceRenderers.Length > 0 && SourceRenderers[0].renderer;
        public bool ValidRootBone => IsValidBone(SourceObject.transform, Bones.root);
        public bool ValidHeadBone => IsValidBone(Bones.root, Bones.head);
        public bool ValidLeftArmBone => IsValidBone(Bones.root, Bones.leftArm);
        public bool ValidRightArmBone => IsValidBone(Bones.root, Bones.rightArm);
        public bool ValidLeftLegBone => IsValidBone(Bones.root, Bones.leftLeg);
        public bool ValidRightLegBone => IsValidBone(Bones.root, Bones.rightLeg);
        public bool ValidBones =>
            ValidRootBone && ValidHeadBone && ValidLeftArmBone && ValidRightArmBone &&
            ((!Settings.processLegs) || (ValidLeftLegBone && ValidRightLegBone));
        public bool ValidAnimator => animator != null && animator.isHuman;
        public bool CanShowPreview => ValidObject && ValidRenderers && ValidBones;

        public FPSMeshContext(GameObject sourceObject, MeshSettings settings) {
            if (!sourceObject) return;
            SourceObject = FPSMeshTool.GetValidSourceObject(sourceObject);
            Settings = settings;
            SourceRenderers = SourceObject.GetRenderers().Select(r => new RendererEntry(r)).ToArray();
            animator = SourceObject.GetComponentInChildren<Animator>();
            this.GuessBones();
        }

        private bool IsValidBone(Transform rootBone, Transform trans) {
            return ValidRenderers && (trans != null) && (trans.IsChildOf(rootBone));
        }

        public void SetTargetObject(GameObject target) {
            targetObject = target;
            TargetRenderers = targetObject.GetRenderers();
        }
    }
}
#endif