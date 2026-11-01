using UnityEngine;
using UnityEngine.AI;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// Action Node: Commands the NavMeshAgent to sprint aggressively towards a vulnerable target,
    /// closing the distance to point-blank range to eliminate them.
    /// </summary>
    public class RushTargetAction : Node
    {
        private NavMeshAgent agent;
        private EnemySensory sensory;
        private float closeCombatDistance;
        private float sprintSpeed;
        private float normalSpeed;

        public RushTargetAction(NavMeshAgent agent, EnemySensory sensory, float closeCombatDistance = 6f, float sprintSpeed = 6.5f)
        {
            this.agent = agent;
            this.sensory = sensory;
            this.closeCombatDistance = closeCombatDistance;
            this.sprintSpeed = sprintSpeed;
            if (agent != null)
            {
                this.normalSpeed = agent.speed;
            }
        }

        public override NodeState Evaluate()
        {
            if (agent == null || sensory == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            Transform target = sensory.currentTarget;

            // Target lost or dead
            if (target == null)
            {
                ResetSpeed();
                state = NodeState.FAILURE;
                return state;
            }

            Health targetHp = target.GetComponentInParent<Health>();
            if (targetHp != null && targetHp.IsDead)
            {
                ResetSpeed();
                state = NodeState.FAILURE;
                return state;
            }

            // Sprint towards target
            agent.speed = sprintSpeed;
            agent.SetDestination(target.position);

            float distance = Vector3.Distance(agent.transform.position, target.position);

            // Once we have closed in to point-blank combat range
            if (distance <= closeCombatDistance)
            {
                ResetSpeed();
                state = NodeState.SUCCESS;
                return state;
            }

            // Still sprinting forward
            state = NodeState.RUNNING;
            return state;
        }

        private void ResetSpeed()
        {
            if (agent != null && normalSpeed > 0f)
            {
                agent.speed = normalSpeed;
            }
        }
    }
}
