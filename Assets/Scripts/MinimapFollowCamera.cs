using System;
using TMPro;
using UnityEngine;

public class MinimapFollowCamera : MonoBehaviour
{
    private Transform _player;
    [SerializeField] private Vector3 _offset;
    
    void Update()
    {
        if (_player)
        {
            transform.position = _player.position + _offset;
        }

    }

    public void SetPlayer(GameObject player)
    {
        _player = player.transform;
    }
}
