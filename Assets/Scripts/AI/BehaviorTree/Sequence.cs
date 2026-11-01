using System.Collections.Generic;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// A Sequence evaluates its children from left to right like a checklist.
    /// If ANY child fails, the entire Sequence aborts immediately and returns FAILURE.
    /// If a child is RUNNING, it pauses and returns RUNNING.
    /// It ONLY returns SUCCESS if EVERY single child succeeds.
    /// (Think of this as a multi-step 'AND' plan).
    /// </summary>
    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        // A step in our plan failed! Abort immediately!
                        state = NodeState.FAILURE;
                        return state;

                    case NodeState.SUCCESS:
                        // This step succeeded! Move to the next step in the checklist.
                        continue;

                    case NodeState.RUNNING:
                        // This step is still working (e.g. running to cover). Keep waiting!
                        anyChildRunning = true;
                        state = NodeState.RUNNING;
                        return state;

                    default:
                        state = NodeState.SUCCESS;
                        return state;
                }
            }

            // If we looped through all children and NONE failed:
            state = anyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return state;
        }
    }
}
