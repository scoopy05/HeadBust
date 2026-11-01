using UnityEngine;
using UnityEngine.AI;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// Action Node: Tactically breaks line-of-sight from the attacker.
    /// Finds nearby walls/obstacles, calculates the hidden side opposite the threat,
    /// verifies line-of-sight is blocked, and commands the NavMeshAgent to take cover.
    /// </summary>
    public class SeekCoverAction : Node
    {
        private NavMeshAgent agent;
        private EnemySensory sensory;
        private LayerMask obstacleMask;
        private float searchRadius;
        private float coverOffset;

        private Vector3 currentCoverDestination;
        private bool isMovingToCover = false;

        public SeekCoverAction(NavMeshAgent agent, EnemySensory sensory, LayerMask obstacleMask, float searchRadius = 25f, float coverOffset = 2.5f)
        {
            this.agent = agent;
            this.sensory = sensory;
            this.obstacleMask = obstacleMask;
            this.searchRadius = searchRadius;
            this.coverOffset = coverOffset;
        }

        public override NodeState Evaluate()
        {
            if (agent == null || sensory == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            Transform threat = sensory.currentTarget;

            // If there's no active threat attacking us, no need to seek cover
            if (threat == null)
            {
                isMovingToCover = false;
                state = NodeState.SUCCESS;
                return state;
            }

            // 1. If we are already running toward cover, monitor arrival
            if (isMovingToCover)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.6f)
                {
                    // Arrived behind cover!
                    isMovingToCover = false;
                    state = NodeState.SUCCESS;
                    return state;
                }

                // Still sprinting toward cover
                state = NodeState.RUNNING;
                return state;
            }

            // 2. Find the best cover spot hidden from threat
            if (TryFindCoverPoint(threat.position, out Vector3 coverPoint))
            {
                currentCoverDestination = coverPoint;
                agent.SetDestination(coverPoint);
                isMovingToCover = true;
                state = NodeState.RUNNING;
                return state;
            }

            // If no wall cover found nearby, back away in the opposite direction
            Vector3 retreatDir = (agent.transform.position - threat.position).normalized;
            Vector3 retreatPoint = agent.transform.position + retreatDir * 10f;
            if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                isMovingToCover = true;
                state = NodeState.RUNNING;
                return state;
            }

            state = NodeState.FAILURE;
            return state;
        }

        /// <summary>
        /// Scans nearby colliders, calculates the shadow point behind each obstacle relative to the threat,
        /// and tests whether that point breaks line-of-sight.
        /// </summary>
        private bool TryFindCoverPoint(Vector3 threatPos, out Vector3 bestPoint)
        {
            Collider[] obstacles = Physics.OverlapSphere(agent.transform.position, searchRadius, obstacleMask);
            bestPoint = Vector3.zero;
            float closestDistance = Mathf.Infinity;

            Vector3 threatEyePos = threatPos + Vector3.up * 3f;

            foreach (Collider obs in obstacles)
            {
                // Don't use other characters or yourself as cover objects
                if (obs.CompareTag("Fighter") || obs.transform.root == agent.transform.root) continue;

                Bounds bounds = obs.bounds;

                // Direction vector pointing from threat through obstacle center
                Vector3 threatToObstacleDir = (bounds.center - threatPos).normalized;

                // Candidate cover spot: on the far side of the obstacle along that direction
                float obstacleExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
                Vector3 candidate = bounds.center + threatToObstacleDir * (obstacleExtent + coverOffset);
                candidate.y = agent.transform.position.y;

                // Check if candidate point is on the walkable NavMesh
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    Vector3 testCoverPos = hit.position + Vector3.up * 1.5f;
                    Vector3 dirToThreat = (threatEyePos - testCoverPos).normalized;
                    float distToThreat = Vector3.Distance(testCoverPos, threatEyePos);

                    // Raycast from cover point back to threat eyes.
                    // If the ray hits a wall, line of sight is broken: Safe Cover!
                    if (Physics.Raycast(testCoverPos, dirToThreat, out RaycastHit wallHit, distToThreat, obstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        if (wallHit.transform.root != sensory.currentTarget?.root)
                        {
                            float distFromAgent = Vector3.Distance(agent.transform.position, hit.position);
                            if (distFromAgent < closestDistance)
                            {
                                closestDistance = distFromAgent;
                                bestPoint = hit.position;
                            }
                        }
                    }
                }
            }

            return closestDistance < Mathf.Infinity;
        }
    }
}
