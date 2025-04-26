namespace ProjectDark.BehaviorTree
{
	public abstract class Decorator : Node
	{
		protected Node _child;
        
		public Decorator(Node child)
		{
			_child = child;
		}
	}
}