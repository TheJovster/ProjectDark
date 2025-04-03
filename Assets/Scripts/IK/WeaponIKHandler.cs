using System;
using UnityEngine;
using UnityEngine.Serialization;

// This script handles IK for weapon positioning
[RequireComponent(typeof(Animator))]
public class WeaponIKHandler : MonoBehaviour
{

    [Header("IK Controls")]
    [Range(0, 1)] public float _ikWeight = 1.0f;
    [Range(0, 1)] public float _ikRotationWeight = 1.0f;
    
    [Header("Hands IK Targets")]
    public Transform _rightHandTarget;
    public Transform _leftHandTarget;
    
    [Header("Current Weapon")]
    public WeaponIKData _currentWeaponData;
    
    // References
    [SerializeField]private Animator _animator;
    [SerializeField]private WeaponInventory _weaponInventory;
    
    // Transition
    private bool _isTransitioning = false;
    private float _transitionTime = 0.3f;
    private float _currentTransitionTime = 0f;
    private WeaponIKData _targetWeapon;



    private void Start()
    {
        ApplyWeaponIK(_weaponInventory.CurrentWeapon.RightHandPosition,
            _weaponInventory.CurrentWeapon.LeftHandPosition,
            _weaponInventory.CurrentWeapon.AnimatorOverrideController);
    }
    
    // Called by Unity's Animation system
    private void OnAnimatorIK(int layerIndex)
    {
        // If no animator or IK weight is 0, don't process IK
        if (_animator == null || _ikWeight <= 0f)
        {
            ResetIK();
            return;
        }
        
        // Handle IK transition
        if (_isTransitioning)
        {
            HandleIKTransition();
            return;
        }
            // Right Hand IK
            if (_rightHandTarget != null)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _ikWeight * _weaponInventory.CurrentWeapon.RightHandIKWeight);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, _ikRotationWeight * _weaponInventory.CurrentWeapon.RightHandIKWeight);
                _animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTarget.position);
                _animator.SetIKRotation(AvatarIKGoal.RightHand, _rightHandTarget.rotation);
            }
            
            // Left Hand IK
            if (_leftHandTarget != null)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _ikWeight * _weaponInventory.CurrentWeapon.LeftHandIKWeight);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _ikRotationWeight * _weaponInventory.CurrentWeapon.LeftHandIKWeight);
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTarget.position);
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandTarget.rotation);
            }
            
            // Apply any additional IK hints for elbows if specified
            /*if (_currentWeaponData._useElbowHints)
            {
                ApplyElbowHints();
            }*/
            //I need to kill the WeaponData SO
        else
        {
            ResetIK();
        }
    }
    
    // Set a new weapon and transition to its IK settings
    public void SetWeapon(WeaponIKData newWeapon)
    {
        if (newWeapon == null)
        {
            _currentWeaponData = null;
            ResetIK();
            return;
        }
        
        // Start a transition to the new weapon IK
        _targetWeapon = newWeapon;
        _isTransitioning = true;
        _currentTransitionTime = 0f;
        
        // Apply animation override controller if it exists
        if (newWeapon._animatorOverrideController != null)
        {
            _animator.runtimeAnimatorController = newWeapon._animatorOverrideController;
        }
    }
    
    // Apply weapon IK immediately without transition
    public void ApplyWeaponIK(Transform rightHandTarget, Transform leftHandTarget, AnimatorOverrideController animatorOverrideController)
    {
        _rightHandTarget = rightHandTarget;
        _leftHandTarget = leftHandTarget;
        if (animatorOverrideController != null)
        {
            _animator.runtimeAnimatorController = animatorOverrideController;
        }
    }
    
    // Handle smooth transition between IK poses
    private void HandleIKTransition()
    {
        _currentTransitionTime += Time.deltaTime;
        float t = Mathf.Clamp01(_currentTransitionTime / _transitionTime);
        
        // If transition is complete, set new weapon and exit transition mode
        if (t >= 1.0f)
        {
            ApplyWeaponIK(_weaponInventory.CurrentWeapon.RightHandPosition,
                _weaponInventory.CurrentWeapon.LeftHandPosition,
                _weaponInventory.CurrentWeapon.AnimatorOverrideController);
            return;
        }
        
        // Right Hand IK
        if (_rightHandTarget != null)
        {
            float rightHandWeightLerp = Mathf.Lerp(_currentWeaponData != null ? _currentWeaponData._rightHandWeight : 0f, 
                                                  _targetWeapon._rightHandWeight, t);
            
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _ikWeight * rightHandWeightLerp);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, _ikRotationWeight * rightHandWeightLerp);
            _animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTarget.position);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, _rightHandTarget.rotation);
        }
        
        // Left Hand IK
        if (_leftHandTarget != null)
        {
            float leftHandWeightLerp = Mathf.Lerp(_currentWeaponData != null ? _currentWeaponData._leftHandWeight : 0f, 
                                                 _targetWeapon._leftHandWeight, t);
            
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _ikWeight * leftHandWeightLerp);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _ikRotationWeight * leftHandWeightLerp);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTarget.position);
            _animator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandTarget.rotation);
        }
    }
    
    // Apply elbow hints for better arm positioning
    private void ApplyElbowHints()
    {
        if (_currentWeaponData._rightElbowHint != null)
        {
            _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, _currentWeaponData._rightElbowHintWeight);
            _animator.SetIKHintPosition(AvatarIKHint.RightElbow, _currentWeaponData._rightElbowHint.position);
        }
        
        if (_currentWeaponData._leftElbowHint != null)
        {
            _animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, _currentWeaponData._leftElbowHintWeight);
            _animator.SetIKHintPosition(AvatarIKHint.LeftElbow, _currentWeaponData._leftElbowHint.position);
        }
    }
    
    // Reset all IK weights
    private void ResetIK()
    {
        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0);
        _animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0);
    }
}
