namespace ProjectDark.BehaviorTree
{
	public class Inverter : Decorator
	{
		public Inverter(Node child) : base(child) { }
        
		public override NodeStatus Execute()
		{
			_status = _child.Execute();
			if (_status == NodeStatus.Success) return _status = NodeStatus.Failure;
			if (_status == NodeStatus.Failure) return _status = NodeStatus.Success;
			return _status;
		}
	}
}
