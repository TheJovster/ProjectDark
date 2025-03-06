using System;
using System.Reflection;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum WeaponType
    {
        Revolver,
        Pistol,
        Shotgun,
        AssaultRifle,
    }

    [Header("Weapon Properties")]
    [SerializeField] private WeaponType _weaponType;
    [SerializeField] private Transform _muzzlePoint;
    private WeaponInventory _weaponInventory;
    private AmmoInventory _ammoInventory;
    [SerializeField] private PlayerProjectile _projectilePrefab;

    [SerializeField]private int _currentAmmoInMag;
    [SerializeField] private int _maxAmmoInMag;
    
    [Header("Weapon Behavior Properties")] 
    [SerializeField] private bool _hasSelectFire = false;
    [SerializeField] private bool _isSemi = false;
    [SerializeField] private float _rateOfFire;
    private float _timeSinceLastShot = 0.0f;
    [SerializeField] private float _weaponRecoilAmplitude;
    [SerializeField] private float _weaponRecoilForce;
    [SerializeField] private int _weaponDamage = 10;
    
    #region Properties
    public bool IsSemi => _isSemi;
    #endregion
    
    private void Awake()
    {
        _weaponInventory = GetComponentInParent<WeaponInventory>();
        _ammoInventory = GetComponentInParent<AmmoInventory>();
    }
    
    private void Start()
    {
        _currentAmmoInMag = _maxAmmoInMag;
    }

    private void Update()
    {
        _timeSinceLastShot += Time.deltaTime;
        if (_timeSinceLastShot >= 10.0f)
        {
            _timeSinceLastShot = 10.0f;
        }
    }

    public void Fire()
    {
        if (_currentAmmoInMag > 0 && _timeSinceLastShot >= _rateOfFire)
        {
            PlayerProjectile projectileInstance = Instantiate(_projectilePrefab, _muzzlePoint.position, _muzzlePoint.rotation);
            projectileInstance.SetDamage(_weaponDamage);
            projectileInstance.SetRotation(_muzzlePoint.forward);
            _timeSinceLastShot = 0.0f;
            _currentAmmoInMag--;
        }
        else return;
    }

    public void Reload() //I guess I can just have this as an anim event;
    {
        _currentAmmoInMag = _maxAmmoInMag; 
        //edgecases
    }
}
