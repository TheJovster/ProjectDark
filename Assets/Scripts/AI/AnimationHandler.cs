using System;
using Unity.VisualScripting;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    [SerializeField] private string _triggerTakeDamage = "TakeDamage";
    [SerializeField] private string _isDead = "IsDead";
    [SerializeField] private string _triggerAttack = "Attack";
    private Animator _animator;
    private AIAgent _aiAgent;

    private void Update()
    {
       
    }

    private void Awake()
    {
        _aiAgent = GetComponentInParent<AIAgent>();
        _animator = GetComponent<Animator>();
    }

    public void Trigger_TakeDamage()
    {
        _animator.SetTrigger(_triggerTakeDamage);
    }

    public void Trigger_Death()
    {
        _animator.SetBool(_isDead, true);
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

    public void Attack()
    {
        _aiAgent.AttackBehavior();
    }

    public void SetAggressive(string name, bool value)
    {
        _animator.SetBool(name, value);
    }

}
