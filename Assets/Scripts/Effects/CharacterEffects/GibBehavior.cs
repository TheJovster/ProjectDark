using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GibBehavior : MonoBehaviour
{
    [SerializeField] private float _fadeStartTime = 8.0f;
    [SerializeField] private bool _leaveBloodDecal = true;
    [SerializeField] private GameObject _bloodDecalPrefab;
    [SerializeField] private ParticleSystem _bloodParticleSystem;
    [SerializeField] private float _bloodEmissionDuration = 2.0f;
    [SerializeField] private float _minImpactForceForSound = 2.0f;
    [SerializeField] private AudioClip[] _impactSounds;
    [SerializeField] private AudioClip[] _fleshSounds;
    
    private Rigidbody _rigidbody;
    private Renderer[] _renderers;
    private bool _hasCollided = false;
    private bool _isFading = false;
    private float _lifetime;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        // Get lifetime from object's Destroy call or use default
        float destroyTime = 0f;
        foreach (var mono in GetComponents<MonoBehaviour>())
        {
            System.Reflection.FieldInfo fieldInfo = mono.GetType().GetField("_gibLifetime");
            if (fieldInfo != null && fieldInfo.FieldType == typeof(float))
            {
                destroyTime = (float)fieldInfo.GetValue(mono);
                break;
            }
        }
        _lifetime = destroyTime > 0 ? destroyTime : 10f;
        
        // Start blood particle system if it exists
        if (_bloodParticleSystem != null)
        {
            _bloodParticleSystem.Play();
            StartCoroutine(StopBloodEmission());
        }
        else if (TryGetComponent<ParticleSystem>(out var particleSystem))
        {
            // Use any particle system on this object if none specified
            _bloodParticleSystem = particleSystem;
            _bloodParticleSystem.Play();
            StartCoroutine(StopBloodEmission());
        }
        
        //Play initial flesh sound - use Audio Manager
        
        // Start fade coroutine if needed
        if (_fadeStartTime < _lifetime)
        {
            StartCoroutine(FadeOut());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_hasCollided && collision.relativeVelocity.magnitude > _minImpactForceForSound)
        {
            _hasCollided = true;
            //play impac sound
            
            if (_leaveBloodDecal && _bloodDecalPrefab != null)
            {
                LeaveBloodDecal(collision);
            }
        }
    }

    private void LeaveBloodDecal(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        
        // Check if we hit a valid surface for blood (not another gib)
        if (collision.gameObject.GetComponent<GibBehavior>() != null)
        {
            return;
        }
        
        // Create rotation aligned with surface
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
        
        // Add slight offset to prevent z-fighting
        Vector3 position = contact.point + contact.normal * 0.01f;
        
        // Create the decal
        GameObject bloodDecal = Instantiate(_bloodDecalPrefab, position, rotation);
        
        // Add random rotation around normal
        bloodDecal.transform.rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);
        
        // Random scale variation
        float scaleVar = Random.Range(0.8f, 1.2f);
        bloodDecal.transform.localScale *= scaleVar;
        
        // Parent to the hit object if possible
        if (collision.transform.gameObject.isStatic)
        {
            bloodDecal.transform.parent = collision.transform;
        }
        
        // Destroy after some time
        Destroy(bloodDecal, 30f);
    }

    
    private IEnumerator StopBloodEmission()
    {
        yield return new WaitForSeconds(_bloodEmissionDuration);
        
        if (_bloodParticleSystem != null)
        {
            var emission = _bloodParticleSystem.emission;
            emission.enabled = false;
        }
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(_fadeStartTime);
        
        _isFading = true;
        float fadeDuration = _lifetime - _fadeStartTime;
        float elapsedTime = 0f;
        
        // Store original materials and their colors
        Material[] originalMaterials = new Material[_renderers.Length];
        Color[] originalColors = new Color[_renderers.Length];
        
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                // Create a material instance to avoid affecting shared materials
                _renderers[i].material = new Material(_renderers[i].material);
                originalMaterials[i] = _renderers[i].material;
                originalColors[i] = _renderers[i].material.color;
            }
        }
        
        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && originalMaterials[i] != null)
                {
                    // Check if the shader supports transparency
                    if (originalMaterials[i].HasProperty("_Color"))
                    {
                        Color color = originalColors[i];
                        color.a = alpha;
                        originalMaterials[i].color = color;
                    }
                    
                    // Handle common fade modes
                    if (originalMaterials[i].HasProperty("_Mode"))
                    {
                        originalMaterials[i].SetFloat("_Mode", 2); // Fade mode
                        originalMaterials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        originalMaterials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        originalMaterials[i].SetInt("_ZWrite", 0);
                        originalMaterials[i].DisableKeyword("_ALPHATEST_ON");
                        originalMaterials[i].EnableKeyword("_ALPHABLEND_ON");
                        originalMaterials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        originalMaterials[i].renderQueue = 3000;
                    }
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Add some randomness to the physics
    private void FixedUpdate()
    {
        // Occasionally add small random torque for more natural movement
        if (Random.value < 0.05f && _rigidbody != null)
        {
            Vector3 randomTorque = Random.insideUnitSphere * 0.2f;
            _rigidbody.AddTorque(randomTorque, ForceMode.Impulse);
        }
    }
    
    // Allow external scripts to trigger effects
    public void SpawnBloodEffect(Vector3 position)
    {
        if (_bloodDecalPrefab != null)
        {
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            Instantiate(_bloodDecalPrefab, position, rotation);
        }
    }
    
}
