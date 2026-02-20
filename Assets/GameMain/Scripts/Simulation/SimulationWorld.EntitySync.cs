using Components;
using Entity;
using Entity.EntityData;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace Simulation
{
    public partial class SimulationWorld
    {
        public sealed class EntitySync
        {
            private const string EnemyGroupName = "Enemy";
            private const string DropGroupName = "Drop";
            private const string BulletGroupName = "Bullet";
            private const string ProjectileGroupName = "Projectile";

            private readonly SimulationWorld _world;

            public EntitySync(SimulationWorld world)
            {
                _world = world;
            }

            public void OnStart()
            {
                GameEntry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
                GameEntry.Event.Subscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
            }

            public void OnDestroy()
            {
                GameEntry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
                GameEntry.Event.Unsubscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
            }

            private void OnShowEntitySuccess(object sender, GameEventArgs e)
            {
                if (e is not ShowEntitySuccessEventArgs args) return;
                if (args.Entity == null) return;
                if (_world == null) return;

                string groupName = args.Entity.EntityGroup?.Name;
                if (string.IsNullOrEmpty(groupName)) return;

                if (groupName == EnemyGroupName && args.Entity.Logic is EnemyBase enemy)
                {
                    _world.RegisterEnemyTransform(enemy.Id, enemy.CachedTransform);
                    _world.UpsertEnemy(CreateEnemySimData(enemy, args.UserData as EnemyData));
                    return;
                }

                if (groupName == DropGroupName && args.Entity.Logic is EntityBase pickupEntity)
                {
                    _world.UpsertPickup(CreatePickupSimData(pickupEntity));
                    return;
                }

                if ((groupName == BulletGroupName || groupName == ProjectileGroupName) &&
                    args.Entity.Logic is EntityBase projectileEntity)
                {
                    _world.UpsertProjectile(CreateProjectileSimData(projectileEntity));
                }
            }

            private void OnHideEntityComplete(object sender, GameEventArgs e)
            {
                if (e is not HideEntityCompleteEventArgs args) return;
                if (args.EntityGroup == null) return;
                if (_world == null) return;

                string groupName = args.EntityGroup.Name;
                if (groupName == EnemyGroupName)
                {
                    _world.UnregisterEnemyTransform(args.EntityId);
                    _world.RemoveEnemyByEntityId(args.EntityId);
                    return;
                }

                if (groupName == DropGroupName)
                {
                    _world.RemovePickupByEntityId(args.EntityId);
                    return;
                }

                if (groupName == BulletGroupName || groupName == ProjectileGroupName)
                {
                    _world.RemoveProjectileByEntityId(args.EntityId);
                }
            }

            private static EnemySimData CreateEnemySimData(EnemyBase enemy, EnemyData enemyData)
            {
                MovementComponent movementComponent = enemy != null ? enemy.GetComponent<MovementComponent>() : null;

                return new EnemySimData
                {
                    EntityId = enemy.Id,
                    Position = enemy.CachedTransform.position,
                    Forward = enemy.CachedTransform.forward,
                    Rotation = enemy.CachedTransform.rotation,
                    Speed = enemyData != null ? enemyData.SpeedBase : 0f,
                    AttackRange = 1f,
                    AvoidEnemyOverlap = movementComponent != null && movementComponent.AvoidEnemyOverlap,
                    EnemyBodyRadius = movementComponent != null ? movementComponent.EnemyBodyRadius : 0.45f,
                    SeparationIterations = movementComponent != null ? movementComponent.SeparationIterations : 2,
                    TargetType = 0,
                    State = 0
                };
            }

            private static PickupSimData CreatePickupSimData(EntityBase pickupEntity)
            {
                return new PickupSimData
                {
                    EntityId = pickupEntity.Id,
                    Position = pickupEntity.CachedTransform.position,
                    PickupRadius = 0.35f,
                    State = 0
                };
            }

            private static ProjectileSimData CreateProjectileSimData(EntityBase projectileEntity)
            {
                return new ProjectileSimData
                {
                    EntityId = projectileEntity.Id,
                    OwnerEntityId = 0,
                    Position = projectileEntity.CachedTransform.position,
                    Forward = projectileEntity.CachedTransform.forward,
                    Speed = 0f,
                    RemainingLifetime = 0f,
                    State = 0
                };
            }
        }
    }
}