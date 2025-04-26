namespace ProjectDark.BehaviorTree
{
	public class Sequence : CompositeNode
	{
		public override NodeStatus Execute()
		{
			foreach (var child in _children)
			{
				_status = child.Execute();
				if (_status != NodeStatus.Success) return _status;
			}
			return _status = NodeStatus.Success;
		}
	}
}
