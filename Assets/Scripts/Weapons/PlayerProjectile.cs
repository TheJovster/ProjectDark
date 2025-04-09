using UnityEngine;
using Biostart.Impact;
using Biostart.Blood;

public class PlayerProjectile : MonoBehaviour
{
	private float _damage;
	private Rigidbody _rigidBody;
	[SerializeField] private float _muzzleVelocity = 30.0f;
	[SerializeField] private float _lifeTime = 3.0f;
	private float _timeSinceSpawned = 0.0f;
	[SerializeField] private GameObject _impactEffect;
	
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
		if (other.gameObject.CompareTag("Enemy"))
		{
			RaycastHit hit;
			Vector3 rayOrigin = transform.position;
			Vector3 rayDirection = transform.forward;
			
			//the impact effects from the blood VFX package
			if(Physics.Raycast(rayOrigin, rayDirection, out hit))
			{
				ImpactEffect newEffect = other.gameObject.GetComponent<ImpactEffect>();
				newEffect.SpawnBloodEffect(hit.point, hit.normal);

				BloodProjector newBloodProjector = other.gameObject.GetComponent<BloodProjector>();
				newBloodProjector.AttachBloodProjector(hit.point, hit.normal, hit.collider);
			}
			other.gameObject.GetComponent<Stats>().TakeDamage(15.0f);
		}
		GameObject newImpacEffect = Instantiate(_impactEffect, transform.position, transform.rotation);
		Destroy(newImpacEffect, 1.5f);
		Destroy(this.gameObject);
	}
}
