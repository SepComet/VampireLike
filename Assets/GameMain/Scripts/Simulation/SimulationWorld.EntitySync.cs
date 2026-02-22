using Entity;
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
            private const string EnemyProjectileGroupName = "EnemyProjectile";

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
                    _world.RegisterEnemyLifecycle(enemy, args.UserData);
                    return;
                }

                if (groupName == DropGroupName && args.Entity.Logic is EntityBase pickupEntity)
                {
                    _world.RegisterPickupLifecycle(pickupEntity);
                    return;
                }

                if ((groupName == BulletGroupName || groupName == ProjectileGroupName ||
                     groupName == EnemyProjectileGroupName) &&
                    args.Entity.Logic is EntityBase projectileEntity)
                {
                    _world.RegisterProjectileLifecycle(projectileEntity, args.UserData);
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
                    _world.UnregisterEnemyLifecycle(args.EntityId);
                    return;
                }

                if (groupName == DropGroupName)
                {
                    _world.UnregisterPickupLifecycle(args.EntityId);
                    return;
                }

                if (groupName == BulletGroupName || groupName == ProjectileGroupName ||
                    groupName == EnemyProjectileGroupName)
                {
                    _world.UnregisterProjectileLifecycle(args.EntityId);
                }
            }
        }
    }
}
