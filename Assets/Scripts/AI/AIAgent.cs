using System.Collections;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.AI;
public class AIAgent : MonoBehaviour
{
    public enum BehaviorState
    {
        Idle,
        Patrol,
        Aggressive,
        Dead    
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
    [SerializeField] private float _minimumDistanceToAggressive = 10.0f;
    [SerializeField] private float _minimumAttackDistance = 5.0f;
    private bool _bIsAggressive = false;
    private string _sIsAggressive = "IsAggressive";
    
    [Header("Waypoints")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waitTimeAtWaypoint = 3.0f;
    private float _timeAtWaypoint = 0.0f; //probably not necessary
    //serializefield for testing, make purely private later
    [SerializeField]private int _currentWaypointIndex;
    [SerializeField]private int _nextWaypointIndex;

    [Header("Combat")] 
    [SerializeField] private float _attackRange = 2.0f;
    [SerializeField] private float _attackDamage = 10.0f;
    [SerializeField] private LayerMask _attackLayer;
    [SerializeField] private BallisticProjectile _projectilePrefab;
    [SerializeField] private Transform _muzzlePoint;
    private float _timeSinceLastShot = 0.0f;
    [SerializeField] private float _fireRate = 1.2f;

    [SerializeField] private float _damage = 5.0f;
    //components
    private AnimationHandler _animationHandler;
    private NavMeshAgent _navMeshAgent;
    
    private void Awake()
    {
        _animationHandler = GetComponentInChildren<AnimationHandler>();
        _stats = GetComponent<Stats>();
        
        // Subscribe to death event
        /*if (_stats != null)
        {
            _stats.OnDeathEvent.AddListener(HandleDeath);
            _stats.OnDamageEvent.AddListener(HandleDamage);
        }*/
        //the above might be useful later
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
        _timeSinceLastShot += Time.deltaTime;
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
        if (Vector3.Distance(transform.position, GameManager.Instance.PlayerInstance.transform.position) <=
            _minimumDistanceToAggressive)
        {
                SetBehaviorState(BehaviorState.Aggressive);
        }
    }

    private void Idle()
    {
        _navMeshAgent.velocity = Vector3.zero;
        _animationHandler.SetFloat_Speed("Speed", _navMeshAgent.velocity.magnitude, 0.2f,Time.deltaTime);

        if (Vector3.Distance(transform.position, GameManager.Instance.PlayerInstance.transform.position) <=
            _minimumDistanceToAggressive)
        {
            SetBehaviorState(BehaviorState.Aggressive);
        }
    }

    public void DisableNavMeshAgent()
    {
        _navMeshAgent.velocity = Vector3.zero;
        _navMeshAgent.isStopped = true;
        _navMeshAgent.enabled = false;
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
            case BehaviorState.Dead:
                DisableNavMeshAgent();
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
        switch (_type)
        {
            case EnemyType.Melee:
                AggressiveBehaviourMelee();
                break;
            case EnemyType.Ranged:
                AggressiveBehaviourRanged();
                break;
            case EnemyType.Special:
                AggressiveBehaviourSpecial();
                break;
        }
    }

    private void AggressiveBehaviourMelee()
    {
        Debug.Log("Melee boi is aggressive");
        transform.LookAt(new Vector3(
            GameManager.Instance.PlayerInstance.transform.position.x,
            transform.position.y,
            GameManager.Instance.PlayerInstance.transform.position.z));
        _navMeshAgent.isStopped = false;
        _navMeshAgent.destination = GameManager.Instance.PlayerInstance.transform.position;
        _animationHandler.SetFloat_Speed("Speed", Mathf.Abs(_navMeshAgent.velocity.magnitude), 0.2f, Time.deltaTime);
        if (Vector3.Distance(transform.position, GameManager.Instance.PlayerInstance.transform.position) >
            _minimumDistanceToAggressive)
        {
            SetBehaviorState(BehaviorState.Idle);
        }
        if (Vector3.Distance(transform.position, GameManager.Instance.PlayerInstance.transform.position) <=
            _minimumAttackDistance)
        {
            _navMeshAgent.isStopped = true;
            _animationHandler.SetFloat_Speed("Speed", 0.0f, 0.2f, Time.deltaTime);
            _animationHandler.TriggerAttack();
        }
    }

    private void AggressiveBehaviourRanged()
    {
        _bIsAggressive = true;
        Debug.Log("Shotty boi is aggressive");
        transform.LookAt(new Vector3(
            GameManager.Instance.PlayerInstance.transform.position.x,
            transform.position.y,
            GameManager.Instance.PlayerInstance.transform.position.z));
        _animationHandler.SetAggressive(_sIsAggressive, _bIsAggressive);
        _navMeshAgent.isStopped = false;
        _navMeshAgent.destination = GameManager.Instance.PlayerInstance.transform.position;
        _animationHandler.SetFloat_Speed("Speed", Mathf.Abs(_navMeshAgent.velocity.magnitude), 0.2f, Time.deltaTime);
        if (Vector3.Distance(transform.position, 
                GameManager.Instance.PlayerInstance.transform.position) >
            _minimumDistanceToAggressive)
        {
            SetBehaviorState(BehaviorState.Patrol);
        }
        if (Vector3.Distance(transform.position, GameManager.Instance.PlayerInstance.transform.position) <=
            _minimumAttackDistance)
        {
            _navMeshAgent.isStopped = true;
            _animationHandler.SetFloat_Speed("Speed", 0.0f, 0.2f, Time.deltaTime);
            _animationHandler.TriggerAttack();
            AttackBehavior(_type);
        }
    }

    private void AggressiveBehaviourSpecial()
    {
        Debug.Log("Special boi is aggressive and explodey");
    }

    public void AttackBehavior(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Melee:
                break;
            case EnemyType.Ranged:
                if (_timeSinceLastShot <= _fireRate || !_stats.IsAlive) return;
                //audio effects
                //visual effects
                BallisticProjectile projectileInstance =
                Instantiate(_projectilePrefab, _muzzlePoint.position, _muzzlePoint.rotation);
                projectileInstance.SetDamageToDeal(_damage);
                _muzzlePoint.LookAt(new Vector3(GameManager.Instance.PlayerInstance.transform.position.x, _muzzlePoint.position.y, GameManager.Instance.PlayerInstance.transform.position.z));
                //muzzle point can not be set to look at
                //need a function that smoothly aims at the player.
                //animations look goofy
                projectileInstance.Fire(_muzzlePoint.forward);
                Destroy(projectileInstance, 5.0f);
                _timeSinceLastShot = 0;
                break;
            case EnemyType.Special:
                break;
        }
        
    }
}
