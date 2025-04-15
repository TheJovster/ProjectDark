using System;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    [SerializeField] private string _triggerTakeDamage;
    [SerializeField] private string _triggerDeath;
    [SerializeField] private string _triggerAttack = "Attack";
    
    
    private Animator _animator;

    private void Update()
    {
       
    }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public void Trigger_TakeDamage()
    {
        _animator.SetTrigger(_triggerTakeDamage);
    }

    public void Trigger_Death()
    {
        _animator.SetTrigger(_triggerDeath);
    }

    public void SetFloat_Speed(string name, float value, float damping, float deltaTime)
    {
        _animator.SetFloat(name, value, damping, deltaTime);
    }

    public void FreezeAnimation()
    {
        _animator.speed = 0.0f;
    }

    public void ResumeAnimation()
    {
        _animator.speed = 1.0f;
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(_triggerAttack);
    }

}
