using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [SerializeField] private GameObject _aimReticle;
    [SerializeField] private Image _healthSlider;
    [SerializeField] private TMP_Text _ammoInMag;
    [SerializeField] private TMP_Text _ammoInInventory;
    [SerializeField] private TMP_Text _weaponName;
    [SerializeField] private GameObject _semiAutoIcon;
    [SerializeField] private GameObject _fullAutoIcon;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        DontDestroyOnLoad(this);
    }

    public void UpdateAmmoCount(int ammoInMag, int ammoInInventory)
    {
        _ammoInMag.text = ammoInMag.ToString();
        _ammoInInventory.text = ammoInInventory.ToString();
    }

    public void UpdateWeaponName(string name)
    {
        _weaponName.text = name;
    }

    public void EnableAimReticle()
    {
        _aimReticle.SetActive(true);
    }

    public void SetFireModeIcon(bool value)
    {
        if (value)
        {
            _semiAutoIcon.SetActive(true);
            _fullAutoIcon.SetActive(false);
        }
        else
        {
            _semiAutoIcon.SetActive(false);
            _fullAutoIcon.SetActive(true);
        }
    }

    public void DisableAimReticle()
    {
        _aimReticle.SetActive(false);
    }
}
