using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Stats))]
public class ProceduralGibbing : MonoBehaviour
{
    [Serializable]
    public class GibPart
    {
        public string Name;
        public Transform BoneRoot;
        public Transform[] BonesToInclude; // Optional - if empty, will use all child bones of BoneRoot
        [Range(0f, 1f)] public float GibChance = 0.5f;
        [HideInInspector] public bool IsGibbed = false;
    }

    [Header("Gibbing Settings")] [SerializeField]
    private bool _gibEnabled = true;

    [SerializeField] private float _gibChanceMultiplier = 1.0f;
    [SerializeField] private float _gibForceMin = 5.0f;
    [SerializeField] private float _gibForceMax = 10.0f;
    [SerializeField] private float _gibTorqueMin = 2.0f;
    [SerializeField] private float _gibTorqueMax = 5.0f;
    [SerializeField] private float _gibLifetime = 10.0f;
    [SerializeField] private Material _gibMaterial; // Optional override material
    [SerializeField] private GameObject _bloodEffectPrefab;
    [SerializeField] private float _bloodEffectDuration = 3.0f;

    [Header("Gib Physics")] [SerializeField]
    private float _gibMass = 3.0f;

    [SerializeField] private float _gibDrag = 0.5f;
    [SerializeField] private float _gibAngularDrag = 0.2f;
    [SerializeField] private PhysicsMaterial _gibPhysicsMaterial;

    [Header("Mesh Settings")] [SerializeField]
    private SkinnedMeshRenderer _characterMesh;
    private List<Mesh> _modifiedMeshes = new List<Mesh>();
    [SerializeField] private bool _createCutFaces = true;
    [SerializeField] private Material _cutFaceMaterial;
    [SerializeField] private Color _cutFaceColor = Color.red;

    [Header("Body Parts")] [SerializeField]
    private List<GibPart> _gibParts = new List<GibPart>();

    
    [Header("Debug")] [SerializeField] private bool _debugGibbing = false;
    [SerializeField] private bool _drawGizmosForParts = false;

    // References
    private Stats _stats;
    private AIAgent _aiAgent;
    private Animator _animator;

    // Internal state
    private Dictionary<string, BoneWeight[]> _originalBoneWeights = new Dictionary<string, BoneWeight[]>();
    private Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();
    private bool _processingDeath = false;
    private List<GameObject> _spawnedGibs = new List<GameObject>();

