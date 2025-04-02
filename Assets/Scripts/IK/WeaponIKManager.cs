using UnityEngine;
using UnityEngine.Serialization;

// This component should be added to each weapon to store its specific IK data
public class WeaponIKManager : MonoBehaviour
{
    [FormerlySerializedAs("ikData")] public WeaponIKData _ikData;
    
    [FormerlySerializedAs("useLocalTargets")] [Header("Optional Local Targets")]
    // If true, will use the transforms below instead of the ones in the IK data
    public bool _useLocalTargets = false;
    public Transform _rightHandLocalTarget;
    public Transform _leftHandLocalTarget;
    public Transform _rightElbowLocalHint;
    public Transform _leftElbowLocalHint;
    
    private WeaponIKData _runtimeIKData;
    
    private void Awake()
    {
        // Create a runtime copy of the IK data so we can modify it without affecting the original asset
        if (_ikData != null)
        {
            _runtimeIKData = Instantiate(_ikData);
            
            // Override with local targets if specified
            if (_useLocalTargets)
            {
                if (_rightHandLocalTarget != null) _runtimeIKData._rightHandIKTarget = _rightHandLocalTarget;
                if (_leftHandLocalTarget != null) _runtimeIKData._leftHandIKTarget = _leftHandLocalTarget;
                if (_rightElbowLocalHint != null)
                {
                    _runtimeIKData._rightElbowHint = _rightElbowLocalHint;
                    _runtimeIKData._useElbowHints = true;
                }
                if (_leftElbowLocalHint != null)
                {
                    _runtimeIKData._leftElbowHint = _leftElbowLocalHint;
                    _runtimeIKData._useElbowHints = true;
                }
            }
        }
    }
    
    // Get the IK data for this weapon
    public WeaponIKData GetIKData()
    {
        return _runtimeIKData != null ? _runtimeIKData : _ikData;
    }
}

// Example usage component - attaches to the player/character
public class WeaponHandler : MonoBehaviour
{
    private WeaponIKHandler ikHandler;
    private WeaponIKManager currentWeaponManager;
    
    private void Start()
    {
        ikHandler = GetComponent<WeaponIKHandler>();
    }
    
    // Call this when the player equips a new weapon
    public void EquipWeapon(GameObject weaponObject)
    {
        if (weaponObject == null)
        {
            ikHandler.SetWeapon(null);
            currentWeaponManager = null;
            return;
        }
        
        // Try to get the weapon's IK manager
        WeaponIKManager weaponManager = weaponObject.GetComponent<WeaponIKManager>();
        
        if (weaponManager != null)
        {
            currentWeaponManager = weaponManager;
            ikHandler.SetWeapon(weaponManager.GetIKData());
        }
        else
        {
            Debug.LogWarning("Weapon object does not have a WeaponIKManager component!");
        }
    }
}