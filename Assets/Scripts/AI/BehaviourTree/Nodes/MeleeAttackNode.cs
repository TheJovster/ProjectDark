using UnityEngine;
using UnityEngine.AI;

namespace ProjectDark.BehaviorTree
{
	public class MeleeAttackNode : Node
	{
		private readonly AnimationHandler _animator;
		private readonly NavMeshAgent _agent;

		public MeleeAttackNode(AnimationHandler animator, NavMeshAgent agent)
		{
			_animator = animator;
			_agent = agent;
		}

		public override NodeStatus Execute()
		{
			_agent.isStopped = true;
			_animator.SetFloat_Speed("Speed", 0f, 0.2f, Time.deltaTime);
			_animator.TriggerAttack();
			return _status = NodeStatus.Success;
		}
	}
}
