using System.Collections.Generic;

namespace ProjectDark.BehaviorTree
{
	public abstract class CompositeNode : Node
	{
		protected List<Node> _children = new List<Node>();
        
		public void AddChild(Node child)
		{
			_children.Add(child);
		}
	}
}
