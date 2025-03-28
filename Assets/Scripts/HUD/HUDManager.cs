using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;
    
    [SerializeField] private Image _healthSlider;
    [SerializeField] private TMP_Text _ammoInMag;
    [SerializeField] private TMP_Text _ammoInInventory;

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
}
