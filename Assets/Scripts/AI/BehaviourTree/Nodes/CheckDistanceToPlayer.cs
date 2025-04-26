using UnityEngine;

namespace ProjectDark.BehaviorTree
{
	public class CheckDistanceToPlayer : Node
	{
		private readonly AIAgent _agent;
		private readonly float _targetDistance;
		private readonly bool _checkIfCloser;

		public CheckDistanceToPlayer(AIAgent agent, float distance, bool checkIfCloser = true)
		{
			_agent = agent;
			_targetDistance = distance;
			_checkIfCloser = checkIfCloser;
		}

		public override NodeStatus Execute()
		{
			float distance = Vector3.Distance(_agent.transform.position, 
				GameManager.Instance.PlayerInstance.transform.position);
            
			bool condition = _checkIfCloser ? 
				distance <= _targetDistance : 
				distance > _targetDistance;
            
			return _status = condition ? NodeStatus.Success : NodeStatus.Failure;
		}
	}
}
