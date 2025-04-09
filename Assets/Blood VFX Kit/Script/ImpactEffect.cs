using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Biostart.Impact
{
    public class ImpactEffect : MonoBehaviour
    {
        public List<GameObject> bloodEffectPrefabs; 
        public float bloodEffectPrefabsDestroy = 5f; 
        public List<BloodEffectData> otherBloodEffectsData; 
        public bool destroy = false; 
        public bool disabled = false; 
        public List<GameObject> disabledObjects; 
        public float normalOffset = 0.01f; 

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider != null)
            {
                ContactPoint contact = collision.contacts[0];
                Vector3 hitPosition = contact.point;
                Vector3 hitNormal = contact.normal;
                SpawnBloodEffect(hitPosition, hitNormal);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null)
            {
                Vector3 hitPosition = other.ClosestPointOnBounds(transform.position);
                
                Vector3 hitNormal = (hitPosition - other.transform.position).normalized;
                
                if (hitNormal.magnitude < 0.01f)
                {
                    hitNormal = -other.transform.forward;
                }
                
                SpawnBloodEffect(hitPosition, hitNormal);
            }
        }

        public void SpawnBloodEffect(Vector3 position, Vector3 normal)
        {
            position = position + normal * normalOffset;
            Vector3 upVector = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(normal, upVector)) > 0.99f)
            {
                upVector = Vector3.forward; 
            }
            
            Quaternion rotation = Quaternion.LookRotation(normal, upVector);
            
            foreach (var bloodEffectPrefab in bloodEffectPrefabs)
            {
                if (bloodEffectPrefab != null)
                {
                    GameObject effect = Instantiate(bloodEffectPrefab, position, rotation);
                    Destroy(effect, bloodEffectPrefabsDestroy);
                }
            }
            
            foreach (var effectData in otherBloodEffectsData)
            {
                if (effectData.effect != null && effectData.positionObject != null)
                {
                    GameObject effect = Instantiate(effectData.effect, effectData.positionObject.transform);
                    
                    effect.transform.rotation = effectData.positionObject.transform.rotation;

                    Destroy(effect, effectData.destroyTime);
                }
                else
                {
                    Debug.LogWarning("Effect or position object is not assigned in the list!");
                }
            }
            
            if (disabled)
            {
                foreach (GameObject obj in disabledObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning("One of the disabled objects is null!");
                    }
                }
            }
            
            if (destroy)
            {
                Destroy(gameObject);
            }
        }
    }

    [System.Serializable]
    public struct BloodEffectData
    {
        public GameObject effect;             
        public GameObject positionObject;     
        public float destroyTime;             
    }
}