using System;
using System.Collections.Generic;
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
        _weapons[oldIndex].gameObject.SetActive(false);
        _weapons[newIndex].gameObject.SetActive(true);
        _currentWeapon = _weapons[newIndex];
        _handsAnimator.runtimeAnimatorController = _currentWeapon.AnimatorOverrideController;
        Debug.Log("Switched Animator");
        _weaponIKHandler.ApplyWeaponIK(_currentWeapon.RightHandPosition, _currentWeapon.LeftHandPosition, _currentWeapon.AnimatorOverrideController);
        Debug.Log("Applied IK");
        HUDManager.Instance.UpdateAmmoCount(_currentWeapon.GetCurrentAmmoInMag(), _currentWeapon.GetCurrentAmmoInInventory());
    }

}
