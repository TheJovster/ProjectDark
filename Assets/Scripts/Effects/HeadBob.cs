using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HeadBob : MonoBehaviour
{
    [Header("Bob Parameters")]
    [SerializeField] private float _bobFrequency = 5f;
    [SerializeField] private float _bobHorizontalAmplitude = 0.1f;
    [SerializeField] private float _bobVerticalAmplitude = 0.1f;
    [SerializeField] private float _headBobSmoothing = 10f;
    
    [Header("Movement Detection")]
    [SerializeField] private bool _enableHeadBob = true;
    [SerializeField] private float _bobRequiredSpeed = 0.1f;
    [SerializeField] private Transform _velocitySource;
    
    [Header("Landing Parameters")]
    [SerializeField] private bool _enableLandingBob = true;
    [SerializeField] private float _landingBobAmplitude = 0.15f;
    [SerializeField] private float _landingBobDuration = 0.3f;
    [SerializeField] private float _landingBobMinVelocity = 5f;
    [SerializeField] private float _landingBobFraction = 20.0f;
    [SerializeField] private AnimationCurve _landingBobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Input System")]
    [SerializeField] private InputActionReference _movementAction;
    
    [Header("Debug")]
    [SerializeField] private bool _forceActiveBob = false;
    [SerializeField] private bool _debugLogs = false;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] _landingSounds;
    

    
    private Transform _playerTransform;
    private CharacterController _characterController;
    private PlayerController _playerController;
    private Vector3 _originalLocalPosition;
    private Vector3 _previousPosition;
    
    private float _bobCycle;
    private float _headBobFade;
    private Vector3 _targetCameraPosition;
    private Vector2 _movementInput;
    private bool _wasGrounded = true;
    private float _verticalVelocity = 0f;
    private bool _isLanding = false;
    private Vector3 _landingOffset = Vector3.zero;
    
    private void OnEnable()
    {
        if (_movementAction != null && _movementAction.action != null)
        {
            _movementAction.action.Enable();
        }
        
        if (_debugLogs)
        {
            Debug.Log("HeadBob script enabled. Make sure you've assigned a movement input action.");
        }
    }
    
    private void Start()
    {
        _playerTransform = _velocitySource != null ? _velocitySource : transform.parent;
        _characterController = _playerTransform.GetComponent<CharacterController>();
        _playerController = _playerTransform.GetComponent<PlayerController>();
        _originalLocalPosition = transform.localPosition;
        _previousPosition = _playerTransform.position;
        
        if (_debugLogs)
        {
            Debug.Log($"HeadBob initialized. Original position: {_originalLocalPosition}");
        }
    }
    
    private void Update()
    {
        if (!_enableHeadBob && !_forceActiveBob && !_enableLandingBob) return;
        
        if (_movementAction != null && _movementAction.action != null)
        {
            _movementInput = _movementAction.action.ReadValue<Vector2>();
        }
        
        Vector3 velocity = GetPlayerVelocity();
        _verticalVelocity = velocity.y;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        
        CheckForLanding();
        
        bool isActive = _forceActiveBob || currentSpeed > _bobRequiredSpeed || _movementInput.sqrMagnitude > 0.01f;
        
        _headBobFade = Mathf.Lerp(_headBobFade, isActive ? 1 : 0, Time.deltaTime * _headBobSmoothing);
        Vector3 movementBobOffset = CalculateMovementBob(_headBobFade);
        _targetCameraPosition = _originalLocalPosition + movementBobOffset + _landingOffset;
        
        transform.localPosition = Vector3.Lerp(transform.localPosition, _targetCameraPosition, Time.deltaTime * _headBobSmoothing);
    }
    
    private Vector3 CalculateMovementBob(float intensity)
    {
        if (intensity > 0.01f)
        {
            _bobCycle += Time.deltaTime * _bobFrequency;
        }
        
        float horizontalOffset = Mathf.Sin(_bobCycle) * _bobHorizontalAmplitude;
        float verticalOffset = Mathf.Cos(_bobCycle * 2f) * _bobVerticalAmplitude;
        
        return new Vector3(horizontalOffset, verticalOffset, 0) * intensity;
    }
    
    private void CheckForLanding()
    {
        if (!_enableLandingBob) return;
        
        bool isGrounded = IsGrounded();
        
        if (!_wasGrounded && isGrounded && !_isLanding)
        {
            float fallVelocity = Mathf.Abs(_verticalVelocity);
            
            if (fallVelocity > _landingBobMinVelocity)
            {
                float intensity = Mathf.Clamp01(fallVelocity / _landingBobFraction);
                float amplitude = _landingBobAmplitude * intensity;
                AudioManager.Instance.PlayEffect(_landingSounds[GetRandomLandingSound()]);
                StopAllCoroutines();
                StartCoroutine(LandingBobEffect(amplitude, _landingBobDuration));
                
                if (_debugLogs)
                {
                    Debug.Log($"Landing detected with velocity: {fallVelocity}, intensity: {intensity}");
                }
            }
        }
        
        _wasGrounded = isGrounded;
    }

    private int GetRandomLandingSound()
    {
        return Random.Range(0, _landingSounds.Length);
    }

    
    
    
    private IEnumerator LandingBobEffect(float amplitude, float duration)
    {
        _isLanding = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            float curveValue = _landingBobCurve.Evaluate(normalizedTime);
            
            float bobValue = amplitude * (1 - Mathf.Sin(normalizedTime * Mathf.PI)) * (1 - normalizedTime);
            
            _landingOffset = new Vector3(0, -bobValue, 0);
            
            yield return null;
        }
        
        _landingOffset = Vector3.zero;
        _isLanding = false;
    }
    
    private bool IsGrounded()
    {
        if (_characterController != null)
        {
            return _characterController.isGrounded;
        }
        
        return true;
    }
    
    private Vector3 GetPlayerVelocity()
    {
        if (_characterController != null)
        {
            return _characterController.velocity;
        }
        
        Vector3 currentPosition = _playerTransform.position;
        Vector3 velocity = (currentPosition - _previousPosition) / Time.deltaTime;
        _previousPosition = currentPosition;
        
        return velocity;
    }
}

