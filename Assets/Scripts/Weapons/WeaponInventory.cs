using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    private Weapon _currentWeapon;
    [SerializeField] private List<Weapon> _weapons;
    [SerializeField] private Transform _weaponContainer;
    private int _currentWeaponIndex = 0;
    
    #region Properties
    public Weapon CurrentWeapon => _currentWeapon;
    #endregion

    private void Awake()
    {
        PopulateWeaponsList();

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
        if (_currentWeaponIndex <= 0)
        {
            _currentWeaponIndex = 0;
        }
        SwitchWeapon(oldIndex, _currentWeaponIndex);
    }

    public void IncrementWeaponIndex()
    {
        int oldIndex = _currentWeaponIndex;
        _currentWeaponIndex++;
        if (_currentWeaponIndex >= _weapons.Count)
        {
            _currentWeaponIndex = _weapons.Count;
        }
        SwitchWeapon(oldIndex, _currentWeaponIndex);
    }

    private void SwitchWeapon(int oldIndex, int newIndex)
    {
        _weapons[oldIndex].gameObject.SetActive(false);
        _weapons[newIndex].gameObject.SetActive(true);
        _currentWeapon = _weapons[newIndex];
    }
}
