using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class Stats : MonoBehaviour
{
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _currentStamina;
    [SerializeField] private float _maxStamina;
    [SerializeField] private bool _isPlayer;
    private AnimationHandler _animationHandler;
    private CapsuleCollider _capsuleCollider;
    [SerializeField]private bool _isAlive = true;
    
    #region Properties
    public float CurrentHealth => _currentHealth;
    public float CurrentStamina => _currentStamina;
    public float MaxHealth => _maxHealth;
    public float MaxStamina => _maxStamina;
    public bool IsAlive => _isAlive;
    #endregion

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
        if (!_isPlayer)
        {
            _animationHandler = GetComponentInChildren<AnimationHandler>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
        }
    }

    public void TakeDamage(float damageToTake)
    {
        _currentHealth -= damageToTake;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Death();
            if (!_isPlayer)
            {
                _capsuleCollider.enabled = false;
                //temporary - will add more functionality, like gibbing
            }
        }
        if (_isPlayer && _isAlive && _isPlayer)
        {
            GameManager.Instance.SetHealthBarFill(_currentHealth, _maxHealth);
        }
        if (_isAlive && !_isPlayer)
        {
            _animationHandler.Trigger_TakeDamage();
        }

    }

    private void Death()
    {
        _isAlive = false;
        if (!_isPlayer)
        {
            _animationHandler.Trigger_Death();
        }
        else if (_isPlayer)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.GameOver);
        }
    }
    
    public void SetStamina(float amount)
    {
        _currentStamina = amount;
    }

    public void SetHealth(float amount)
    {
        _currentHealth = amount;
    }
    
    
    //player specific functions
    public void DrainStamina(float drainRate)
    {
        if (_isPlayer)
        {
            _currentStamina -= drainRate * Time.deltaTime;
            if (_currentStamina <= 0)
            {
                _currentStamina = 0;
            }
            GameManager.Instance.SetStaminaBarFill(_currentStamina, _maxStamina);
        }
    }

    public void RegenStamina(float restoreRate)
    {
        if (_isPlayer)
        {
            _currentStamina += restoreRate * Time.deltaTime;
            if (_currentStamina >= _maxStamina)
            {
                _currentStamina = _maxStamina;
            }
            GameManager.Instance.SetStaminaBarFill(_currentStamina, _maxStamina);
        }
    }
    
    public void ReduceStamina(float staminaCost)
    {
        if (_isPlayer)
        {
            _currentStamina -= staminaCost * Time.deltaTime;
            GameManager.Instance.SetStaminaBarFill(_currentStamina, _maxStamina);
        }
    }



    public void AddHealth(float amounToAdd)
    {
        if (_isPlayer)
        {
            _currentHealth += amounToAdd;
            if (_currentHealth > _maxHealth)
            {
                _currentHealth = _maxHealth;
            }
            GameManager.Instance.SetHealthBarFill(_currentHealth, _maxHealth);
        }
    }

    public void AddStamina(float amountToAdd)
    {
        if(_isPlayer){}
        _currentStamina += amountToAdd;
        if (_currentStamina > _maxStamina)
        {
            _currentStamina = _maxStamina;
        }
        GameManager.Instance.SetStaminaBarFill(_currentStamina, _maxStamina);
    }
}
