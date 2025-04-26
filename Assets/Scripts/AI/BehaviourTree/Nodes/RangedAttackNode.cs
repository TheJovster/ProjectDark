using UnityEngine;
using UnityEngine.AI;

namespace ProjectDark.BehaviorTree
{
	public class RangedAttackNode : Node
    {
        private readonly Transform _transform;
        private readonly Transform _muzzlePoint;
        private readonly AnimationHandler _animator;
        private readonly NavMeshAgent _agent;
        private readonly BallisticProjectile _projectilePrefab;
        private readonly ParticleSystem _muzzleFlash;
        private readonly AudioClip[] _audioClips;
        private readonly float _damage;
        private readonly float _fireRate;
        private readonly float _rotationSpeed;
        private readonly float _aimingThreshold;
        private float _timeSinceLastShot;

        public RangedAttackNode(Transform transform, Transform muzzlePoint, AnimationHandler animator, 
            NavMeshAgent agent, BallisticProjectile projectilePrefab, ParticleSystem muzzleFlash,
            AudioClip[] audioClips, float damage, float fireRate, float rotationSpeed, float aimingThreshold)
        {
            _transform = transform;
            _muzzlePoint = muzzlePoint;
            _animator = animator;
            _agent = agent;
            _projectilePrefab = projectilePrefab;
            _muzzleFlash = muzzleFlash;
            _audioClips = audioClips;
            _damage = damage;
            _fireRate = fireRate;
            _rotationSpeed = rotationSpeed;
            _aimingThreshold = aimingThreshold;
        }

        public override NodeStatus Execute()
        {
            _agent.isStopped = true;
            _animator.SetFloat_Speed("Speed", 0f, 0.2f, Time.deltaTime);
            _animator.SetAggressive("IsAggressive", true);

            // Aim at player
            Vector3 direction = GameManager.Instance.PlayerInstance.transform.position - _transform.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, lookRotation, Time.deltaTime * _rotationSpeed);
            }

            // Check if aimed
            float angle = Vector3.Angle(_transform.forward, direction);
            if (angle > _aimingThreshold) return _status = NodeStatus.Running;

            // Fire if ready
            _timeSinceLastShot += Time.deltaTime;
            if (_timeSinceLastShot < _fireRate) return _status = NodeStatus.Running;

            _animator.TriggerAttack();
            AudioManager.Instance.PlayEffectHalfVolume(_audioClips[UnityEngine.Random.Range(0, _audioClips.Length)]);
            _muzzleFlash.Play();
            
            BallisticProjectile projectile = GameObject.Instantiate(_projectilePrefab, _muzzlePoint.position, _muzzlePoint.rotation);
            projectile.SetDamageToDeal(_damage);
            _muzzlePoint.LookAt(new Vector3(GameManager.Instance.PlayerInstance.transform.position.x, 
                _muzzlePoint.position.y, GameManager.Instance.PlayerInstance.transform.position.z));
            projectile.Fire(_muzzlePoint.forward);
            GameObject.Destroy(projectile, 5.0f);
            
            _timeSinceLastShot = 0;
            return _status = NodeStatus.Success;
        }
    }
}
