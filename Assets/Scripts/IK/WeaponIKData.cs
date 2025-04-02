using UnityEngine;


[CreateAssetMenu(fileName = "New Weapon IK Data", menuName = "IK System/Weapon IK Data")]
public class WeaponIKData : ScriptableObject
{
    [Header("Weapon Settings")]
    public string _weaponName;
    
    [Header("Animation")]
    public AnimatorOverrideController _animatorOverrideController;
    
    
    [Header("Hand Weights")]
    [Range(0, 1)] public float _rightHandWeight = 1.0f;
    [Range(0, 1)] public float _leftHandWeight = 1.0f;
    
    [Header("Optional Custom IK Targets")]
    public Transform _rightHandIKTarget;
    public Transform _leftHandIKTarget;
    
    [Header("Elbow Hints")]
    public bool _useElbowHints = false;
    public Transform _rightElbowHint;
    public Transform _leftElbowHint;
    [Range(0, 1)] public float _rightElbowHintWeight = 0.5f;
    [Range(0, 1)] public float _leftElbowHintWeight = 0.5f;
}
