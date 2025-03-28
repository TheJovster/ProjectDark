// This script goes on the Weapon Container object

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] private float _swayAmount = 0.02f;
    [SerializeField] private float _maxSwayAmount = 0.06f;
    [SerializeField] private float _swaySmoothing = 8f;
    [SerializeField] private float _swayResetSmoothing = 2f;
    
    [Header("Movement Sway")]
    [SerializeField] private float _movementSwayX = 0.01f;
    [SerializeField] private float _movementSwayY = 0.01f;
    [SerializeField] private float _movementSwaySmoothing = 10f;
    
    [Header("Input")]
    private InputSystem_Actions _playerInput;
    private InputAction _lookAction;
    
    // References
    private Transform _cameraTransform;
    private CharacterController _characterController;
    
    // Internal state
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _targetSwayPosition;
    private Quaternion _targetSwayRotation;
    private Vector2 _lookInput;
    private Vector3 _movementSway;
    private Vector3 _lastPosition;
    private Vector3 _velocity;

    private void OnEnable()
    {
        _playerInput = new InputSystem_Actions();
        _playerInput.Enable();
    }

    private void OnDisable()
    {
        _playerInput.Disable();
    }

    private void Start()
    {
        _initialPosition = transform.localPosition;
        _initialRotation = transform.localRotation;
        _cameraTransform = transform.parent;
        _characterController = GetComponentInParent<CharacterController>();
        _lastPosition = _cameraTransform.parent.position;
    }
    
    private void Update()
    {
        if (GameManager.Instance.CurrentGameState == GameManager.GameState.Playing)
        {
            _lookInput = _playerInput.Player.Look.ReadValue<Vector2>();
            CalculateVelocity();
            CalculateLookSway();
            CalculateMovementSway();
            ApplySway();
        }
        else return;
    }
    
    private void CalculateVelocity()
    {
        Vector3 currentPosition = _cameraTransform.parent.position;
        _velocity = (currentPosition - _lastPosition) / Time.deltaTime;
        _lastPosition = currentPosition;
    }
    
    private void CalculateLookSway()
    {
        float swayX = Mathf.Clamp(-_lookInput.x * _swayAmount, -_maxSwayAmount, _maxSwayAmount);
        float swayY = Mathf.Clamp(-_lookInput.y * _swayAmount, -_maxSwayAmount, _maxSwayAmount);
        
        _targetSwayRotation = Quaternion.Euler(
            _initialRotation.eulerAngles.x + swayY,
            _initialRotation.eulerAngles.y + swayX,
            _initialRotation.eulerAngles.z + swayX
        );
    }
    
    private void CalculateMovementSway()
    {
        if (_characterController != null)
        {
            Vector3 horizontalVelocity = new Vector3(_velocity.x, 0, _velocity.z);
            
            _movementSway = Vector3.Lerp(_movementSway, new Vector3(
                -horizontalVelocity.x * _movementSwayX,
                -horizontalVelocity.magnitude * _movementSwayY,
                0
            ), Time.deltaTime * _movementSwaySmoothing);
        }
    }
    
    private void ApplySway()
    {
        if (Mathf.Approximately(_lookInput.x, 0) && Mathf.Approximately(_lookInput.y, 0))
        {
            _targetSwayRotation = Quaternion.Slerp(transform.localRotation, _initialRotation, Time.deltaTime * _swayResetSmoothing);
        }
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetSwayRotation, Time.deltaTime * _swaySmoothing);
        
        transform.localPosition = Vector3.Lerp(transform.localPosition, _initialPosition + _movementSway, Time.deltaTime * _swaySmoothing);
    }
}



