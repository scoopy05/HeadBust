using UnityEngine;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// Condition Node: Checks if the bot's currently locked target has low health.
    /// Triggers aggressive push/rush behaviors to finish off vulnerable opponents.
    /// </summary>
    public class CheckTargetWeak : Node
    {
        private EnemySensory sensory;
        private int weakThreshold;

        public CheckTargetWeak(EnemySensory sensory, int weakThreshold = 3)
        {
            this.sensory = sensory;
            this.weakThreshold = weakThreshold;
        }

        public override NodeState Evaluate()
        {
            if (sensory == null || sensory.currentTarget == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            Health targetHp = sensory.currentTarget.GetComponentInParent<Health>();
            if (targetHp != null && !targetHp.IsDead && targetHp.CurrentHits <= weakThreshold)
            {
                // Target is wounded and vulnerable!
                state = NodeState.SUCCESS;
                return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}
