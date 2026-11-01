using UnityEngine;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// The MonoBehaviour runner that lives on the AI GameObject.
    /// It initializes the Behavior Tree and ticks the root node every frame.
    /// </summary>
    public abstract class Tree : MonoBehaviour
    {
        // The topmost node of the entire behavior tree
        protected Node root = null;

        protected virtual void Start()
        {
            // Build the brain when the game begins!
            root = SetupTree();
        }

        protected virtual void Update()
        {
            // The heartbeat: Pulse the tree down from the root every frame!
            if (root != null)
            {
                root.Evaluate();
            }
        }

        /// <summary>
        /// Every enemy bot must override this to assemble its specific
        /// combination of Selectors, Sequences, and Actions!
        /// </summary>
        protected abstract Node SetupTree();
    }
}
