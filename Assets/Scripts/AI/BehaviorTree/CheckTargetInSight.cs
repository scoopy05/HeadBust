using UnityEngine;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// A Condition node that checks if the bot's sensory system
    /// currently has a valid, living target locked in line-of-sight.
    /// </summary>
    public class CheckTargetInSight : Node
    {
        private EnemySensory sensory;

        // Constructor: Receives the bot's sensory eyes
        public CheckTargetInSight(EnemySensory sensory)
        {
            this.sensory = sensory;
        }

        public override NodeState Evaluate()
        {
            // If the sensory component is missing, fail safely
            if (sensory == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Check if sensory has a locked target
            Transform target = sensory.currentTarget;

            if (target != null)
            {
                // Double check: Is the target still alive?
                Health targetHealth = target.GetComponentInParent<Health>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    // Target is confirmed in sight and alive!
                    state = NodeState.SUCCESS;
                    return state;
                }
            }

            // No target spotted, or target is dead
            state = NodeState.FAILURE;
            return state;
        }
    }
}
