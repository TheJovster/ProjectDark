using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
	private float _damage;
	private Rigidbody _rigidBody;
	[SerializeField] private float _muzzleVelocity = 30.0f;
	[SerializeField] private float _lifeTime = 3.0f;
	private float _timeSinceSpawned = 0.0f;
	
	private void Awake()
	{
		_rigidBody = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		
	}

	public void SetDamage(float newAmount)
	{
		_damage = newAmount;
	}

	public void SetRotation(Vector3 position)
	{
		_rigidBody.AddForce(position * _muzzleVelocity, ForceMode.Impulse);
	}

	private void Update()
	{
		_timeSinceSpawned += Time.deltaTime;
		if (_timeSinceSpawned >= _lifeTime)
		{
			Destroy(this.gameObject);
		}
	}

	public void OnCollisionEnter(Collision other)
	{
		Destroy(this.gameObject);
	}
}
