using UnityEngine;
using UnityEngine.AI;

namespace ProjectDark.BehaviorTree
{
	public class PatrolNode : Node
	{
		private readonly NavMeshAgent _agent;
		private readonly Transform[] _waypoints;
		private readonly AnimationHandler _animator;
		private int _currentWaypointIndex;
		private bool _isWaiting;
		private float _waitTimer;
		private const float _waitDuration = 3f;

		public PatrolNode(NavMeshAgent agent, Transform[] waypoints, AnimationHandler animator)
		{
			_agent = agent;
			_waypoints = waypoints;
			_animator = animator;
		}

		public override NodeStatus Execute()
		{
			if (_isWaiting)
			{
				_waitTimer += Time.deltaTime;
				if (_waitTimer >= _waitDuration)
				{
					_isWaiting = false;
					_waitTimer = 0f;
				}
				return _status = NodeStatus.Running;
			}

			if (!_agent.isStopped)
				_animator.SetFloat_Speed("Speed", _agent.velocity.magnitude, 0.2f, Time.deltaTime);

			if (_agent.remainingDistance <= 0.1f && !_agent.pathPending)
			{
				_isWaiting = true;
				_currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
				return _status = NodeStatus.Running;
			}

			_agent.isStopped = false;
			_agent.SetDestination(_waypoints[_currentWaypointIndex].position);
			return _status = NodeStatus.Running;
		}
	}

}
