using UnityEngine;
using UnityEngine.AI;

namespace ProjectDark.BehaviorTree
{
	public class IsPlayerInRangeNode : Node
	{
		private readonly Transform _transform;
		private readonly float _range;

		public IsPlayerInRangeNode(Transform transform, float range)
		{
			_transform = transform;
			_range = range;
		}

		public override NodeStatus Execute()
		{
			float distance = Vector3.Distance(_transform.position, 
				GameManager.Instance.PlayerInstance.transform.position);
			return _status = distance <= _range ? NodeStatus.Success : NodeStatus.Failure;
		}
	}
}
