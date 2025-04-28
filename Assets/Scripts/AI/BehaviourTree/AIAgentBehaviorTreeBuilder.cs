using UnityEngine;
using UnityEngine.AI;

namespace ProjectDark.BehaviorTree
{ 
    public static class AIAgentBehaviorTreeBuilder {
    public static Node BuildBehaviorTree(AIAgent agent, 
        NavMeshAgent navAgent, 
        AnimationHandler animator, 
        Stats stats,
        Transform[] waypoints,
        AIAgent.EnemyType type,
        float aggressiveRange,
        float attackRange,
        BallisticProjectile projectilePrefab = null,
        Transform muzzlePoint = null,
        ParticleSystem muzzleFlash = null,
        AudioClip[] audioClips = null,
        float damage = 5f,
        float fireRate = 1.2f,
        float rotationSpeed = 5f,
        float aimingThreshold = 10f)
    {
        // Root selector
        Selector rootSelector = new Selector();
        
        // Combat branch
        Sequence combatSequence = new Sequence();
        combatSequence.AddChild(new IsAliveNode(stats));
        combatSequence.AddChild(new IsPlayerInRangeNode(agent.transform, aggressiveRange));
        
        // Attack selector based on type
        Selector attackSelector = new Selector();
        
        // Melee attack sequence
        if (type == AIAgent.EnemyType.Melee)
        {
            Sequence meleeSequence = new Sequence();
            meleeSequence.AddChild(new IsPlayerInRangeNode(agent.transform, attackRange));
            meleeSequence.AddChild(new MeleeAttackNode(animator, navAgent));
            attackSelector.AddChild(meleeSequence);
        }
        // Ranged attack sequence
        else if (type == AIAgent.EnemyType.Ranged)
        { 
            Sequence rangedSequence = new Sequence();
            rangedSequence.AddChild(new IsPlayerInRangeNode(agent.transform, attackRange));
            rangedSequence.AddChild(new RangedAttackNode(agent.transform, muzzlePoint, animator,
                navAgent, projectilePrefab, muzzleFlash, audioClips, damage, fireRate, rotationSpeed, aimingThreshold));
            attackSelector.AddChild(rangedSequence);
        }
        
        // Chase if not in attack range
        attackSelector.AddChild(new ChasePlayerNode(navAgent, agent.transform, animator));
        combatSequence.AddChild(attackSelector);
        
        // Patrol branch
        PatrolNode patrolNode = new PatrolNode(navAgent, waypoints, animator);
        
        // Build tree
        rootSelector.AddChild(combatSequence);
        rootSelector.AddChild(patrolNode);
        
        return rootSelector;
    }
}
}
