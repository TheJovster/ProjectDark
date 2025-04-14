using System.Collections.Generic;
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
    
    [Header("Popups")]
    [SerializeField] private Canvas _hudCanvas;
    [SerializeField] private RectTransform _popupContainer;
    [SerializeField] private GameObject _defaultPopupPrefab;
    private Dictionary<string, GameObject> _activePopups = new Dictionary<string, GameObject>();
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
        
        // Create popup container if not assigned
        if (_popupContainer == null)
        {
            GameObject container = new GameObject("PopupContainer");
            _popupContainer = container.AddComponent<RectTransform>();
            _popupContainer.SetParent(_hudCanvas.transform, false);
            _popupContainer.anchorMin = Vector2.zero;
            _popupContainer.anchorMax = Vector2.one;
            _popupContainer.offsetMin = Vector2.zero;
            _popupContainer.offsetMax = Vector2.zero;
        }
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
    
        // Show a popup with the given ID and text
    public void ShowPopup(string popupId, string text, GameObject popupPrefab = null)
    {
        // If a popup with this ID is already active, update it instead
        if (_activePopups.ContainsKey(popupId))
        {
            UpdatePopupText(popupId, text);
            return;
        }
        
        // Determine which prefab to use
        GameObject prefabToUse = popupPrefab != null ? popupPrefab : _defaultPopupPrefab;
        
        if (prefabToUse == null)
        {
            Debug.LogError("No popup prefab provided and no default set in HUDManager");
            return;
        }
        
        // Instantiate the popup
        GameObject popupInstance = Instantiate(prefabToUse, _popupContainer);
        
        // Get the HUDPopup component and initialize it
        HUDPopup popup = popupInstance.GetComponent<HUDPopup>();
        if (popup != null)
        {
            popup.Initialize(popupId, text);
            popup.OnPopupClosed += () => RemovePopup(popupId);
        }
        else
        {
            Debug.LogWarning($"Popup prefab {prefabToUse.name} doesn't have a HUDPopup component");
        }
        
        // Store the popup in our active popups dictionary
        _activePopups[popupId] = popupInstance;
    }
    
    // Update the text of an existing popup
    public void UpdatePopupText(string popupId, string newText)
    {
        if (_activePopups.TryGetValue(popupId, out GameObject popupObj))
        {
            HUDPopup popup = popupObj.GetComponent<HUDPopup>();
            if (popup != null)
            {
                popup.SetText(newText);
            }
        }
    }
    
    // Hide a popup by ID
    public void HidePopup(string popupId)
    {
        if (_activePopups.TryGetValue(popupId, out GameObject popupObj))
        {
            HUDPopup popup = popupObj.GetComponent<HUDPopup>();
            if (popup != null)
            {
                popup.Close();
            }
            else
            {
                Destroy(popupObj);
                _activePopups.Remove(popupId);
            }
        }
    }
    
    // Hide all active popups
    public void HideAllPopups()
    {
        foreach (var popup in _activePopups.Values)
        {
            if (popup != null)
            {
                HUDPopup hudPopup = popup.GetComponent<HUDPopup>();
                if (hudPopup != null)
                {
                    hudPopup.Close();
                }
                else
                {
                    Destroy(popup);
                }
            }
        }
        _activePopups.Clear();
    }
    
    // Remove a popup from the active popups dictionary (called by the popup when closed)
    private void RemovePopup(string popupId)
    {
        if (_activePopups.ContainsKey(popupId))
        {
            _activePopups.Remove(popupId);
        }
    }
}

