using UnityEngine;

namespace HeadBust.AI.BehaviorTree
{
    /// <summary>
    /// Action Node: Commands the bot to aim at its locked target,
    /// track them with smooth body rotation, and discharge weapon bursts.
    /// </summary>
    public class AimAndShootTarget : Node
    {
        private Transform botTransform;
        private EnemySensory sensory;
        private EnemyShooter shooter;
        private Transform headMount;
        private float rotationSpeed;

        public AimAndShootTarget(Transform botTransform, EnemySensory sensory, EnemyShooter shooter, Transform headMount = null, float rotationSpeed = 360f)
        {
            this.botTransform = botTransform;
            this.sensory = sensory;
            this.shooter = shooter;
            this.headMount = headMount;
            this.rotationSpeed = rotationSpeed;
        }

        public override NodeState Evaluate()
        {
            if (sensory == null || shooter == null || botTransform == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            Transform target = sensory.currentTarget;

            // Target lost or invalid
            if (target == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Target dead
            Health targetHp = target.GetComponentInParent<Health>();
            if (targetHp != null && targetHp.IsDead)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Target center point (around chest/head height)
            Vector3 targetAimPoint = target.position + Vector3.up * (sensory.eyeHeight * 0.7f);

            // 1. Smoothly rotate bot body (Yaw) towards target
            Vector3 flatDir = target.position - botTransform.position;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatDir);
                botTransform.rotation = Quaternion.RotateTowards(botTransform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // 2. Smoothly pitch head/gun up or down towards target if headMount provided
            if (headMount != null)
            {
                Vector3 toTarget = targetAimPoint - headMount.position;
                Quaternion lookRot = Quaternion.LookRotation(toTarget);
                // Extract local pitch only
                headMount.rotation = Quaternion.RotateTowards(headMount.rotation, lookRot, rotationSpeed * Time.deltaTime);
            }

            // 3. Fire at target if facing roughly towards them
            float angleToTarget = Vector3.Angle(botTransform.forward, flatDir.normalized);
            if (angleToTarget < 30f)
            {
                shooter.ShootAt(targetAimPoint);
            }

            // While actively engaging in combat, keep state RUNNING
            state = NodeState.RUNNING;
            return state;
        }
    }
}
