using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ProjectDark.BehaviorTree;

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

    [Header("AI Configuration")]
    [SerializeField] private EnemyType _type;
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _aggressiveRange = 10f;
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private BallisticProjectile _projectilePrefab;
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private AudioClip[] _audioClips;
    [SerializeField] private float _damage = 5f;
    [SerializeField] private float _fireRate = 1.2f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _aimingThreshold = 10f;

    private BehaviorTreeManager _behaviorTree;
    private NavMeshAgent _navMeshAgent;
    private AnimationHandler _animationHandler;
    private Stats _stats;

    private void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _animationHandler = GetComponentInChildren<AnimationHandler>();
        _stats = GetComponent<Stats>();
       
        InitializeBehaviorTree();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
        {
            _animationHandler.StopAnimator();
            return;
        }
        else if(GameManager.Instance.CurrentGameState == GameManager.GameState.Playing)
        {
            _animationHandler.ResumeAnimation();
        }
        _behaviorTree?.Tick();
    }

    private void InitializeBehaviorTree()
    {
        _behaviorTree = new BehaviorTreeManager();
        
        Node rootNode = AIAgentBehaviorTreeBuilder.BuildBehaviorTree(
            this,
            _navMeshAgent,
            _animationHandler,
            _stats,
            _waypoints,
            _type,
            _aggressiveRange,
            _attackRange,
            _projectilePrefab,
            _muzzlePoint,
            _muzzleFlash,
            _audioClips,
            _damage,
            _fireRate,
            _rotationSpeed,
            _aimingThreshold
        );
        
        _behaviorTree.SetRootNode(rootNode);
    }
}
