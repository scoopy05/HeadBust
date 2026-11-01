using System.Collections.Generic;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// A Selector evaluates its children from left to right.
    /// It returns SUCCESS or RUNNING on the FIRST child that succeeds or runs.
    /// It only returns FAILURE if EVERY child fails.
    /// (Think of this as a prioritized 'OR' condition).
    /// </summary>
    public class Selector : Node
    {
        public Selector() : base() { }
        public Selector(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        // This child failed. Continue the loop to try the next child!
                        continue;

                    case NodeState.SUCCESS:
                        // A child succeeded! Stop immediately and report SUCCESS.
                        state = NodeState.SUCCESS;
                        return state;

                    case NodeState.RUNNING:
                        // A child is still busy doing work! Stop and report RUNNING.
                        state = NodeState.RUNNING;
                        return state;

                    default:
                        continue;
                }
            }

            // If we looped through all children and every single one failed:
            state = NodeState.FAILURE;
            return state;
        }
    }
}
