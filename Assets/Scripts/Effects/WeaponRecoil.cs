using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Positional Recoil")]
    [SerializeField] private float _recoilX = 0f;        
    [SerializeField] private float _recoilY = 0f;         
    [SerializeField] private float _recoilZ = -0.5f;      
    
    [Header("Rotational Recoil")]
    [SerializeField] private float _recoilRotationX = -10.0f;  
    [SerializeField] private float _recoilRotationY = 0.1f;   
    [SerializeField] private float _recoilRotationZ = 0.1f;   
    
    [Header("Recoil Settings")]
    [SerializeField] private float _returnSpeed = 10f;      
    [SerializeField] private float _snappiness = 12f;       
    [SerializeField] private bool _applyToCameraRotation = false; 
    [SerializeField] private float _cameraRecoilMultiplier = 0.1f; 
    
    [Header("Additional Recoil Components")]
    [Tooltip("Assign the camera game object to this field")]
    [SerializeField]private Transform _cameraTransform;
    
    private Vector3 _targetRecoilPosition;
    private Vector3 _targetRecoilRotation;
    private Vector3 _currentRecoilPosition;
    private Vector3 _currentRecoilRotation;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    
    private void Start()
    {
        _initialPosition = transform.localPosition;
        _initialRotation = transform.localRotation;
    }
    
    private void Update()
    {
        _targetRecoilPosition = Vector3.Lerp(_targetRecoilPosition, Vector3.zero, _returnSpeed * Time.deltaTime);
        _targetRecoilRotation = Vector3.Lerp(_targetRecoilRotation, Vector3.zero, _returnSpeed * Time.deltaTime);
        
        _currentRecoilPosition = Vector3.Lerp(_currentRecoilPosition, _targetRecoilPosition, _snappiness * Time.deltaTime);
        _currentRecoilRotation = Vector3.Lerp(_currentRecoilRotation, _targetRecoilRotation, _snappiness * Time.deltaTime);
        
        transform.localPosition = _initialPosition + _currentRecoilPosition;
        transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles + _currentRecoilRotation);
        
        if (_applyToCameraRotation && _cameraTransform != null)
        {
            _cameraTransform.localRotation = Quaternion.Euler(
                _cameraTransform.localEulerAngles + 
                new Vector3(
                    _targetRecoilRotation.x * _cameraRecoilMultiplier, 
                    _targetRecoilRotation.y * _cameraRecoilMultiplier, 
                    0
                ) * Time.deltaTime
            );
        }
    }
    

    public void ApplyRecoil(float multiplier = 1.0f)
    {
        float yRandom = Random.Range(-1f, 1f);
        
        _targetRecoilPosition += new Vector3(
            _recoilX * multiplier, 
            _recoilY * yRandom * multiplier, 
            _recoilZ * multiplier
        );
        
        _targetRecoilRotation += new Vector3(
            _recoilRotationX * multiplier, 
            _recoilRotationY * yRandom * multiplier, 
            _recoilRotationZ * Random.Range(-1f, 1f) * multiplier
        );
    }
    
    public void ResetRecoil()
    {
        _targetRecoilPosition = Vector3.zero;
        _targetRecoilRotation = Vector3.zero;
        _currentRecoilPosition = Vector3.zero;
        _currentRecoilRotation = Vector3.zero;
        
        transform.localPosition = _initialPosition;
        transform.localRotation = _initialRotation;
    }
}
