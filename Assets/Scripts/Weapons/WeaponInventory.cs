using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    private Weapon _currentWeapon;
    [SerializeField] private List<Weapon> _weapons;
    [SerializeField] private Transform _weaponContainer;
    
    #region Properties
    public Weapon CurrentWeapon => _currentWeapon;
    #endregion

    private void Awake()
    {
        PopulateWeaponsList();
        _currentWeapon = _weapons[0];
    }

    private void PopulateWeaponsList()
    {
        foreach (Weapon weapon in _weaponContainer.GetComponentsInChildren<Weapon>())
        {
            _weapons.Add(weapon);
        }
    }
}
