using UnityEngine;
using System.Collections.Generic;

public class ProceduralObjectSpawner : MonoBehaviour
{
    public List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();
    public SpawnArea spawnArea = new SpawnArea();
    public bool avoidOverlap = true;
    public float minDistanceBetweenObjects = 1.0f;
    public LayerMask collisionLayers;
    
    public bool showDebugGizmos = true;
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    private List<GameObject> spawnedObjects = new List<GameObject>();

    public void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
        
        spawnedObjects.Clear();
    }

    public void SpawnObjects()
    {
        if (spawnableObjects.Count == 0)
        {
            Debug.LogWarning("No spawnable objects defined!");
            return;
        }

        ClearSpawnedObjects();
        
        int attempts = 0;
        int maxAttempts = spawnArea.maxObjectsToSpawn * 5; // Limit attempts to prevent infinite loops
        
        while (spawnedObjects.Count < spawnArea.maxObjectsToSpawn && attempts < maxAttempts)
        {
            attempts++;
            
            // Randomly select a prefab based on probability
            SpawnableObject objectToSpawn = GetRandomSpawnableObject();
            if (objectToSpawn == null || objectToSpawn.prefab == null) continue;
            
            // Generate a random position within the spawn area
            Vector3 position = GetRandomPositionInArea();
            
            // Check for overlap if enabled
            if (avoidOverlap && IsPositionOverlapping(position, minDistanceBetweenObjects))
            {
                continue;
            }
            
            // Spawn the object with default rotation
            GameObject newObject = Instantiate(objectToSpawn.prefab, position, Quaternion.identity, transform);
            spawnedObjects.Add(newObject);
        }
        
        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("Reached maximum spawn attempts. Some objects may not have been spawned due to space constraints.");
        }
        
        Debug.Log($"Spawned {spawnedObjects.Count} objects out of {spawnArea.maxObjectsToSpawn} desired.");
    }
    
    private SpawnableObject GetRandomSpawnableObject()
    {
        float totalProbability = 0;
        foreach (var obj in spawnableObjects)
        {
            totalProbability += obj.spawnProbability;
        }
        
        float random = Random.Range(0, totalProbability);
        float currentProbability = 0;
        
        foreach (var obj in spawnableObjects)
        {
            currentProbability += obj.spawnProbability;
            if (random <= currentProbability)
            {
                return obj;
            }
        }
        
        return spawnableObjects[0];
    }
    
    private Vector3 GetRandomPositionInArea()
    {
        Vector3 center = transform.position + spawnArea.areaOffset;
        
        float x = Random.Range(-spawnArea.areaSize.x / 2, spawnArea.areaSize.x / 2);
        float z = Random.Range(-spawnArea.areaSize.z / 2, spawnArea.areaSize.z / 2);
        float y = Random.Range(0, spawnArea.areaSize.y);
        
        return center + new Vector3(x, y, z);
    }
    
    private bool IsPositionOverlapping(Vector3 position, float minDistance)
    {
        // Check against already spawned objects
        foreach (var obj in spawnedObjects)
        {
            if (Vector3.Distance(obj.transform.position, position) < minDistance)
            {
                return true;
            }
        }
        
        // Check against physics colliders
        Collider[] colliders = Physics.OverlapSphere(position, minDistance / 2, collisionLayers);
        return colliders.Length > 0;
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Gizmos.color = gizmoColor;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(spawnArea.areaOffset, spawnArea.areaSize);
        Gizmos.matrix = oldMatrix;
        
        // Draw spheres for all spawned objects
        Gizmos.color = Color.blue;
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Gizmos.DrawWireSphere(obj.transform.position, minDistanceBetweenObjects / 2);
            }
        }
    }
}
