using System;
using TMPro;
using UnityEngine;

public class MinimapFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Vector3 _offset;

    private void Awake()
    {
        this.transform.parent = null;
    }

    void Update()
    {
        transform.position = _player.position + _offset;
    }
}
