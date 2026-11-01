using UnityEngine;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// Condition Node: Checks if this bot's health has dropped below a critical threshold.
    /// Used at the top of the Survival branch to trigger retreat or cover-seeking.
    /// </summary>
    public class CheckHealthLow : Node
    {
        private Health botHealth;
        private int lowHealthThreshold;

        public CheckHealthLow(Health botHealth, int lowHealthThreshold = 3)
        {
            this.botHealth = botHealth;
            this.lowHealthThreshold = lowHealthThreshold;
        }

        public override NodeState Evaluate()
        {
            if (botHealth == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // If health is at or below threshold (e.g. 3 hits left out of 8)
            if (botHealth.CurrentHits <= lowHealthThreshold && !botHealth.IsDead)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}
