namespace ProjectDark.BehaviorTree
{
	public class Selector : CompositeNode
	{
		public override NodeStatus Execute()
		{
			foreach (var child in _children)
			{
				_status = child.Execute();
				if (_status != NodeStatus.Failure) return _status;
			}
			return _status = NodeStatus.Failure;
		}
	}
}
