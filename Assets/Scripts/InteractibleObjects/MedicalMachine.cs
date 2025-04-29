using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MedicalMachine : MonoBehaviour
{
    [SerializeField] private float _amountToHeal = 10.0f;
    [SerializeField] private float _totalHealCapacity = 30.0f;
    [SerializeField] private AudioClip _healSound;
    [SerializeField] private AudioClip _emptySound;
    [SerializeField] private HashSet<string> _noHeal = new HashSet<string>();
    private InteractableObject _interactableObject;
    
    private void Awake()
    {
        _interactableObject = GetComponent<InteractableObject>();
        _noHeal.Add("The machine has no energy left.");
    }

    public void Heal()
    {
        if (GameManager.Instance.PlayerInstance.GetComponent<Stats>().CurrentHealth >=
            GameManager.Instance.PlayerInstance.GetComponent<Stats>().MaxHealth) return;
        if(_totalHealCapacity <= 0)
        {
            _interactableObject.SetInteractionPrompt(_noHeal.First());
            AudioManager.Instance.PlayEffectDoubleVolume(_emptySound);
            return;
        }
        GameManager.Instance.PlayerInstance.GetComponent<Stats>().AddHealth(_amountToHeal);
        _totalHealCapacity -= _amountToHeal;
    }
}
