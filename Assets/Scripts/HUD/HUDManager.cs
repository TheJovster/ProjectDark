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
    [SerializeField] private GameObject _flashlightOn;
    [SerializeField] private Image _flashLightOnImage;
    [SerializeField] private GameObject _flashLightOff;
    
    
    [Header("Mouse Textures")] 
    [SerializeField] private Texture2D _defaultMouseCursor;
    [SerializeField] private Texture2D _selectionMouseCursor;
    private CursorMode _cursorMode = CursorMode.Auto;
    [SerializeField] private Vector2 _cursorHotSpot = Vector3.zero;
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
        SetDefaultCursor();
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

    public void ToggleFireModeIcon(bool value)
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

    public void ToggleFlashlight(bool value)
    {
        if (value)
        {
            _flashlightOn.SetActive(true);
        }
        else
        {
            _flashlightOn.SetActive(false);
        }
    }

    public void DisableAimReticle()
    {
        _aimReticle.SetActive(false);
    }

    public void SetCursorMode(CursorMode cursorMode)
    {
        _cursorMode = cursorMode;
    }

    public void SetFlashLightFill(float currentValue, float maxValue)
    {
        _flashLightOnImage.fillAmount = currentValue / maxValue;
    }

    public void SetDefaultCursor()
    {
        Cursor.SetCursor(_defaultMouseCursor, _cursorHotSpot, _cursorMode);
    }
}
