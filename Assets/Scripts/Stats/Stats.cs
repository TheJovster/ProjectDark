using System;
using UnityEngine;

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
    
    // Added for gibbing system
    [Header("Gibbing Settings")]
    [SerializeField] private float _gibDamageThreshold = 20f;
    [SerializeField] private bool _enableGibbing = true;
    [SerializeField] private float _highDamageGibMultiplier = 2.0f;
    private ProceduralGibbing _gibbingSystem;
    
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
            _gibbingSystem = GetComponent<ProceduralGibbing>();
        }
    }


    public void TakeDamage(float damageToTake)
    {
        _currentHealth -= damageToTake;
        
        // Check for high damage for gibbing chance increase
        if (_gibbingSystem != null && _enableGibbing && damageToTake >= _gibDamageThreshold)
        {
            float multiplier = Mathf.Min(damageToTake / _gibDamageThreshold, _highDamageGibMultiplier);
            _gibbingSystem.SetGibChanceMultiplier(multiplier);
        }
        
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Death();
            
            if (!_isPlayer)
            {
                _capsuleCollider.enabled = false;
                
                // Handle gibbing at death
                if (_gibbingSystem != null && _enableGibbing)
                {
                    // Gibbing will be processed in the ProceduralGibbing Update method
                    // when it detects that _isAlive is false
                }
            }
        }
        
        if (_isPlayer && _isAlive)
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
            _gibbingSystem.ForceGibAll();
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
    
    // Added for gibbing system
    public void InstantGibDeath()
    {
        if (_gibbingSystem != null && _enableGibbing)
        {
            _gibbingSystem.SetGibChanceMultiplier(3.0f); // High chance
            TakeDamage(_maxHealth * 2); // Ensure death
        }
        else
        {
            TakeDamage(_maxHealth);
        }
    }
    
    public void EnableGibbing(bool enable)
    {
        _enableGibbing = enable;
        
        if (_gibbingSystem != null)
        {
            _gibbingSystem.SetGibEnabled(enable);
        }
    }
    
    public void ForceGib(string partName = "")
    {
        if (_gibbingSystem != null)
        {
            if (string.IsNullOrEmpty(partName))
            {
                _gibbingSystem.ForceGibAll();
            }
            else
            {
                _gibbingSystem.ForceGib(partName);
            }
        }
    }
}
