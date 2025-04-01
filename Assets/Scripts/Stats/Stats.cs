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
    private bool _isAlive = true;
    
    #region Properties
    public float CurrentHealth => _currentHealth;
    public float CurrentStamina => _currentStamina;
    public bool IsAlive => _isAlive;
    #endregion

    private void Awake()
    {
        _currentHealth = _maxHealth;
        if (!_isPlayer)
        {
            _animationHandler = GetComponentInChildren<AnimationHandler>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
        }
    }

    public void TakeDamage(float damageToTake)
    {
        _currentHealth -= damageToTake;
        if (_currentHealth > 0 && !_isPlayer)
        {
            _animationHandler.Trigger_TakeDamage();
        }
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            if (!_isPlayer)
            {
                Death();
                _capsuleCollider.enabled = false;
                //temporary - will add more functionality, like gibbing
            }
            else if (_isPlayer)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.GameOver);
            }
        }
    }

    private void Death()
    {
        _isAlive = false;
        if (!_isPlayer)
        {
            _animationHandler.Trigger_Death();
        }

    }

    public void ReduceStamina(float staminaCost)
    {
        _currentStamina -= staminaCost * Time.deltaTime;
    }

    public void SetStamina(float amount)
    {
        _currentStamina = amount;
    }

    public void SetHealth(float amount)
    {
        _currentHealth = amount;
    }

    public void AddHealth(float amounToAdd)
    {
        _currentHealth += amounToAdd;
        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }
    }

    public void AddStamina(float amountToAdd)
    {
        _currentStamina += amountToAdd;
        if (_currentStamina > _maxStamina)
        {
            _currentStamina = _maxStamina;
        }
    }
}
