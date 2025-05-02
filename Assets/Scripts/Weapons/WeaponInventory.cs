using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    private Weapon _currentWeapon;
    [SerializeField] private List<Weapon> _weapons;
    [SerializeField] private Transform _weaponContainer;
    [SerializeField] private int _currentWeaponIndex = 0;
    private PlayerController _playerController;
    private WeaponIKHandler _weaponIKHandler;
    [SerializeField]private Animator _handsAnimator;
    
    //weapon switching
    private bool _isWeaponSwitching = false;
    [SerializeField]private float _weaponLoweredPosition = -0.3f; 
    [SerializeField]private float _weaponRaisedPosition = 0f;     
    [SerializeField]private float _weaponMoveSpeed = 2f;          
        
    #region Properties
    public Weapon CurrentWeapon => _currentWeapon;
    public PlayerController PlayerController => _playerController;
    #endregion

    private void Awake()
    {
        PopulateWeaponsList();
        _playerController = GetComponent<PlayerController>();
        _weaponIKHandler = GetComponentInChildren<WeaponIKHandler>();
        _weaponIKHandler.ApplyWeaponIK(_currentWeapon.RightHandPosition, _currentWeapon.LeftHandPosition, _currentWeapon.AnimatorOverrideController);
    }

    private void Start()
    {
       
    }

    private void PopulateWeaponsList()
    {
        foreach (Weapon weapon in _weaponContainer.GetComponentsInChildren<Weapon>())
        {
            _weapons.Add(weapon);
        }

        foreach (Weapon weapon in _weapons)
        {
            weapon.gameObject.SetActive(false);
        }
        _currentWeapon = _weapons[_currentWeaponIndex];
        _weapons[_currentWeaponIndex].gameObject.SetActive(true);
    }

    public void DecrementWeaponIndex()
    {
        if (_isWeaponSwitching) return;
        int oldIndex = _currentWeaponIndex;
        --_currentWeaponIndex;
        if (_currentWeaponIndex < 0)
        {
            _currentWeaponIndex = _weapons.Count - 1;
        }
        SwitchWeapon(oldIndex, _currentWeaponIndex);
    }

    public void IncrementWeaponIndex()
    {
        if (_isWeaponSwitching) return;
        int oldIndex = _currentWeaponIndex;
        _currentWeaponIndex++;
        if (_currentWeaponIndex >= _weapons.Count)
        {
            _currentWeaponIndex = 0;
        }
        SwitchWeapon(oldIndex, _currentWeaponIndex);
    }

    private void SwitchWeapon(int oldIndex, int newIndex)
    {
        if (_isWeaponSwitching) return;

        StartCoroutine(SwitchWeaponRoutine(oldIndex, newIndex));
    }

    private IEnumerator SwitchWeaponRoutine(int oldIndex, int newIndex)
    {
        _isWeaponSwitching = true;
        
        Vector3 startPosition = _weaponContainer.localPosition;
        Vector3 loweredPosition = new Vector3(startPosition.x, _weaponLoweredPosition, startPosition.z);
    
        // Lower current weapon
        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            _weaponContainer.localPosition = Vector3.Lerp(startPosition, loweredPosition, elapsedTime * _weaponMoveSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        // Ensure weapon is fully lowered
        _weaponContainer.localPosition = loweredPosition;
    
        // Switch weapons
        _weapons[oldIndex].gameObject.SetActive(false);
        _weapons[newIndex].gameObject.SetActive(true);
        _currentWeapon = _weapons[newIndex];
        _handsAnimator.runtimeAnimatorController = _currentWeapon.AnimatorOverrideController;
        _weaponIKHandler.ApplyWeaponIK(_currentWeapon.RightHandPosition, _currentWeapon.LeftHandPosition, _currentWeapon.AnimatorOverrideController);
        _currentWeapon.SetCanFire();

        // Update HUD
        HUDManager.Instance.UpdateAmmoCount(_currentWeapon.GetCurrentAmmoInMag(), _currentWeapon.GetCurrentAmmoInInventory());
        HUDManager.Instance.UpdateWeaponName(_currentWeapon.WeapoonName);
        HUDManager.Instance.ToggleFireModeIcon(_currentWeapon.IsSemi);
        
        // Raise new weapon
        Vector3 raisedPosition = new Vector3(startPosition.x, _weaponRaisedPosition, startPosition.z);
        elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            _weaponContainer.localPosition = Vector3.Lerp(loweredPosition, raisedPosition, elapsedTime * _weaponMoveSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        // Ensure weapon is fully raised
        _weaponContainer.localPosition = raisedPosition;
        
        _isWeaponSwitching = false;
        _currentWeapon.SetCanFire();
    }

}
