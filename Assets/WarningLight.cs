using System;
using UnityEngine;

public class WarningLight : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 4.0f;

    private void Update()
    {
        transform.Rotate(Vector3.up * _rotationSpeed * Time.deltaTime);
    }
}
