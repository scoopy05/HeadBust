using UnityEngine;
using UnityEngine.AI;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// Action Node: Roams the arena looking for enemies.
    /// Uses NavMesh.SamplePosition to pick guaranteed walkable points on the ground,
    /// commands the NavMeshAgent to walk there, pauses briefly to scout, then picks another point.
    /// </summary>
    public class PatrolAction : Node
    {
        private NavMeshAgent agent;
        private float patrolRadius;
        private float waitTime;
        private float waitCounter = 0f;
        private bool destinationSet = false;

        public PatrolAction(NavMeshAgent agent, float patrolRadius = 20f, float waitTime = 2f)
        {
            this.agent = agent;
            this.patrolRadius = patrolRadius;
            this.waitTime = waitTime;
        }

        public override NodeState Evaluate()
        {
            if (agent == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // 1. If we don't have a destination yet, find a valid spot on the NavMesh
            if (!destinationSet)
            {
                if (TryGetRandomNavMeshPoint(out Vector3 targetPoint))
                {
                    agent.SetDestination(targetPoint);
                    destinationSet = true;
                    waitCounter = 0f;
                    state = NodeState.RUNNING;
                    return state;
                }
                else
                {
                    state = NodeState.FAILURE;
                    return state;
                }
            }

            // 2. If walking towards destination, check if we arrived
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.6f)
            {
                // Pause and scout the area
                waitCounter += Time.deltaTime;

                if (waitCounter >= waitTime)
                {
                    // Finished waiting, ready to pick a new destination next tick
                    destinationSet = false;
                    state = NodeState.SUCCESS;
                    return state;
                }

                state = NodeState.RUNNING;
                return state;
            }

            // Still walking towards patrol point
            state = NodeState.RUNNING;
            return state;
        }

        /// <summary>
        /// Projects a random point in space onto the nearest valid, walkable NavMesh polygon.
        /// Prevents AI from picking points inside walls or outside the map boundaries.
        /// </summary>
        private bool TryGetRandomNavMeshPoint(out Vector3 result)
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += agent.transform.position;
            randomDirection.y = agent.transform.position.y; // Keep roughly on same floor height

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }

            result = Vector3.zero;
            return false;
        }
    }
}
