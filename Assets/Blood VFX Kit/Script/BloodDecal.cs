using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Biostart.Blood
{
    public class BloodDecal : MonoBehaviour
    {
        public List<GameObject> decalPrefabs; 
        public bool randomRotation = true; 
        public float minScale = 1f;  
        public float maxScale = 1.5f;
        public float destroyDelay = 20f;
        public string[] targetTags; 
        public float spawnChance = 0.7f; 
        public float decalOffset = 0.005f; 

        private ParticleSystem system;
        private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); 

        void Awake()
        {
            system = GetComponent<ParticleSystem>();
        }

        private void OnParticleCollision(GameObject other)
        {
            foreach (string tag in targetTags)
            {
                if (other.CompareTag(tag))
                {
                    int numCollisionEvents = system.GetCollisionEvents(other, collisionEvents); 

                    for (int i = 0; i < numCollisionEvents; i++)
                    {
                        if (Random.value > spawnChance)
                            continue;
                        
                        Vector3 pos = collisionEvents[i].intersection;
                        Vector3 normal = collisionEvents[i].normal;
                        
                        Vector3 upVector = Vector3.up;
                        if (Mathf.Abs(Vector3.Dot(normal, upVector)) > 0.99f)
                        {
                            upVector = Vector3.forward;
                        }
                        
                        Quaternion rotation;
                        if (randomRotation)
                        {
                            Quaternion baseRotation = Quaternion.LookRotation(-normal, upVector);
                            rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * baseRotation;
                        }
                        else
                        {
                            rotation = Quaternion.LookRotation(-normal, upVector);
                        }
                        
                        GameObject decalPrefab = decalPrefabs[Random.Range(0, decalPrefabs.Count)];
                        
                        GameObject decal = Instantiate(decalPrefab, pos + normal * decalOffset, rotation);
                        decal.transform.localScale *= Random.Range(minScale, maxScale); 
                    }
                    break;
                }
            }
        }
    }
}