using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Serialization;


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
    [SerializeField] private string _weaponName;
    [SerializeField] private WeaponType _weaponType;
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private Animator _weaponAnimator;
    private WeaponInventory _weaponInventory;
    private AmmoInventory _ammoInventory;
    [SerializeField] private PlayerProjectile _projectilePrefab;
    private bool _canFire = true;
    private bool _isFiring = false;
    [SerializeField] private Transform _barrels;
    [SerializeField] private WeaponIKData _weaponData; //do I even need the data?
    [SerializeField] private WeaponIKHandler _IKHandler;
    [SerializeField] private AnimatorOverrideController _animatorOverrideController;

    [Header("IK Settings")] 
    [SerializeField, Range(0f, 1f)] private float _rightHandIKWeight = 1.0f;
    [SerializeField, Range(0f, 1f)] private float _leftHandIKWeight = 1.0f;
    [SerializeField] private Transform _rightHandPosition;
    [SerializeField] private Transform _leftHandPosition;
    [SerializeField] private Transform _rightElbowHint;
    [SerializeField] private Transform _leftElbowHint;
    [SerializeField] private float _rightElbowHintWeight = 1.0f;
    [SerializeField] private float _leftElbowHintWeight = 1.0f;
    [SerializeField] private bool _useElbowHints = false;
    
    [SerializeField]private int _currentAmmoInMag;
    [SerializeField] private int _maxAmmoInMag;
    private bool _isEmpty;
    
    [Header("Weapon Behavior Properties")] 
    [SerializeField] private bool _hasSelectFire = false;
    [SerializeField] private bool _isSemi = false;
    [SerializeField] private float _rateOfFire;
    private float _timeSinceLastShot = 0.0f;
    [SerializeField] private int _weaponDamage = 10;
    [SerializeField] private bool _canADS = true;


    [Header("Audio and VFX")]
    [Tooltip("Set this to 5")]
    [SerializeField] private AudioClip[] _weaponAudioClips; 
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private float _screenShakeIntensity;
    [SerializeField] private float _screenShakeDuration;
    [SerializeField] private float _screenShakeSpeed;
    
    private WeaponRecoil _weaponRecoil;
    
    #region Properties
    public bool IsSemi => _isSemi;
    public bool CanFire => _canFire;
    public string WeapoonName => _weaponName;
    public Animator WeaponAnimator => _weaponAnimator;
    public AnimatorOverrideController AnimatorOverrideController => _animatorOverrideController;
    public WeaponType CurrentWeaponType => _weaponType;

    public float ScreenShakeIntensity => _screenShakeIntensity;
    public float ScreenShakeDuration => _screenShakeDuration;

    public float ScreenShakeSpeed => _screenShakeSpeed;
    public WeaponIKData IKData => _weaponData;
    
    public Transform RightHandPosition => _rightHandPosition;
    public Transform LeftHandPosition => _leftHandPosition;
    
    public Transform RightElbowHint => _rightElbowHint;
    public Transform LeftElbowHint => _leftElbowHint;
    public float RightElbowHintWeight => _rightElbowHintWeight;
    public float LeftElbowHintWeight => _leftElbowHintWeight;
    public bool UseElbowHints => _useElbowHints;
    
    public float RightHandIKWeight => _rightHandIKWeight;
    public float LeftHandIKWeight => _leftHandIKWeight;
    
    #endregion
    
    private void Awake()
    {
        _weaponInventory = GetComponentInParent<WeaponInventory>();
        _ammoInventory = GetComponentInParent<AmmoInventory>();
        _weaponRecoil = GetComponent<WeaponRecoil>();
        if (_muzzleFlash == null)
        {
            _muzzleFlash = GetComponentInChildren<ParticleSystem>();
        }
    }
    
    private void Start()
    {
        SetCurrentAmmoInMag();
    }

    public void SetCurrentAmmoInMag()
    {
        _currentAmmoInMag = _maxAmmoInMag;
    }
    

    private void Update()
    {
        _isEmpty = _currentAmmoInMag == 0;
        _timeSinceLastShot += Time.deltaTime;
        if (_timeSinceLastShot >= 10.0f)
        {
            _timeSinceLastShot = 10.0f;
        }

        _canFire = _timeSinceLastShot >= _rateOfFire &&
                   _currentAmmoInMag > 0;
        if (GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
        {
            _muzzleFlash.Stop();
        }
        
    }

    public void Fire()
    {
        if (_weaponName == "Minigun") //this is a lot of ducttape - will fix this with a local animator.
        {
            _barrels.Rotate(Vector3.forward * (720.0f * Time.deltaTime));
        }
        
        if (_currentAmmoInMag > 0 && _timeSinceLastShot >= _rateOfFire)
        {
            PlayerProjectile projectileInstance =
                Instantiate(_projectilePrefab, _muzzlePoint.position, _muzzlePoint.rotation);
            projectileInstance.SetDamage(_weaponDamage);
            projectileInstance.SetRotation(_muzzlePoint.forward);
            _timeSinceLastShot = 0.0f;
            _currentAmmoInMag--;
            int audioClipIndex = Random.Range(0, _weaponAudioClips.Length);
            AudioManager.Instance.PlayEffect(_weaponAudioClips[audioClipIndex]);
            _weaponRecoil.ApplyRecoil();
            _muzzleFlash.Play();
            _weaponInventory.PlayerController.TriggerShake();
            HUDManager.Instance.UpdateAmmoCount(_weaponInventory.CurrentWeapon.GetCurrentAmmoInMag(), 
                _weaponInventory.CurrentWeapon.GetCurrentAmmoInInventory());
        }
        if (_isEmpty && _ammoInventory.ReturnCurrentAmmoAmount(_weaponInventory.CurrentWeapon.CurrentWeaponType) > 0)
        {
            Reload();
        }
    }
    
    public void Reload() //I guess I can just have this as an anim event;
    {
        _muzzleFlash.Stop();
        int amountToReduce = _weaponInventory.CurrentWeapon.GetMaxAmmoInMag() - 
                             _weaponInventory.CurrentWeapon.GetCurrentAmmoInMag();
        if (_ammoInventory.ReturnCurrentAmmoAmount(_weaponInventory.CurrentWeapon.CurrentWeaponType) <= 0)
        {
            return;
        }
        _currentAmmoInMag = _maxAmmoInMag; 
        _ammoInventory.ReduceAmmoAmount(_weaponInventory.CurrentWeapon.CurrentWeaponType, amountToReduce);
        HUDManager.Instance.UpdateAmmoCount(_weaponInventory.CurrentWeapon.GetCurrentAmmoInMag(), 
            _weaponInventory.CurrentWeapon.GetCurrentAmmoInInventory());

        //edgecases
    }
    
    public void ToggleADS(bool isAiming)
    {
        if(_canADS)
        {
            if(isAiming)
            {
                
            }
            else if(!isAiming)
            {
                
            }
        }
        else 
        {
            return;
        }

    }
    
    
    //getter functions

    public int GetCurrentAmmoInMag()
    {
        return _currentAmmoInMag;
    }

    public int GetCurrentAmmoInInventory()
    {
        return _ammoInventory.ReturnCurrentAmmoAmount(_weaponType);
    }

    public int GetMaxAmmoInMag()
    {
        return _maxAmmoInMag;
    }
    
    //setters
    
    //animator setters
    public void DisableAnimator()
    {
        _weaponAnimator.enabled = false;
    }
    
    //animation events
    
    //public void LowerWeapon()
    //{
    //anim event disables firing and lovers the weapon. First frame of the animation when switching the weapons. I could, maybe, go with a translation instead of an anim event?
    //}
    
    //public void RaiseWeapon()
    //{
    //anim event when raising the weapon, enables weapon fire. 
    //}
    
    //public void Reload()
    //public void SwitchWeapon()
    //more stuff to add?
    
    
    //public setters
    public void StopMuzzleFlash()
    {
        _muzzleFlash.Stop();
    }
}
