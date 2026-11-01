using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using HeadBust.AI.BehaviorTree;

namespace HeadBust.AI
{
    /// <summary>
    /// The master AI brain that runs on each Enemy Bot.
    /// Inherits from Tree.cs, constructing and evaluating a priority-based
    /// Behavior Tree every frame.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemySensory))]
    [RequireComponent(typeof(EnemyShooter))]
    [RequireComponent(typeof(Health))]
    public class TacticalBotBT : HeadBust.AI.BehaviorTree.Tree
    {
        [Header("Body & Head References")]
        [Tooltip("The Head_Mount transform holding the gun (for vertical pitch aim)")]
        public Transform headMount;

        [Header("Tactical Thresholds")]
        [Tooltip("HP at or below which the bot flees to cover (default: 3 / 8)")]
        public int lowHealthThreshold = 3;
        [Tooltip("Enemy HP at or below which the bot rushes aggressively (default: 3 / 8)")]
        public int weakTargetThreshold = 3;

        [Header("Movement & Cover Settings")]
        [Tooltip("Layers that block line of sight (ProBuilder walls, obstacles)")]
        public LayerMask obstacleMask = ~0;
        [Tooltip("How far the bot wanders during peaceful patrol")]
        public float patrolRadius = 22f;
        [Tooltip("How long the bot scouts a patrol point before moving on")]
        public float patrolWaitTime = 2f;
        [Tooltip("Sprint speed when aggressively rushing weak targets")]
        public float rushSprintSpeed = 6.5f;

        private NavMeshAgent agent;
        private EnemySensory sensory;
        private EnemyShooter shooter;
        private Health botHealth;

        protected override Node SetupTree()
        {
            // Auto-acquire required components on this GameObject
            agent = GetComponent<NavMeshAgent>();
            sensory = GetComponent<EnemySensory>();
            shooter = GetComponent<EnemyShooter>();
            botHealth = GetComponent<Health>();

            // If headMount wasn't assigned in Inspector, search children
            if (headMount == null)
            {
                Transform foundHead = transform.Find("Head_Mount");
                if (foundHead != null) headMount = foundHead;
            }

            // =========================================================================
            // BRANCH 1: SURVIVAL (Highest Priority)
            // If bot health is critical -> take cover behind nearest wall!
            // =========================================================================
            Sequence survivalBranch = new Sequence(new List<Node>
            {
                new CheckHealthLow(botHealth, lowHealthThreshold),
                new SeekCoverAction(agent, sensory, obstacleMask)
            });

            // =========================================================================
            // BRANCH 2: RUSH WEAK TARGET (Aggressive Opportunism)
            // If enemy is in sight AND their health is low -> sprint in & eliminate!
            // =========================================================================
            Sequence rushWeakBranch = new Sequence(new List<Node>
            {
                new CheckTargetInSight(sensory),
                new CheckTargetWeak(sensory, weakTargetThreshold),
                new RushTargetAction(agent, sensory, closeCombatDistance: 6f, sprintSpeed: rushSprintSpeed),
                new AimAndShootTarget(transform, sensory, shooter, headMount)
            });

            // =========================================================================
            // BRANCH 3: STANDARD COMBAT (Medium Priority)
            // If enemy is in sight -> track with body/head and shoot!
            // =========================================================================
            Sequence combatBranch = new Sequence(new List<Node>
            {
                new CheckTargetInSight(sensory),
                new AimAndShootTarget(transform, sensory, shooter, headMount)
            });

            // =========================================================================
            // BRANCH 4: PATROL / EXPLORE (Lowest Priority / Fallback)
            // No enemies in sight -> roam the arena exploring walkable NavMesh points.
            // =========================================================================
            PatrolAction patrolBranch = new PatrolAction(agent, patrolRadius, patrolWaitTime);

            // =========================================================================
            // ROOT SELECTOR (Evaluates top-down, executing highest valid priority)
            // =========================================================================
            Node root = new Selector(new List<Node>
            {
                survivalBranch,
                rushWeakBranch,
                combatBranch,
                patrolBranch
            });

            return root;
        }
    }
}
