namespace ProjectDark.BehaviorTree
{
	public enum NodeStatus { Running, Success, Failure }

	public abstract class Node
	{
		protected NodeStatus _status;
		public NodeStatus Status => _status;
		public abstract NodeStatus Execute();
	}
}

