using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
public class AIAgent : MonoBehaviour
{
    public enum BehaviorState
    {
        Idle,
        Patrol,
        Aggressive
    }

    public enum EnemyType
    {
        Melee,
        Ranged,
        Special
    }

    private Stats _stats;
    [Header("AI Agent Basics")]
    [SerializeField] private BehaviorState _currentState;
    [SerializeField] private EnemyType _type;
    
    [Header("Waypoints")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waitTimeAtWaypoint = 3.0f;
    private float _timeAtWaypoint = 0.0f; //probably not necessary
    //serializefield for testing, make purely private later
    [SerializeField]private int _currentWaypointIndex;
    [SerializeField]private int _nextWaypointIndex;
    
    //components
    private AnimationHandler _animationHandler;
    private NavMeshAgent _navMeshAgent;
    private void Awake()
    {
        _animationHandler = GetComponent<AnimationHandler>();
        _stats = GetComponent<Stats>();
    }

    private void Start()
    {
        foreach (Transform waypoint in _waypoints)
        {
            waypoint.parent = transform.parent;
        }
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _currentState = BehaviorState.Patrol;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentGameState == GameManager.GameState.Playing)
        {
            _animationHandler.ResumeAnimation();
            EvaluateState();
        }
        if (GameManager.Instance.CurrentGameState == GameManager.GameState.Paused)
        {
            _animationHandler.FreezeAnimation();
            _navMeshAgent.velocity = Vector3.zero;
            _navMeshAgent.isStopped = true;
        }
    }

    private void Patrol()
    {
        if (_stats.IsAlive)
        {
            _animationHandler.SetFloat_Speed("Speed", _navMeshAgent.velocity.magnitude, 0.2f, Time.deltaTime);
            _navMeshAgent.isStopped = false;
            GoToLocation(_waypoints[_currentWaypointIndex].position);
            if (_navMeshAgent.remainingDistance <= 0.1f)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex > _waypoints.Length - 1)
                {
                    _currentWaypointIndex = 0;
                }
                StartCoroutine(WaitAtWaypoint());
            }
        }
        else if (!_stats.IsAlive)
        {
            _navMeshAgent.velocity = Vector3.zero;
            _navMeshAgent.isStopped = true;
        }
    }

    private void Idle()
    {
        _navMeshAgent.velocity = Vector3.zero;
        _animationHandler.SetFloat_Speed("Speed", _navMeshAgent.velocity.magnitude, 0.2f,Time.deltaTime);
    }

    private void EvaluateState()
    {
        switch (_currentState)
        {
            case BehaviorState.Idle:
                Idle();
                break;
            case BehaviorState.Patrol:
                Patrol();
                break;
            case BehaviorState.Aggressive:
                AggressiveBehavior();
                break;
        }
    }
    
    private void GoToLocation(Vector3 position)
    {
        _navMeshAgent.destination = position;
    }

    public void SetBehaviorState(BehaviorState newState)
    {
        _currentState = newState;
    }

    private IEnumerator WaitAtWaypoint()
    {
        SetBehaviorState(BehaviorState.Idle);
        yield return new WaitForSeconds(_waitTimeAtWaypoint);
        SetBehaviorState(BehaviorState.Patrol);
    }

    public void AggressiveBehavior()
    {
        if (_type == EnemyType.Melee)
        {
            Debug.Log("Melee boi goes melee");
        }

        if (_type == EnemyType.Ranged)
        {
            Debug.Log("Shooty boi goes pew pew");
        }

        if (_type == EnemyType.Special)
        {
            Debug.Log("Boomy boi goes boom");
        }
    }
}
