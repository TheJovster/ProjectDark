namespace ProjectDark.BehaviorTree
{
	public class IsAliveNode : Node
	{
		private readonly Stats _stats;

		public IsAliveNode(Stats stats)
		{
			_stats = stats;
		}

		public override NodeStatus Execute()
		{
			return _status = _stats.IsAlive ? NodeStatus.Success : NodeStatus.Failure;
		}
	}
}
