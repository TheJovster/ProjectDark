using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Weapon : MonoBehaviour
{
    public enum WeaponType
    {
        Revolver,
        Pistol,
        Shotgun,
        AssaultRifle,
    }
    
    [Header("Weapon Properties")] [SerializeField]
    private string _weaponName;
    [SerializeField] private WeaponType _weaponType;
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private Animator _weaponAnimator;
    private WeaponInventory _weaponInventory;
    private AmmoInventory _ammoInventory;
    [SerializeField] private BallisticProjectile _projectilePrefab;
    private bool _canFire = true;
    private bool _isFiring = false;
    [SerializeField] private Transform _barrels;
    [SerializeField] private WeaponIKHandler _IKHandler;
    [SerializeField] private AnimatorOverrideController _animatorOverrideController;
    private bool _isReloading;

    [Header("IK Settings")] [SerializeField, Range(0f, 1f)]
    private float _rightHandIKWeight = 1.0f;

    [SerializeField, Range(0f, 1f)] private float _leftHandIKWeight = 1.0f;
    [SerializeField] private Transform _rightHandPosition;
    [SerializeField] private Transform _leftHandPosition;
    [SerializeField] private Transform _rightElbowHint;
    [SerializeField] private Transform _leftElbowHint;
    [SerializeField] private float _rightElbowHintWeight = 1.0f;
    [SerializeField] private float _leftElbowHintWeight = 1.0f;
    [SerializeField] private bool _useElbowHints = false;

    [SerializeField] private int _currentAmmoInMag;
    [SerializeField] private int _maxAmmoInMag;
    private bool _isEmpty;

    [Header("Weapon Behavior Properties")] [SerializeField]
    private bool _hasSelectFire = false;
    [SerializeField] private bool _isSemi = false;
    [SerializeField] private float _rateOfFire;
    private float _timeSinceLastShot = 0.0f;
    [SerializeField] private int _weaponDamage = 10;
    [SerializeField] private bool _canADS = true;
    [SerializeField] private Transform _magazine;

    [Header("Audio and VFX")] [Tooltip("Set this to 5")] 
    [SerializeField] private AudioClip[] _weaponAudioClips;
    [SerializeField] private AudioClip[] _weaponReloadClips;
    [SerializeField] private AudioClip[] _weaponToggleFireClips;
    [SerializeField] private ParticleSystem _muzzleFlash;
    
    [SerializeField, Tooltip("Set to 0 if the weapon is automatic")] private float _muzzleFlashDelay = 0.5f; //used only on semi weapons. 
    [SerializeField] private float _screenShakeIntensity;
    [SerializeField] private float _screenShakeDuration;
    [SerializeField] private float _screenShakeSpeed;

    private WeaponRecoil _weaponRecoil;

    //ads settings to be used in the player controller
    [SerializeField] private Vector3 _adsPosition;
    [SerializeField] private Transform _scope;
    [SerializeField] private Transform _aimReticleObject;

    [Header("Extre")] 
    [SerializeField] private Animator _handsAnimator;
    private HashSet<string> _reloadHashSet = new HashSet<string>();


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

    public Transform RightHandPosition => _rightHandPosition;
    public Transform LeftHandPosition => _leftHandPosition;

    public Transform RightElbowHint => _rightElbowHint;
    public Transform LeftElbowHint => _leftElbowHint;
    public float RightElbowHintWeight => _rightElbowHintWeight;
    public float LeftElbowHintWeight => _leftElbowHintWeight;
    public bool UseElbowHints => _useElbowHints;

    public float RightHandIKWeight => _rightHandIKWeight;
    public float LeftHandIKWeight => _leftHandIKWeight;
    public Vector3 ADSPosition => _adsPosition;
    public bool CanADS => _canADS;
    
    public Transform AimReticleObject => _aimReticleObject;
    
    public bool IsReloading => _isReloading;
    
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

        _reloadHashSet.Add("Reload");
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

    public void ToggleSemi()
    {
        if (_hasSelectFire)
        {
            _isSemi = !_isSemi;
            AudioManager.Instance.PlayEffect(_weaponToggleFireClips[Random.Range(0, _weaponToggleFireClips.Length)]);
            HUDManager.Instance.ToggleFireModeIcon(_isSemi);
        }
    }
    
    public void Fire()
    {
        if (_weaponName == "XM994" && _currentAmmoInMag > 0) //this is a lot of ducttape - will fix this with a local animator.
        {
            _barrels.Rotate(Vector3.forward * (720.0f * Time.deltaTime));
        }

        if (_currentAmmoInMag > 0 && _timeSinceLastShot >= _rateOfFire)
        {
            BallisticProjectile projectileInstance =
                Instantiate(_projectilePrefab, _muzzlePoint.position, _muzzlePoint.rotation);
            projectileInstance.SetDamageToDeal(_weaponDamage);
            projectileInstance.Fire(_muzzlePoint.forward);
            _timeSinceLastShot = 0.0f;
            _currentAmmoInMag--;
            int audioClipIndex = Random.Range(0, _weaponAudioClips.Length);
            AudioManager.Instance.PlayEffectDoubleVolume(_weaponAudioClips[audioClipIndex]);
            _weaponRecoil.ApplyRecoil();
            _muzzleFlash.Play();
            _weaponInventory.PlayerController.TriggerShake();
            HUDManager.Instance.UpdateAmmoCount(_weaponInventory.CurrentWeapon.GetCurrentAmmoInMag(),
                _weaponInventory.CurrentWeapon.GetCurrentAmmoInInventory());
            if (IsSemi)
            {
                StartCoroutine(StopMuzzleFlashAfterDelay(0.05f));
            }

        }

        if (_isEmpty && _ammoInventory.ReturnCurrentAmmoAmount(_weaponInventory.CurrentWeapon.CurrentWeaponType) > 0)
        {
            Reload();
        }
    }

    private IEnumerator StopMuzzleFlashAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _muzzleFlash.Stop();
    }

    public void StartReload()
    {
        _muzzleFlash.Stop();
        _isReloading = true;
        _handsAnimator.SetTrigger(_reloadHashSet.First());
    }

    public void EndReload()
    {
        _isReloading = false;
    }
    
    public void Reload() //I guess I can just have this as an anim event;
    {
        _muzzleFlash.Stop();//makes sure the muzzle flash isn't firing
        if (_ammoInventory.ReturnCurrentAmmoAmount(_weaponType) <= 0) return;
        int amountToReduce = _weaponInventory.CurrentWeapon.GetMaxAmmoInMag() -
                             _weaponInventory.CurrentWeapon.GetCurrentAmmoInMag(); //works most of the time need more edgecases
        _currentAmmoInMag = _maxAmmoInMag;
        _ammoInventory.ReduceAmmoAmount(_weaponInventory.CurrentWeapon.CurrentWeaponType, amountToReduce);
        HUDManager.Instance.UpdateAmmoCount(_weaponInventory.CurrentWeapon.GetCurrentAmmoInMag(),
            _weaponInventory.CurrentWeapon.GetCurrentAmmoInInventory());
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

    public void SetMuzzlePointLookDirection(Vector3 lookDirection)
    {
        _muzzlePoint.LookAt(lookDirection);
    }

    public void ScopeLookAtAimPoint(Vector3 aimPoint)
    {
        if (_scope == null) return;
        _scope.LookAt(aimPoint);
    }

    public void ResetScope()
    {
        if (_scope == null) return;
        _scope.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }
    
    
    

//setters
    
    //animator setters
    public void DisableAnimator()
    {
        _weaponAnimator.enabled = false;
    }

    public void SetCanFire(bool value)
    {
        _canFire = value;
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
