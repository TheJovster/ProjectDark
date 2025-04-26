namespace ProjectDark.BehaviorTree
{
	public class BehaviorTreeManager
	{
		private Node _root;
        
		public void SetRootNode(Node root)
		{
			_root = root;
		}
        
		public void Tick()
		{
			_root?.Execute();
		}
	}
}
