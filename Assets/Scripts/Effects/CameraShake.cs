using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using UnityEngine.Serialization;

public class CameraShake : MonoBehaviour
{
    [FormerlySerializedAs("shakeDuration")]
    [Header("Shake Parameters")]
    [SerializeField] private float _shakeDuration = 0.2f;
    [SerializeField] private float _shakeIntensity = 0.2f;
    [SerializeField] private float _noiseFrequency = 15.0f;
    
    [Header("Volume Settings")]
    [SerializeField] private Volume _postProcessVolume;
    
    // Internal variables
    private float _shakeTimer = 0f;
    private bool _isShaking = false;
    private Vector3 _originalPosition;
    private LensDistortion _lensDistortion;
    private ChromaticAberration _chromaticAberration;
    
    // Cached values
    private float _originalDistortionIntensity = 0f;
    private float _originalChromaticIntensity = 0f;
    
    private void Start()
    {
        if (_postProcessVolume == null)
        {
            _postProcessVolume = FindObjectOfType<Volume>();
            if (_postProcessVolume == null)
            {
                Debug.LogWarning("No Post Process Volume found in scene. Creating one for camera shake effects.");
                GameObject volumeObj = new GameObject("Camera Shake Post Process Volume");
                _postProcessVolume = volumeObj.AddComponent<Volume>();
                _postProcessVolume.isGlobal = true;
                
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _postProcessVolume.profile = profile;
                
                profile.Add<LensDistortion>(true);
                profile.Add<ChromaticAberration>(true);
            }
        }
        
        if (_postProcessVolume.profile.TryGet(out LensDistortion distortion))
        {
            _lensDistortion = distortion;
            if (_lensDistortion.active)
                _originalDistortionIntensity = _lensDistortion.intensity.value;
        }
        
        if (_postProcessVolume.profile.TryGet(out ChromaticAberration chromatic))
        {
            _chromaticAberration = chromatic;
            if (_chromaticAberration.active)
                _originalChromaticIntensity = _chromaticAberration.intensity.value;
        }
        
        _originalPosition = transform.localPosition;
    }
    
    private void Update()
    {
        if (_isShaking)
        {
            _shakeTimer -= Time.deltaTime;
            
            float progress = Mathf.Clamp01(1f - (_shakeTimer / _shakeDuration));
            float damper = 1.0f - progress;
            
            ApplyPositionalShake(damper);
            ApplyPostProcessingEffects(damper);
            
            if (_shakeTimer <= 0)
            {
                _isShaking = false;
                ResetEffects();
            }
        }
    }
    
    private void ApplyPositionalShake(float damper)
    {
        float offsetX = (Mathf.PerlinNoise(Time.time * _noiseFrequency, 0) - 0.5f) * _shakeIntensity * damper * 0.1f;
        float offsetY = (Mathf.PerlinNoise(0, Time.time * _noiseFrequency) - 0.5f) * _shakeIntensity * damper * 0.1f;
        
        transform.localPosition = _originalPosition + new Vector3(offsetX, offsetY, 0);
    }
    
    private void ApplyPostProcessingEffects(float damper)
    {
        if (_lensDistortion != null)
        {
            float distortionNoise = Mathf.PerlinNoise(Time.time * _noiseFrequency * 1.25f, 0.5f) - 0.5f;
            _lensDistortion.active = true;
            _lensDistortion.intensity.Override(distortionNoise * _shakeIntensity * damper * 0.3f);
        }
        
        if (_chromaticAberration != null)
        {
            _chromaticAberration.active = true;
            _chromaticAberration.intensity.Override(_originalChromaticIntensity + (_shakeIntensity * damper * 0.2f));
        }
    }
    
    private void ResetEffects()
    {
        transform.localPosition = _originalPosition;
        
        if (_lensDistortion != null)
        {
            _lensDistortion.intensity.Override(_originalDistortionIntensity);
        }
        
        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.Override(_originalChromaticIntensity);
        }
    }
    
    public void ShakeCamera()
    {
        _shakeTimer = _shakeDuration;
        _isShaking = true;
    }
    
    public void ShakeCamera(float duration, float intensity)
    {
        _shakeDuration = duration;
        _shakeIntensity = intensity;
        _shakeTimer = duration;
        _isShaking = true;
    }
    
    public void TriggerFireShake(float weaponRecoil)
    {
        float recoilDuration = 0.1f + (weaponRecoil * 0.05f);
        float recoilIntensity = weaponRecoil;
        
        ShakeCamera(recoilDuration, recoilIntensity);
    }
}