    private void Awake()
    {
        _stats = GetComponent<Stats>();
        _aiAgent = GetComponent<AIAgent>();
        _animator = GetComponentInChildren<Animator>();

        if (_characterMesh == null)
        {
            _characterMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        if (_characterMesh == null)
        {
            Debug.LogError("ProceduralGibbing: No SkinnedMeshRenderer found!");
            enabled = false;
            return;
        }

        // Cache original bone weights
        CacheOriginalBoneWeights();

        // Map bones by name for faster lookups
        MapBones();
    }

    private void Start()
    {
        // No event subscription needed - we'll check stats in Update
    }

    private void Update()
    {
        // Check if character just died and we haven't processed it yet
        if (!_processingDeath && _stats != null && !_stats.IsAlive)
        {
            _processingDeath = true;
            ProcessDeath();
        }
    }

    private void CacheOriginalBoneWeights()
    {
        _originalBoneWeights.Clear();

        // Cache the original mesh data
        Mesh sharedMesh = _characterMesh.sharedMesh;
        if (sharedMesh != null)
        {
            _originalBoneWeights["original"] = sharedMesh.boneWeights;
        }
    }

    private void MapBones()
    {
        _boneMap.Clear();
        Transform[] bones = _characterMesh.bones;

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null)
            {
                _boneMap[bones[i].name] = bones[i];
            }
        }
    }

    private void ProcessDeath()
    {
        if (_debugGibbing)
        {
            Debug.Log($"Processing death for {gameObject.name}");
        }

        if (!_gibEnabled || _characterMesh == null) return;

        // Process each body part for potential gibbing
        foreach (GibPart part in _gibParts)
        {
            if (part.BoneRoot == null) continue;

            float finalGibChance = part.GibChance * _gibChanceMultiplier;
            bool shouldGib = Random.value <= finalGibChance;

            if (shouldGib)
            {
                CreateGibFromBones(part);
            }
        }
    }

    private void CreateGibFromBones(GibPart part)
    {
        if (part.IsGibbed) return;

        part.IsGibbed = true;

        if (_debugGibbing)
        {
            Debug.Log($"Gibbing {part.Name} on {gameObject.name}");
        }

        // Get the position and rotation of the body part
        Vector3 position = part.BoneRoot.position;
        Quaternion rotation = part.BoneRoot.rotation;

        // Get all the bones to include in this gib
        List<Transform> includedBones = new List<Transform>();

        if (part.BonesToInclude != null && part.BonesToInclude.Length > 0)
        {
            // Use the specified bones
            includedBones.AddRange(part.BonesToInclude);
        }
        else
        {
            // Use all child bones of the root
            includedBones.Add(part.BoneRoot);
            GetAllChildBones(part.BoneRoot, includedBones);
        }

        // Get bone indices
        List<int> boneIndices = new List<int>();
        Transform[] meshBones = _characterMesh.bones;

        for (int i = 0; i < meshBones.Length; i++)
        {
            if (includedBones.Contains(meshBones[i]))
            {
                boneIndices.Add(i);
            }
        }

        if (boneIndices.Count == 0)
        {
            Debug.LogWarning($"No valid bones found for gib part {part.Name}");
            return;
        }

        // Create a new game object for the gib
        GameObject gibObject = new GameObject($"Gib_{part.Name}");
        gibObject.transform.position = position;
        gibObject.transform.rotation = rotation;

        // Create new skinned mesh for the gib
        SkinnedMeshRenderer gibRenderer = gibObject.AddComponent<SkinnedMeshRenderer>();

        // Create a copy of the original mesh
        Mesh originalMesh = _characterMesh.sharedMesh;
        Mesh gibMesh = ExtractGibMesh(originalMesh, boneIndices.ToArray());

        // Set up the new renderer
        gibRenderer.sharedMesh = gibMesh;
        gibRenderer.materials = _gibMaterial != null
            ? new Material[] { _gibMaterial }
            : _characterMesh.materials;

        
        // Set up bones for the new mesh
        Transform[] gibBones = CloneBoneHierarchy(includedBones, gibObject.transform);
        gibRenderer.bones = gibBones;
        // Set up bones for the new mesh
        /*Transform[] gibBones = new Transform[meshBones.Length];
        for (int i = 0; i < meshBones.Length; i++)
        {
            gibBones[i] = meshBones[i];
        }*/

        gibRenderer.bones = gibBones;
        gibRenderer.rootBone = part.BoneRoot;

        // Add physics
        Rigidbody gibRigidbody = gibObject.AddComponent<Rigidbody>();
        gibRigidbody.mass = _gibMass;
        gibRigidbody.linearDamping = _gibDrag;
        gibRigidbody.angularDamping = _gibAngularDrag;

        // Add collider based on mesh bounds
        // This is a simple approach; might need more complex colliders for accuracy
        BoxCollider gibCollider = gibObject.AddComponent<BoxCollider>();
        gibCollider.center = gibMesh.bounds.center;
        gibCollider.size = gibMesh.bounds.size;

        // Set physics material if specified
        if (_gibPhysicsMaterial != null)
        {
            gibCollider.material = _gibPhysicsMaterial;
        }

        // Apply random force and torque
        Vector3 forceDirection = Random.onUnitSphere;
        float forceMagnitude = Random.Range(_gibForceMin, _gibForceMax);
        gibRigidbody.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);

        Vector3 torqueDirection = Random.onUnitSphere;
        float torqueMagnitude = Random.Range(_gibTorqueMin, _gibTorqueMax);
        gibRigidbody.AddTorque(torqueDirection * torqueMagnitude, ForceMode.Impulse);

        // Add a gib behavior component to handle effects
        GibBehavior gibBehavior = gibObject.AddComponent<GibBehavior>();

        // Spawn blood effect
        if (_bloodEffectPrefab != null)
        {
            GameObject bloodEffect = Instantiate(_bloodEffectPrefab, position, Quaternion.identity);
            Destroy(bloodEffect, _bloodEffectDuration);
        }

        // Hide the original bones
        foreach (Transform bone in includedBones)
        {
            // We can't disable the bone GameObject directly as it would affect the skeleton
            // Instead, we'll hide any mesh renderers attached to the bone
            if (bone.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.enabled = false;
            }

            // And we'll hide any child objects that aren't other bones
            foreach (Transform child in bone)
            {
                if (!_boneMap.ContainsValue(child)) // Not a bone used by the mesh
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // Track spawned gibs
        _spawnedGibs.Add(gibObject);

        // Set lifetime
        Destroy(gibObject, _gibLifetime);
    }
    
    private Mesh ExtractGibMesh(Mesh originalMesh, int[] boneIndices)
    {
        Mesh gibMesh = new Mesh();

        // Get the original mesh data
        Vector3[] vertices = originalMesh.vertices;
        Vector3[] normals = originalMesh.normals;
        Vector2[] uvs = originalMesh.uv;
        BoneWeight[] boneWeights = originalMesh.boneWeights;
        int[] triangles = originalMesh.triangles;

        // Create lists to hold the new mesh data
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<BoneWeight> newBoneWeights = new List<BoneWeight>();
        List<int> newTriangles = new List<int>();

        // Keep track of which vertices we're keeping and their new indices
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        // First pass: identify vertices influenced by the bones we want
        for (int i = 0; i < vertices.Length; i++)
        {
            BoneWeight bw = boneWeights[i];

            // Check if any of the bones influencing this vertex are in our list
            bool influenced = false;

            // Check the 4 bone influences
            if (bw.weight0 > 0 && Array.IndexOf(boneIndices, bw.boneIndex0) >= 0) influenced = true;
            if (bw.weight1 > 0 && Array.IndexOf(boneIndices, bw.boneIndex1) >= 0) influenced = true;
            if (bw.weight2 > 0 && Array.IndexOf(boneIndices, bw.boneIndex2) >= 0) influenced = true;
            if (bw.weight3 > 0 && Array.IndexOf(boneIndices, bw.boneIndex3) >= 0) influenced = true;

            // If this vertex is influenced by our bones, add it to the new mesh
            if (influenced)
            {
                int newIndex = newVertices.Count;
                vertexMap[i] = newIndex;

                newVertices.Add(vertices[i]);

                if (normals.Length > i) newNormals.Add(normals[i]);
                if (uvs.Length > i) newUVs.Add(uvs[i]);

                // Create a new bone weight that only includes the bones we want
                BoneWeight newBW = bw;

                // If the bone isn't in our list, zero out its influence
                if (Array.IndexOf(boneIndices, bw.boneIndex0) < 0) newBW.weight0 = 0;
                if (Array.IndexOf(boneIndices, bw.boneIndex1) < 0) newBW.weight1 = 0;
                if (Array.IndexOf(boneIndices, bw.boneIndex2) < 0) newBW.weight2 = 0;
                if (Array.IndexOf(boneIndices, bw.boneIndex3) < 0) newBW.weight3 = 0;

                // Normalize the weights
                float weightSum = newBW.weight0 + newBW.weight1 + newBW.weight2 + newBW.weight3;
                if (weightSum > 0)
                {
                    newBW.weight0 /= weightSum;
                    newBW.weight1 /= weightSum;
                    newBW.weight2 /= weightSum;
                    newBW.weight3 /= weightSum;
                }

                newBoneWeights.Add(newBW);
            }
        }

        // Second pass: identify triangles that use our selected vertices
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            // If all three vertices are in our map, add this triangle
            if (vertexMap.ContainsKey(a) && vertexMap.ContainsKey(b) && vertexMap.ContainsKey(c))
            {
                newTriangles.Add(vertexMap[a]);
                newTriangles.Add(vertexMap[b]);
                newTriangles.Add(vertexMap[c]);
            }
        }

        // Create the new mesh
        gibMesh.vertices = newVertices.ToArray();
        if (newNormals.Count > 0) gibMesh.normals = newNormals.ToArray();
        if (newUVs.Count > 0) gibMesh.uv = newUVs.ToArray();
        gibMesh.boneWeights = newBoneWeights.ToArray();
        gibMesh.triangles = newTriangles.ToArray();

        // Copy the bind poses
        gibMesh.bindposes = originalMesh.bindposes;

        // Create cut faces if needed
        if (_createCutFaces && _cutFaceMaterial != null)
        {
            // This would require identifying the boundary edges and creating a new mesh
            // For simplicity, we're not implementing full cut face generation here
        }

        gibMesh.RecalculateBounds();
        gibMesh.RecalculateNormals();

        return gibMesh;
    }

    private void GetAllChildBones(Transform bone, List<Transform> result)
    {
        foreach (Transform child in bone)
        {
            if (_boneMap.ContainsValue(child)) // Only include if it's a bone used by the mesh
            {
                result.Add(child);
                GetAllChildBones(child, result);
            }
        }
    }

    // Public methods for external control
    public void SetGibEnabled(bool enabled)
    {
        _gibEnabled = enabled;
    }

    public void SetGibChanceMultiplier(float multiplier)
    {
        _gibChanceMultiplier = Mathf.Max(0, multiplier);
    }

    public void ForceGib(string partName)
    {
        GibPart part = _gibParts.Find(p => p.Name == partName);
        if (part != null && !part.IsGibbed)
        {
            CreateGibFromBones(part);
        }
    }

    public void ForceGibAll()
    {
        foreach (GibPart part in _gibParts)
        {
            if (!part.IsGibbed && part.BoneRoot != null)
            {
                CreateGibFromBones(part);
            }
        }
    }

    // Helper methods
    public bool IsPartGibbed(string partName)
    {
        GibPart part = _gibParts.Find(p => p.Name == partName);
        return part != null && part.IsGibbed;
    }
    
    private Transform[] CloneBoneHierarchy(List<Transform> originalBones, Transform gibParent)
    {
        Dictionary<Transform, Transform> boneMap = new Dictionary<Transform, Transform>();
        Transform[] meshBones = _characterMesh.bones;
        Transform[] newBones = new Transform[meshBones.Length];
    
        // First, create clones of all the bones we need
        foreach (Transform bone in originalBones)
        {
            GameObject boneClone = new GameObject(bone.name + "_clone");
            boneClone.transform.SetParent(gibParent);
            boneClone.transform.position = bone.position;
            boneClone.transform.rotation = bone.rotation;
            boneClone.transform.localScale = bone.localScale;
        
            boneMap[bone] = boneClone.transform;
        }
    
        // Then map them to the bone array
        for (int i = 0; i < meshBones.Length; i++)
        {
            if (boneMap.TryGetValue(meshBones[i], out Transform mappedBone))
            {
                newBones[i] = mappedBone;
            }
            else
            {
                newBones[i] = meshBones[i];
            }
        }
    
        return newBones;
    }

    public void CleanupAllGibs()
    {
        foreach (GameObject gib in _spawnedGibs)
        {
            if (gib != null)
            {
                Destroy(gib);
            }
        }

        _spawnedGibs.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmosForParts) return;

        foreach (GibPart part in _gibParts)
        {
            if (part.BoneRoot != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(part.BoneRoot.position, 0.05f);

                if (part.BonesToInclude != null)
                {
                    Gizmos.color = Color.yellow;
                    foreach (Transform bone in part.BonesToInclude)
                    {
                        if (bone != null)
                        {
                            Gizmos.DrawSphere(bone.position, 0.03f);
                            if (part.BoneRoot != null)
                            {
                                Gizmos.DrawLine(part.BoneRoot.position, bone.position);
                            }
                        }
                    }
                }
            }
        }
    }
}