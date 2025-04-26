using UnityEngine;
using UnityEngine.AI;

namespace ProjectDark.BehaviorTree
{
    public class PatrolNode : Node
    {
        private readonly NavMeshAgent _agent;
        private readonly Transform[] _waypoints;
        private readonly AnimationHandler _animator;
        private int _currentWaypointIndex;
        private bool _isWaiting;
        private float _waitTimer;
        private const float _waitDuration = 3f;
        private bool _pathPending;

        public PatrolNode(NavMeshAgent agent, Transform[] waypoints, AnimationHandler animator)
        {
            _agent = agent;
            _waypoints = waypoints;
            _animator = animator;
        }

        public override NodeStatus Execute()
        {
            // Safety checks
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) 
                return _status = NodeStatus.Failure;
            
            if (_waypoints == null || _waypoints.Length == 0) 
                return _status = NodeStatus.Failure;
            
            // Ensure waypoint index is valid
            if (_currentWaypointIndex >= _waypoints.Length) 
                _currentWaypointIndex = 0;
            
            // Waiting at waypoint
            if (_isWaiting)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= _waitDuration)
                {
                    _isWaiting = false;
                    _waitTimer = 0f;
                }
                _animator.SetFloat_Speed("Speed", 0f, 0.2f, Time.deltaTime);
                return _status = NodeStatus.Running;
            }

            // Check if we need to set a new destination
            if (!_agent.hasPath || !_pathPending)
            {
                Vector3 destination = _waypoints[_currentWaypointIndex].position;
                
                // Check if destination is on NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(destination, out hit, 2.0f, NavMesh.AllAreas))
                {
                    if (_agent.SetDestination(hit.position))
                    {
                        _agent.isStopped = false;
                        _pathPending = true;
                    }
                    else
                    {
                        return _status = NodeStatus.Failure;
                    }
                }
                else
                {
                    // Destination not on NavMesh, skip to next waypoint
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                    return _status = NodeStatus.Running;
                }
            }
            
            // Update animation
            if (!_agent.isStopped && _animator != null)
                _animator.SetFloat_Speed("Speed", _agent.velocity.magnitude, 0.2f, Time.deltaTime);

            // Check if we've reached the destination
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
            {
                // Reached waypoint, start waiting
                _isWaiting = true;
                _pathPending = false;
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            }

            return _status = NodeStatus.Running;
        }
    }

}
