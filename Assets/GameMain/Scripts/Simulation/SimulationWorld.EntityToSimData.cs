using Components;
using Entity;
using Entity.EntityData;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Entity To Sim Data

        private static EnemySimData CreateEnemyInitialSimData(EnemyBase enemy, EnemyData enemyData)
        {
            Transform enemyTransform = enemy.CachedTransform;
            MovementComponent movementComponent = enemy.GetComponent<MovementComponent>();

            float speed = 0f;
            if (enemyData != null)
            {
                speed = enemyData.SpeedBase;
            }
            else if (movementComponent != null)
            {
                speed = movementComponent.Speed;
            }

            float attackRange = enemy.AttackRange > 0f
                ? enemy.AttackRange
                : DefaultAttackRange;

            return new EnemySimData
            {
                EntityId = enemy.Id,
                Position = enemyTransform.position,
                Forward = enemyTransform.forward,
                Rotation = enemyTransform.rotation,
                Speed = speed,
                AttackRange = attackRange,
                AvoidEnemyOverlap = movementComponent != null && movementComponent.AvoidEnemyOverlap,
                EnemyBodyRadius = movementComponent != null ? movementComponent.EnemyBodyRadius : 0.45f,
                SeparationIterations = movementComponent != null ? movementComponent.SeparationIterations : 2,
                TargetType = 0,
                State = EnemyStateIdle
            };
        }

        private static ProjectileSimData CreateProjectileInitialSimData(EntityBase projectileEntity, object userData)
        {
            Vector3 forward = projectileEntity.CachedTransform.forward;
            int ownerEntityId = 0;
            Vector3 velocity = Vector3.zero;
            float speed = 0f;
            float lifeTime = 0f;

            if (userData is EnemyProjectileData enemyProjectileData)
            {
                ownerEntityId = enemyProjectileData.OwnerEntityId;

                Vector3 direction = enemyProjectileData.Direction;
                direction.y = 0f;
                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    direction.Normalize();
                    forward = direction;
                }
                else if (forward.sqrMagnitude > Mathf.Epsilon)
                {
                    forward = forward.normalized;
                }
                else
                {
                    forward = Vector3.forward;
                }

                speed = Mathf.Max(0f, enemyProjectileData.Speed);
                velocity = forward * speed;
                lifeTime = Mathf.Max(0f, enemyProjectileData.LifeTime);
            }

            return new ProjectileSimData
            {
                EntityId = projectileEntity.Id,
                OwnerEntityId = ownerEntityId,
                Position = projectileEntity.CachedTransform.position,
                Forward = forward,
                Velocity = velocity,
                Speed = speed,
                LifeTime = lifeTime,
                Age = 0f,
                Active = true,
                RemainingLifetime = lifeTime,
                State = ProjectileStateActive
            };
        }

        private static PickupSimData CreatePickupInitialSimData(EntityBase pickupEntity)
        {
            return new PickupSimData
            {
                EntityId = pickupEntity.Id,
                Position = pickupEntity.CachedTransform.position,
                PickupRadius = 0.35f,
                State = 0
            };
        }

        #endregion
    }
}
