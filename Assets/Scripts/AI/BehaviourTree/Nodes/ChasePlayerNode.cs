using UnityEngine;
using UnityEngine.AI;

namespace ProjectDark.BehaviorTree
{
	public class ChasePlayerNode : Node
	{
		private readonly NavMeshAgent _agent;
		private readonly Transform _transform;
		private readonly AnimationHandler _animator;

		public ChasePlayerNode(NavMeshAgent agent, Transform transform, AnimationHandler animator)
		{
			_agent = agent;
			_transform = transform;
			_animator = animator;
		}

		public override NodeStatus Execute()
		{
			_agent.isStopped = false;
			_agent.SetDestination(GameManager.Instance.PlayerInstance.transform.position);
			_animator.SetFloat_Speed("Speed", _agent.velocity.magnitude, 0.2f, Time.deltaTime);
            
			Vector3 targetPos = GameManager.Instance.PlayerInstance.transform.position;
			Vector3 direction = new Vector3(targetPos.x, _transform.position.y, targetPos.z) - _transform.position;
			if (direction != Vector3.zero)
				_transform.rotation = Quaternion.Slerp(_transform.rotation, 
					Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            
			return _status = NodeStatus.Running;
		}
	}
}
