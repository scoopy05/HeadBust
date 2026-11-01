using System.Collections.Generic;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// The three sacred return states of every node in a Behavior Tree.
    /// </summary>
    public enum NodeState
    {
        RUNNING, // Currently performing the action; check back next frame.
        SUCCESS, // Successfully completed the action or condition was met.
        FAILURE  // Failed to perform the action or condition was false.
    }

    /// <summary>
    /// The abstract base class that every single branch, selector, sequence,
    /// condition, and action in our AI's brain will inherit from.
    /// </summary>
    public abstract class Node
    {
        // Protected state so children can report their current status
        protected NodeState state;

        public NodeState State => state;

        // Optional list of children for composite branch nodes (Selectors / Sequences)
        protected List<Node> children = new List<Node>();

        public Node()
        {
            children = new List<Node>();
        }

        public Node(List<Node> children)
        {
            this.children = children;
        }

        public void Attach(Node node)
        {
            children.Add(node);
        }

        /// <summary>
        /// The heartbeat of the node. Every tick, this method evaluates the node's logic
        /// and returns either SUCCESS, FAILURE, or RUNNING.
        /// </summary>
        public abstract NodeState Evaluate();
    }
}
